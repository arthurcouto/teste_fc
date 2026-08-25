CREATE TABLE IF NOT EXISTS entry (
    id UUID PRIMARY KEY,
    type SMALLINT NOT NULL,
    amount NUMERIC(19,2) NOT NULL,
    competence_date DATE NOT NULL,
    description VARCHAR(200),
    recorded_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX ASYNC IF NOT EXISTS ix_entry_competence ON entry (competence_date, recorded_at, id);

CREATE TABLE IF NOT EXISTS outbox_message (
    id UUID PRIMARY KEY,
    entry_id UUID NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    payload TEXT NOT NULL,
    partition_key SMALLINT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    published_at TIMESTAMPTZ,
    claimed_by VARCHAR(64),
    claimed_at TIMESTAMPTZ
);

CREATE INDEX ASYNC IF NOT EXISTS ix_outbox_dispatch ON outbox_message (published_at, partition_key, created_at);
