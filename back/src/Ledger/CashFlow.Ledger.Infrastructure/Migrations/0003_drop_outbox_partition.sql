DROP INDEX IF EXISTS ix_outbox_dispatch;

ALTER TABLE outbox_message DROP COLUMN IF EXISTS partition_key;

CREATE INDEX ASYNC IF NOT EXISTS ix_outbox_dispatch ON outbox_message (published_at, created_at);
