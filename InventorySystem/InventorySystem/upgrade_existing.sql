-- Run this ONLY if you already created the old `users` table from an earlier version.
-- (New installs: run schema.sql instead.)  Run it once.
USE inventory_db;

ALTER TABLE users
    ADD COLUMN is_active       TINYINT(1) NOT NULL DEFAULT 1,
    ADD COLUMN failed_attempts INT        NOT NULL DEFAULT 0,
    ADD COLUMN locked_until    DATETIME   NULL DEFAULT NULL,
    ADD COLUMN last_login      DATETIME   NULL DEFAULT NULL;

-- Then run schema.sql too: it only creates the missing suppliers/products/transactions tables
-- (every statement there is CREATE ... IF NOT EXISTS, so your users are untouched).
