#!/bin/bash
# Nightly n8n backup to DigitalOcean Spaces. Installed on the droplet as /usr/local/bin/n8n_backup.sh.
# The database is copied with sqlite3's online backup, so n8n keeps running.
# The `config` file is left out on purpose: it holds the encryption key, which lives in
# /opt/mmv-stack/.env (N8N_ENCRYPTION_KEY) and the secrets manager. A restore needs that key.
set -uo pipefail

TIMESTAMP=$(date +"%Y-%m-%d_%H-%M-%S")
DATA_DIR="/var/lib/docker/volumes/n8n_n8n_data/_data"
WORK_DIR="/tmp/n8n_backups/$TIMESTAMP"
ARCHIVE="/tmp/n8n_backups/n8n_${TIMESTAMP}.tar.gz"
SPACES_BUCKET="s3://mymediaverse-storage/backups/n8n"
RETENTION_DAYS=30

mkdir -p "$WORK_DIR"
trap 'rm -rf "$WORK_DIR" "$ARCHIVE"' EXIT

echo "Backing up n8n ($TIMESTAMP) ..."
if ! sqlite3 "$DATA_DIR/database.sqlite" ".backup '$WORK_DIR/database.sqlite'"; then
  echo "ERROR: sqlite backup failed" >&2; exit 1
fi
if [ "$(sqlite3 "$WORK_DIR/database.sqlite" 'PRAGMA integrity_check;')" != "ok" ]; then
  echo "ERROR: backup copy failed the integrity check" >&2; exit 1
fi
echo "Workflows: $(sqlite3 "$WORK_DIR/database.sqlite" 'SELECT COUNT(*) FROM workflow_entity;'), credentials: $(sqlite3 "$WORK_DIR/database.sqlite" 'SELECT COUNT(*) FROM credentials_entity;')"

for d in binaryData nodes git ssh; do
  [ -d "$DATA_DIR/$d" ] && cp -a "$DATA_DIR/$d" "$WORK_DIR/"
done
tar -czf "$ARCHIVE" -C "$WORK_DIR" .

echo "Uploading to Spaces..."
if ! s3cmd put "$ARCHIVE" "$SPACES_BUCKET/$TIMESTAMP/"; then
  echo "ERROR: upload failed" >&2; exit 1
fi

echo "Pruning backups older than $RETENTION_DAYS days..."
s3cmd ls "$SPACES_BUCKET/" | awk '{print $2}' | while read -r dir; do
  d=$(basename "$dir" | cut -d_ -f1)
  if [[ -n "$d" && $(date -d "$d" +%s 2>/dev/null) -lt $(date -d "-$RETENTION_DAYS days" +%s) ]]; then
    echo "Pruning $dir"; s3cmd rm -r "$dir"
  fi
done
echo "Done."
