BEGIN;
ALTER TABLE account ADD CONSTRAINT uniq_username UNIQUE (username);
COMMIT;
