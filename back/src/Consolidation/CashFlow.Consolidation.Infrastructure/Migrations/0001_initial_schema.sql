CREATE TABLE IF NOT EXISTS daily_balance (
    competence_date DATE PRIMARY KEY,
    total_credits NUMERIC(19,2) NOT NULL,
    total_debits NUMERIC(19,2) NOT NULL,
    entry_count INTEGER NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL
);

CREATE TABLE IF NOT EXISTS processed_entry (
    entry_id UUID PRIMARY KEY,
    competence_date DATE NOT NULL,
    processed_at TIMESTAMPTZ NOT NULL
);

CREATE INDEX ASYNC IF NOT EXISTS ix_processed_entry_competence ON processed_entry (competence_date);
