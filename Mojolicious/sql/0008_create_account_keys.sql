CREATE VIEW account_keys AS
    SELECT account.id AS account_id, username, account_key.id AS account_key_id, account_key, account_value.id as account_value_id, account_value 
    FROM account, account_key, account_value 
    WHERE account.id = account_key.account_id 
        and account_key.id = account_value.account_key_id;
