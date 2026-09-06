#!/bin/sh
# Periodic PostgreSQL dumps for the production stack.
# Runs as the `db-backup` compose service: one dump per BACKUP_INTERVAL_SECONDS,
# gzipped into /backups, with dumps older than BACKUP_RETENTION_DAYS pruned.
set -eu

: "${POSTGRES_HOST:=postgres}"
: "${POSTGRES_USER:?POSTGRES_USER is required}"
: "${POSTGRES_DB:?POSTGRES_DB is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${BACKUP_DIR:=/backups}"
: "${BACKUP_INTERVAL_SECONDS:=86400}"
: "${BACKUP_RETENTION_DAYS:=14}"

mkdir -p "$BACKUP_DIR"

while true; do
	stamp=$(date -u +%Y%m%dT%H%M%SZ)
	target="$BACKUP_DIR/${POSTGRES_DB}_${stamp}.sql.gz"

	# Write under a temporary name so a crash mid-dump cannot leave a partial
	# file behind that looks like a usable backup.
	if pg_dump --host="$POSTGRES_HOST" --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" \
		--no-owner --no-privileges | gzip -c >"$target.partial"; then
		mv "$target.partial" "$target"
		echo "$(date -u +%FT%TZ) backup written: $target ($(wc -c <"$target") bytes)"
		find "$BACKUP_DIR" -name "${POSTGRES_DB}_*.sql.gz" -type f \
			-mtime "+$BACKUP_RETENTION_DAYS" -delete
	else
		rm -f "$target.partial"
		echo "$(date -u +%FT%TZ) backup FAILED" >&2
	fi

	sleep "$BACKUP_INTERVAL_SECONDS"
done
