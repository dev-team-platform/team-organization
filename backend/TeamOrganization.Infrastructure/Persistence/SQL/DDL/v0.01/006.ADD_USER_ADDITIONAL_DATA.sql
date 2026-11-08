ALTER TABLE users
    ADD COLUMN additional_data JSONB NOT NULL DEFAULT '{}'::jsonb;