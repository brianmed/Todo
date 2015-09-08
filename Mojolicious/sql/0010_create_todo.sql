BEGIN;
CREATE TABLE todo(
    id serial not null PRIMARY KEY,
    account_id integer not null,
    todo json NOT NULL,
    updated timestamp default CURRENT_TIMESTAMP,
    inserted timestamp default CURRENT_TIMESTAMP,
    foreign key (account_id) references account (id) on delete cascade
);

CREATE TRIGGER todo_timestamp BEFORE INSERT OR UPDATE ON todo
FOR EACH ROW EXECUTE PROCEDURE update_timestamp();

GRANT SELECT ON TABLE todo TO kevin;
GRANT INSERT ON TABLE todo TO kevin;
GRANT UPDATE ON TABLE todo TO kevin;
GRANT DELETE ON TABLE todo TO kevin;

GRANT USAGE, SELECT ON SEQUENCE todo_id_seq TO kevin;
COMMIT;
