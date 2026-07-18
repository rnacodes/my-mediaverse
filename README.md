# My MediaVerse

**One home for everything you read, watch, and listen to — track it, organize it, and rediscover it.**

<!-- Add project logo here -->

> **Version 1.5** — "The Great Refactor" is complete: the backend and frontend have been re-architected, both test suites rebuilt from scratch, and several new integrations added on top of the v1 demo. **Version 2** (new media types, web archival, automation) is now in active development.

> _Formerly developed under the placeholder name "Project Loopbreaker" — you may still see "PLB" in older commit history, but **My MediaVerse** is the real name._

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Screenshots](#screenshots)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)
- [Demo Site](#demo-site)
- [Testing](#testing)
- [Roadmap (v2)](#roadmap-v2)
- [Project Status](#project-status)
- [Documentation](#documentation)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)

---

## Overview

My MediaVerse (MMV) is a comprehensive personal media library management application designed to help you **track, organize, and discover** content across every format you consume. Books you've read, podcasts you're working through, movies on your watchlist, articles you've saved, notes from your Obsidian vault — MMV brings them all together in one unified platform instead of scattering them across a dozen single-purpose apps.

I built My MediaVerse after years of trying to organize my entertainment options and never finding a tool that did exactly what I wanted. Plenty of platforms handle one kind of media — movies, say — but none could bring _all_ of my options together. The original placeholder name, "Project Loopbreaker," came from a real problem: it's easy to fall into a loop of pointless phone-scrolling when you can't decide what to do. With everything in one place, that's solved. If I'm bored, I just pull up one of my Mixlists for whatever topic I'm in the mood for and immediately find something to read, watch, or listen to.

I'm a fiction writer, and I often compare programming to writing. If the **v1** read-only demo (shared at graduation) was the *first draft*, then **v1.5** is the *editing pass* — a deep refactor incorporating everything I learned during my apprenticeship and coursework, building a solid foundation before the major feature expansion of v2.

### Who is it for?

- **Avid media consumers** who want to track their consumption across many formats at once
- **Note-takers** who want to connect reading highlights and notes back to their source material
- **Curators** who enjoy building themed collections of diverse content
- **Knowledge workers** who want their Obsidian notes living alongside their media library

---

## Features

### Media Library Management

- **8 media types supported:** Books, Podcasts (series & episodes), Movies, TV Shows, YouTube (videos, channels, and playlists), Articles, Websites, and Notes
- **Status tracking:** Uncharted, In Progress, Consumed, or Did Not Finish
- **Rating system:** SuperLike, Like, Neutral, or Dislike
- **Classification:** Organize with genres and topics for easy discovery
- **Ownership tracking:** Track whether you own, want to buy, or have borrowed an item
- **Personal notes & links** on any item, with the ability to link to external documentation

### Mixlists — Cross-Type Collections

Create themed playlists called **Mixlists** that can hold *any combination* of media types. Unlike a traditional playlist, a Mixlist isn't limited to one format — it's a flexible collection built around a theme, topic, or any criteria you choose. Want a science-fiction collection? Drop in your favorite novels, podcasts, films, and short stories side by side.

### External Integrations

| Service                  | What it does                                                          |
| ------------------------ | -------------------------------------------------------------------- |
| **ListenNotes**          | Search and import podcast series & episodes with rich metadata       |
| **TMDB**                 | Movie and TV show metadata enrichment                                |
| **Trakt**                | Import your TV watch history, watchlist, and ratings                 |
| **YouTube Data API**     | Import videos, channels, and playlists                               |
| **Readwise / Reader**    | Sync highlights and saved articles, auto-linked to their sources     |
| **Google Books**         | Book search and metadata retrieval                                   |
| **Open Library**         | Book metadata and description enrichment                             |
| **Obsidian (Quartz)**    | Sync notes from your personal Obsidian vaults                        |

### Sync Workflows

Dedicated sync pages make bulk imports painless:

- **Trakt sync** — pull in your entire TV watch history, watchlist, and ratings via device authorization
- **Readwise sync** — import highlights and articles in one pass, with highlights automatically linked back to their source books and articles

### Search & Discovery

- **Typesense-powered search:** fast, typo-tolerant full-text search across your entire library
- **Advanced filtering:** filter by media type, topic, genre, or status, and combine filters
- **Batch indexing model:** indexing runs in bulk — manually from the admin dashboard or on a schedule — rather than on every individual save, so large imports stay fast

### Highlights & Notes

- Import highlights from Readwise, automatically linked to their source articles and books
- Bulk-linking tools to associate orphaned highlights with sources
- Sync notes from your Obsidian vaults and link them to related media items
- Notes are searchable alongside every other media type

### AI-Powered Discovery

My MediaVerse uses AI to enhance organization and discovery:

| Feature                       | Description                                                                                                        |
| ----------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **Vector embeddings**         | Semantic embeddings for all media items and notes via OpenAI's `text-embedding-3-large`, stored in PostgreSQL with **pgvector** |
| **Similar items**             | Surface semantically related media and notes based on embedding similarity                                        |
| **AI note summaries**         | Concise descriptions auto-generated for notes using DigitalOcean Gradient AI (`gpt-oss-120b`)                     |

> Two AI providers are used intentionally — OpenAI for embeddings, DigitalOcean Gradient for text generation — because each serves a distinct purpose and the single-user cost difference is negligible.

### Admin Dashboard

An admin area for operating the library:

- **Search reindexing** — trigger bulk Typesense reindexing per collection
- **Background-job monitoring** — observe the scheduled enrichment services
- **AI admin** — manage embedding and summary generation
- **Script execution** — run maintenance scripts from the UI
- **Demo read-only mode** — a guarded mode that blocks writes for the public demo

---

## Tech Stack

### Frontend

| Technology              | Purpose                          |
| ----------------------- | -------------------------------- |
| React 18                | UI framework                     |
| Vite                    | Build tool and dev server        |
| Material-UI (MUI)       | Component library                |
| React Router v7         | Client-side routing              |
| TanStack Query v5       | Server-state & data caching      |
| React Hook Form + Zod   | Form handling and validation     |
| react-error-boundary    | Resilient error isolation        |
| Axios                   | HTTP client                      |
| Vitest + RTL + MSW      | Testing                          |

### Backend

| Technology               | Purpose                |
| ------------------------ | ---------------------- |
| ASP.NET Core 10.0        | Web API framework      |
| Entity Framework Core 10 | ORM                    |
| PostgreSQL + pgvector    | Database & vector search |
| JWT                      | Authentication         |
| Clean Architecture       | Architectural pattern  |

### Infrastructure & DevOps

| Service                     | Purpose                                |
| --------------------------- | -------------------------------------- |
| Typesense (self-hosted)     | Full-text search engine                |
| DigitalOcean Droplet        | Hosts Typesense + Umami via Docker Compose |
| DigitalOcean Spaces         | File / thumbnail storage               |
| Umami (self-hosted)         | Privacy-friendly analytics             |
| Render.com                  | Application hosting                    |
| Cloudflare                  | DNS, SSL, DDoS protection              |

### AI Services

| Provider                 | Purpose                                    |
| ------------------------ | ------------------------------------------ |
| OpenAI                   | Vector embeddings (`text-embedding-3-large`) |
| DigitalOcean Gradient AI | Text generation for note descriptions      |

---


## Architecture

My MediaVerse follows **Clean Architecture** principles with a layered structure that promotes separation of concerns and testability.

```
  ┌──── Web.API ─── Infrastructure ────┐   outer ring — frameworks & drivers
  │    ┌───── Application ──────┐       │   use cases / business logic
  │    │     ┌── Domain ──┐     │       │   entities & domain models
  │    │     │   Shared   │     │       │   shared kernel (core)
  │    │     └────────────┘     │       │
  │    └────────────────────────┘       │
  └──────── dependencies point inward ──┘

  Web.API        Controllers, middleware, filters
  Infrastructure EF Core, external API clients, background services
  Application    Services, business logic, interfaces (uses DTOs)
  DTOs           Data transfer objects (depend on Domain)
  Domain         Entities, domain models
  Shared         Cross-cutting contracts & utilities — the leaf every layer builds on
```

### What "The Great Refactor" changed (v1.5)

The v1.5 refactor was as much about engineering discipline as features:

- **Split the "fat" MediaController** into focused, media-type-specific controllers (book, movie, tvshow, podcast, youtube, article, website, note, …)
- **Slimmed a "god" `Program.cs`** by extracting service registration into composable extension methods and simplifying connection-string resolution
- **Rebuilt the frontend** around a feature-based folder structure with ~25 custom **TanStack Query** hooks for all data access, replacing ad-hoc fetching
- **Moved Typesense to a bulk/scheduled indexing model**, removing per-item realtime indexing that would have wasted processing during large imports
- **Added scheduled background services** that periodically enrich metadata (book descriptions, movie/TV, podcasts) and generate AI note summaries and embeddings
- **Rebuilt both test suites** on open-source tooling (see [Testing](#testing))

### Key Architectural Patterns

- **Clean Architecture:** dependencies flow inward; outer layers (Web.API, Infrastructure) depend on inner ones, with a Shared kernel at the core
- **SOLID principles:** single responsibility, dependency injection throughout
- **Service layer:** business logic separated from controllers
- **Shared interfaces:** cross-layer contracts live in the Shared layer to preserve dependency inversion

---

## Getting Started

### Prerequisites

- Node.js 18+
- .NET 10 SDK
- PostgreSQL 15+ (with the `pgvector` extension)
- Typesense server (optional for local development)

### Frontend Setup

```bash
cd frontend
npm install
npm start          # or: npm run dev
```

The frontend starts at `http://localhost:5173`.

### Backend Setup

```bash
cd src/MyMediaVerse/MyMediaVerse.Web.API
dotnet build
dotnet run
```

The API starts at `http://localhost:5033`.

### Environment Variables

#### Required

| Variable                                                 | Description                         |
| -------------------------------------------------------- | ----------------------------------- |
| `DATABASE_URL` or `ConnectionStrings__DefaultConnection` | PostgreSQL connection string        |
| `JWT_SECRET`                                             | JWT signing key (min 32 characters) |
| `AUTH_USERNAME`                                          | Login username                      |
| `AUTH_PASSWORD`                                          | Login password                      |
| `FRONTEND_URL`                                           | Frontend URL for CORS               |

#### Optional (API Integrations)

| Variable                        | Description              |
| ------------------------------- | ------------------------ |
| `LISTENNOTES_API_KEY`           | ListenNotes podcast API  |
| `TMDB_API_KEY`                  | TMDB movie/TV API        |
| `YOUTUBE_API_KEY`               | YouTube Data API         |
| `READWISE_API_KEY`              | Readwise / Reader API    |
| `TYPESENSE_ADMIN_API_KEY`       | Typesense search         |
| `TYPESENSE_HOST`                | Typesense server URL     |
| `DIGITALOCEANSPACES__ACCESSKEY` | DO Spaces access         |
| `DIGITALOCEANSPACES__SECRETKEY` | DO Spaces secret         |
| `OPENAI_API_KEY`                | OpenAI embeddings        |
| `GRADIENT_API_KEY`              | DigitalOcean Gradient AI |

#### Local-only (migration script)

These are read **only** by `scripts/run-migrations.ps1` from your local shell. They are never read by deployed app code and should not be set on Render or the demo Droplet.

| Variable                   | Description                                                                        |
| -------------------------- | --------------------------------------------------------------------------------- |
| `PRODUCTION_DB_CONNECTION` | Connection string for the production DB (Render)                                  |
| `DEMO_DB_CONNECTION`       | Connection string for the demo DB via SSH tunnel (`Host=localhost;Port=5433;...`) |
| `DEMO_DROPLET_IP`          | IP of the DigitalOcean Droplet hosting the demo DB                                |

---

## API Endpoints

The API is RESTful with a base URL of `/api`. Controllers are organized by responsibility:

### Media

| Controller | Endpoint         | Description                          |
| ---------- | ---------------- | ----------------------------------- |
| Media      | `/api/media`     | Unified CRUD across all media types |
| Book       | `/api/book`      | Book-specific operations            |
| Movie      | `/api/movie`     | Movie operations (TMDB)             |
| TVShow     | `/api/tvshow`    | TV show operations                  |
| Podcast    | `/api/podcast`   | Podcast series & episodes           |
| YouTube    | `/api/youtube`   | Videos, channels, playlists         |
| Article    | `/api/article`   | Article operations                  |
| Website    | `/api/website`   | Website operations                  |
| Note       | `/api/note`      | Notes management                    |

### Collections, Classification & Discovery

| Controller     | Endpoint              | Description                              |
| -------------- | --------------------- | ---------------------------------------- |
| Mixlist        | `/api/mixlist`        | Cross-type collection management         |
| Topics         | `/api/topics`         | Topic management                         |
| Genres         | `/api/genres`         | Genre management                         |
| Highlight      | `/api/highlight`      | Highlight operations and linking         |
| Search         | `/api/search`         | Typesense search + bulk reindexing       |
| Recommendation | `/api/recommendation` | Similar-items / embedding-based discovery |
| RelatedMedia   | `/api/relatedmedia`   | Media relationships                      |

### Integrations & Sync

| Controller  | Endpoint          | Description                          |
| ----------- | ----------------- | ------------------------------------ |
| Readwise    | `/api/readwise`   | Readwise + Reader sync               |
| Trakt       | `/api/trakt`      | Trakt TV watch-history sync          |
| ListenNotes | `/api/listennotes`| Podcast search proxy                 |
| TMDB        | `/api/tmdb`       | Movie / TV search proxy              |

### AI, Admin & Ops

| Controller | Endpoint        | Description                         |
| ---------- | --------------- | ----------------------------------- |
| AI         | `/api/ai`       | Embedding & summary generation      |
| Auth       | `/api/auth`     | Authentication                      |
| Dev / Demo | `/api/dev`, `/api/demo` | Maintenance & demo controls |
| Health     | `/api/health`   | Service health checks               |

---

## Demo Site

**Live Demo:** [https://demo.mymediaverseuniverse.com](https://demo.mymediaverseuniverse.com)

The demo site runs in **read-only mode** by default, so you can explore the interface and features without modifying data. Sample data is included to demonstrate all media types and functionality.

---

## Testing

Both test suites were rebuilt from scratch during v1.5 on actively maintained, open-source tooling.

- **Backend:** xUnit, **NSubstitute** (mocking), **AwesomeAssertions**, and **Testcontainers** for real PostgreSQL + pgvector integration tests
- **Frontend:** **Vitest**, React Testing Library, and **MSW** (Mock Service Worker) with shared data factories and a `renderWithProviders` helper

### Run All Tests

```powershell
.\run-all-tests.ps1
```

### Backend Tests

```bash
dotnet test tests/MyMediaVerse.UnitTests/MyMediaVerse.UnitTests.csproj
dotnet test tests/MyMediaVerse.IntegrationTests/MyMediaVerse.IntegrationTests.csproj
```

### Frontend Tests

```bash
cd frontend
npm test              # Watch mode
npm run test:run      # Single run
npm run test:coverage # With coverage
```

Test results are saved to the `logs/` directory with timestamps.

---

## Roadmap (v2)

Version 2 expands the feature set on top of the v1.5 foundation:

- **New media types:** Video Games, Music, Reddit, and *Panorama* — a catch-all type for anything that doesn't fit neatly elsewhere
- **Document management:** full **Paperless-ngx** integration (the service is provisioned; wiring it into the app is a v2 task)
- **Web bookmarking & archival:** webpage hierarchy, **ArchiveBox** archival, Wayback Machine fallback, link de-duplication, and broken-link monitoring
- **Automation:** scheduled **N8N** workflows for ongoing metadata enrichment and daily syncs
- **Transcription import** for podcasts and videos

### In flight

- **Search Refactor** — queued/batched Typesense indexing, delete propagation, and a cleaner frontend search flow, hardening search for a full-size library
- **Initial Data Sync** — building the remaining import features (Goodreads chunked upload, genre mapping, podcast OPML) and running the first full data population

---

## Project Status

**Current Version:** v1.5 — *The Great Refactor* complete.

### Completed in v1.5

- Backend re-architecture (controller split, slimmed `Program.cs`, clean dependency flow)
- Frontend re-architecture (feature folders, TanStack Query data layer, shared components)
- Both test suites rebuilt on open-source tooling
- New integrations: Trakt sync, Obsidian/Quartz note sync, scheduled background enrichment
- Bulk/scheduled Typesense indexing model
- AI embeddings, similar-items discovery, and AI note summaries
- Read-only demo site with sample data

### Next

Populate the library via the **Initial Data Sync**, complete the **Search Refactor**, then begin **v2** feature expansion.

---

## Contributing

This is a personal project, but feedback and suggestions are welcome. Please open an issue on GitHub to:

- Report bugs
- Suggest features
- Ask questions

---

## License

**Proprietary License**

> Please do not copy or redistribute this code without permission.

This project is not open source. All rights reserved.

---

## Acknowledgments

This project integrates with and is grateful for the following services and APIs:

- [ListenNotes](https://www.listennotes.com/) — Podcast search API
- [TMDB](https://www.themoviedb.org/) — Movie and TV database
- [Trakt](https://trakt.tv/) — TV watch-history and tracking API
- [YouTube Data API](https://developers.google.com/youtube/v3) — Video platform integration
- [Readwise](https://readwise.io/) — Reading highlights service
- [Google Books API](https://developers.google.com/books) — Book metadata
- [Open Library](https://openlibrary.org/) — Open book metadata
- [Typesense](https://typesense.org/) — Open-source search engine
- [OpenAI](https://openai.com/) — Embedding models for semantic discovery
- [DigitalOcean Gradient AI](https://www.digitalocean.com/products/gradient) — Text generation
- [TanStack Query](https://tanstack.com/query) — Frontend data management
- [MSW](https://mswjs.io/) — API mocking for tests
- [Material-UI](https://mui.com/) — React component library

---

*Built with care for personal media management.*

— Portfolio: [raeccleston.com](https://www.raeccleston.com/) · Repo: [github.com/rnacodes/my-mediaverse](https://github.com/rnacodes/my-mediaverse)
