# MyMediaVerse — Local Testing Database (Docker)

Scripts to stand up a local Postgres container, seed it with the **real demo data** from a
backup dump, and tear it down. This gives you a testing database that is **decoupled from the
live demo/production databases** — safe for Playwright E2E runs, manual poking, and rehearsing
migrations before applying them for real.

> **Local-testing convention:** when testing locally, **unless explicitly indicated otherwise,
> point the backend at this Docker test DB** (`localhost:5433`) — **not** the live demo or
> production databases. This keeps local experiments, reindexes, and migration rehearsals off
> the real data. Reindexing the *live* demo/prod Typesense collections is the documented
> exception: that intentionally requires pointing at the corresponding live DB.

The frontend never talks to the database directly (it only knows `localhost:5033/api`), so using
this DB is purely a matter of pointing the **backend** at it. Keep `VITE_DEMO_MODE=true` to skip
auth, same as normal demo dev.

## Prerequisites

- **Docker Desktop** installed and running.
- A demo backup dump in the repo's `for-claude/` folder (e.g.
  `mmvdemodb_2026-06-01_02-00-01.dump`). The seed script uses the **newest** `.dump` there by default.

## The scripts

| Script | What it does |
| --- | --- |
| `create-testing-db.ps1` | Spins up the `mmv-testing-db` container (pgvector, port 5433, named volume) and waits until it's ready. |
| `seed-testing-db.ps1` | Restores the demo backup dump into the container — schema, EF migration history, and all data. Re-runnable (clean reseed each time). |
| `delete-testing-db.ps1` | Removes the container; `-RemoveVolume` also deletes the data volume for a full wipe. |
| `create-testing-typesense.ps1` | Spins up the `mmv-testing-typesense` container (typesense 30.2, port 8108, named volume) for isolated local **search** and waits until it's healthy. |
| `delete-testing-typesense.ps1` | Removes the Typesense container; `-RemoveVolume` also deletes its data volume. |

All three accept parameters (container name, port, db name, image, etc.) — run
`Get-Help .\create-testing-db.ps1 -Detailed` to see them. The defaults match this README.

## Quick start

From this folder (`scripts/database`) in PowerShell:

```powershell
# 1. Create the container (empty DB, ready to seed)
.\create-testing-db.ps1

# 2. Load the real demo data from the newest backup dump in for-claude\
.\seed-testing-db.ps1

# 3. (later) Remove the container, keeping the data volume
.\delete-testing-db.ps1

# 3b. Or remove the container AND wipe the data volume (fresh start next time)
.\delete-testing-db.ps1 -RemoveVolume
```

After step 2 the script prints row counts per table — that's your proof the data landed
(e.g. ~32 MediaItems, 46 Genres, 40 Highlights for the 2026-06-01 dump).

## Pointing the backend at the testing DB

In the **same PowerShell window** you'll launch the backend from:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=mmvdemodb;Username=postgres;Password=test"
cd ..\..\src\MyMediaVerse\MyMediaVerse.Web.API
dotnet run
```

`ConnectionStrings__DefaultConnection` is the reliable override (it wins over `DATABASE_URL` and
appsettings). The app does **not** auto-migrate on startup, but you don't need it to here — the
seed restores a fully-built schema (including `__EFMigrationsHistory`) straight from the dump.

## Local search (Typesense)

The Postgres test DB on its own has no search backend — search goes through Typesense, not the
database. For fully isolated local search (including auto-embedding and hybrid search), stand up a
local Typesense container alongside the test DB instead of pointing at the live Droplet:

```powershell
# 1. Start a local Typesense (empty, no collections yet)
.\create-testing-typesense.ps1

# 2. In the backend window, point at BOTH the test DB and local Typesense:
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5433;Database=mmvdemodb;Username=postgres;Password=test"
$env:TYPESENSE_HOST = "localhost"; $env:TYPESENSE_PORT = "8108"; $env:TYPESENSE_PROTOCOL = "http"
$env:TYPESENSE_ADMIN_API_KEY = "test-api-key"
$env:OPENAI_API_KEY = "<key from repo-root .env>"   # required for auto-embedding; omit for keyword-only
cd ..\..\src\MyMediaVerse\MyMediaVerse.Web.API; dotnet run

# 3. A fresh Typesense has no collections — reindex once to create + populate them:
#    POST /api/search/reindex, /reindex-mixlists, /reindex-notes, /reindex-highlights  (auth required)
```

The `embedding` field (and therefore hybrid/semantic search) is only created when the backend that
builds the collections has `OPENAI_API_KEY` set. Without a key, collections come up keyword-only —
which is fine for non-search testing. Tear down with `.\delete-testing-typesense.ps1 [-RemoveVolume]`.

## Day-to-day

```powershell
docker stop mmv-testing-db     # done for now (data persists in the volume)
docker start mmv-testing-db    # back to it, data intact
.\seed-testing-db.ps1          # reset to the pristine demo dataset any time
```

## Why pg18 and pg_restore (not psql)

- The demo backups are **custom-format** dumps (`pg_restore`, **not** `psql < file.sql`).
- They are produced by `pg_dump 18.x` (archive format v1.16). A **pg18** `pg_restore` reads them
  cleanly; an older **pg17** one can reject them. The container defaults to `pgvector/pgvector:pg18`
  for this reason. To mirror the current production major version instead, pass
  `-PgImage "pgvector/pgvector:pg17"` to `create-testing-db.ps1` (be aware of the restore caveat above).
- The image is `pgvector/...` (not plain `postgres`) because the demo DB uses the `vector` extension.

## Rehearsing a migration before touching demo

This DB is ideal for practicing a demo-DB update safely:

```powershell
# 1. Container up + seeded, backend env var pointed at :5433 (see above)
# 2. Apply the new migration ONLY to this local container:
cd ..\..\src\MyMediaVerse\MyMediaVerse.Web.API
dotnet ef database update --project ..\MyMediaVerse.Infrastructure
# 3. Run the app, click around, confirm nothing broke.
# 4. If it goes wrong: .\delete-testing-db.ps1 -RemoveVolume, then recreate + reseed. Zero risk to demo.
# 5. Only once clean, apply the migration manually to demo + production:
#    dotnet ef database update --project ..\MyMediaVerse.Infrastructure --connection "$env:DEMO_DB_CONNECTION"
#    dotnet ef database update --project ..\MyMediaVerse.Infrastructure --connection "$env:PRODUCTION_DB_CONNECTION"
```

Because the seed restores the dump's `__EFMigrationsHistory`, `dotnet ef database update` correctly
applies only the **new** migrations on top of the restored schema.

> ⚠️ Do **not** use `POST /api/dev/reset-database` on this DB if you're practicing migrations — it
> uses `EnsureCreated` (no migration history), which makes a later `dotnet ef database update` try
> to re-run every migration and fail. For a clean migration-practice DB, recreate the container instead.
