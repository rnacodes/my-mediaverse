#!/bin/bash
# On-demand production database backup, taken before each import step of the initial data sync.
# Installed on the droplet as /usr/local/bin/pg_backup_before_step.sh.
# Usage: pg_backup_before_step.sh <step>     e.g.  pg_backup_before_step.sh goodreads
#
# Writes two files to Spaces (kept until the sync is finished, no pruning):
#   backups/initial-sync/prod-<YYYY-MM-DD-HHMM>-before-<step>.dump    pg_dump custom format
#   backups/initial-sync/prod-<YYYY-MM-DD-HHMM>-before-<step>.counts  "table<TAB>rows" per public table
# The .counts file lets a restore be checked table by table.
#
# The password comes from /root/.pgpass (chmod 600), the same as the nightly pg_backup.sh.
# Do NOT add `set -x`: it would echo connection details into the terminal.
set -euo pipefail

STEP="${1:-}"
if [[ ! "$STEP" =~ ^[a-z0-9]+(-[a-z0-9]+)*$ ]]; then
  echo "Usage: $0 <step>   (lowercase letters, digits and dashes, e.g. goodreads)" >&2
  exit 2
fi

PGHOST="dpg-d75huhnfte5s73fd31f0-a.ohio-postgres.render.com"
PGPORT="5432"
PGUSER="mmvproduser"
PGDATABASE="mmvproductiondb"
export PGHOST PGPORT PGUSER PGDATABASE PGPASSFILE=/root/.pgpass

SPACES_DIR="s3://mymediaverse-storage/backups/initial-sync"
NAME="prod-$(TZ=America/New_York date +%Y-%m-%d-%H%M)-before-${STEP}"

WORK_DIR=$(mktemp -d /tmp/pg_before_step.XXXXXX)
chmod 700 "$WORK_DIR"
trap 'rm -rf "$WORK_DIR"' EXIT

echo "Dumping $PGDATABASE -> $NAME.dump ..."
pg_dump -Fc -f "$WORK_DIR/$NAME.dump"

echo "Counting rows per table ..."
psql -X -v ON_ERROR_STOP=1 -At -F $'\t' -o "$WORK_DIR/$NAME.counts" -c "
  SELECT table_name,
         (xpath('/row/c/text()',
                query_to_xml(format('SELECT count(*) AS c FROM %I.%I', table_schema, table_name),
                             false, true, '')))[1]::text::bigint
  FROM information_schema.tables
  WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
  ORDER BY table_name;"

TABLES=$(wc -l < "$WORK_DIR/$NAME.counts")
if [[ "$TABLES" -eq 0 ]]; then
  echo "ERROR: the row-count file is empty" >&2
  exit 1
fi

echo "Uploading to $SPACES_DIR/ ..."
for ext in dump counts; do
  f="$WORK_DIR/$NAME.$ext"
  s3cmd put --no-progress "$f" "$SPACES_DIR/$NAME.$ext" > /dev/null
  local_size=$(stat -c %s "$f")
  remote_size=$(s3cmd ls "$SPACES_DIR/$NAME.$ext" | awk '{print $3}')
  if [[ "$local_size" != "$remote_size" ]]; then
    echo "ERROR: upload check failed for $NAME.$ext (local $local_size bytes, Spaces '${remote_size}')" >&2
    exit 1
  fi
done

echo "OK  $NAME.dump  $(numfmt --to=iec "$(stat -c %s "$WORK_DIR/$NAME.dump")")  ($TABLES tables counted)"
echo "    $SPACES_DIR/$NAME.dump"
