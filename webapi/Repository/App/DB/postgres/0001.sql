-- User & database.
DO $$ BEGIN
  IF (SELECT COUNT(*) FROM pg_user WHERE usename = 'expenseuser') = 0 THEN
    CREATE USER expenseuser WITH PASSWORD 'expensepwd' CREATEDB;
  END if;
END $$;

CREATE DATABASE expenses_db WITH
    OWNER = expenseuser
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.utf8'
    LC_CTYPE = 'en_US.utf8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

CREATE EXTENSION unaccent;

-- Config table.
CREATE TABLE IF NOT EXISTS c_config(
    config_key VARCHAR(64) NOT NULL
    , config_value VARCHAR(64) NOT NULL
);

DO $$ BEGIN
  IF (SELECT COUNT(*) FROM c_config WHERE config_key = 'db_version') = 0 THEN
    INSERT INTO c_config(config_key, config_value)
    VALUES('db_version', '1');
  END IF;
END $$;

-- Category table.
CREATE TABLE IF NOT EXISTS d_category(
    cat_id 		SERIAL PRIMARY KEY NOT NULL
    , cat_desc 	VARCHAR(64) NOT NULL COLLATE "und-ci-ai"
    , cat_order 	INT NULL
);

CREATE INDEX IF NOT EXISTS i_category_cat_order 
	ON d_category (
	cat_order ASC
	, cat_desc	ASC
);

CREATE INDEX IF NOT EXISTS i_category_cat_desc
	ON d_category (cat_desc ASC);

-- Expense table.
CREATE TABLE IF NOT EXISTS t_expense (
	expense_id			SERIAL PRIMARY KEY NOT NULL
	, expense_date		TIMESTAMPTZ NOT NULL
	, expense_desc		VARCHAR(128)
	, expense_amount	NUMERIC(12, 2) NOT NULL
	, cat_id			    INT 
	REFERENCES d_category(cat_id)
	ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS i_expense_expense_date 
	ON t_expense (expense_date ASC);

-- Expense search view.
CREATE OR REPLACE VIEW v_expense AS
SELECT expense_id 
    , expense_date
    , expense_desc
    , expense_amount
    , expense.cat_id
    , cat.cat_desc
FROM t_expense AS expense
	LEFT JOIN d_category AS cat
		ON expense.cat_id = cat.cat_id
;

-- Expense report function.
CREATE OR REPLACE FUNCTION fn_expense_report (
	tz VARCHAR(128) DEFAULT NULL
) 
RETURNS TABLE (
	expense_id				INT4
	, expense_date			TIMESTAMPTZ
	, expense_desc			VARCHAR(128)
	, expense_amount		NUMERIC(12, 2)
	, cat_id				INT4
	, cat_desc				VARCHAR(64)
	, expense_date_tz		TIMESTAMP WITHOUT TIME zone
	, year				    INT4
	, month				    INT4
	, week 				    INT4
	, day				    INT4
)
LANGUAGE plpgsql
AS $$
BEGIN
	tz := COALESCE(tz, CURRENT_SETTING('TIMEZONE')) ;
	
	RETURN QUERY 
	SELECT 
    expense.expense_id
    , expense.expense_date
    , expense.expense_desc
    , expense.expense_amount
    , expense.cat_id
    , cat.cat_desc
    , expense.expense_date AT TIME ZONE tz as expense_date_tz
		, CAST(DATE_PART('year',  expense.expense_date AT TIME ZONE tz) AS INTEGER) AS year
		, CAST(DATE_PART('month', expense.expense_date AT TIME ZONE tz) AS INTEGER) AS month
		, CAST(DATE_PART('week',  expense.expense_date AT TIME ZONE tz) AS INTEGER) AS week
		, CAST(DATE_PART('day',   expense.expense_date AT TIME ZONE tz) AS INTEGER) AS day
	FROM t_expense expense
		LEFT JOIN d_category cat 
			ON expense.cat_id = cat.cat_id;
END; $$

-- User table.
CREATE TABLE IF NOT EXISTS d_user (
	user_id 					        SERIAL PRIMARY KEY NOT NULL
	, user_login 				        VARCHAR(128) NOT NULL
	, user_pwd 					        TEXT NOT NULL
	, user_access_key			        UUID NULL
	, user_access_token_expire_minutes  NUMERIC(12, 2) NULL
	, user_refresh_key			        VARCHAR(128) NULL
	, user_refresh_token_expire_minutes NUMERIC(12, 2) NULL
	, user_refresh_expire_date	        TIMESTAMPTZ NULL
	, user_login_expire_date	        TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS i_user_login_pwd
	ON d_user (
		user_login ASC
		, user_pwd	ASC
);

CREATE INDEX IF NOT EXISTS i_user_access_key
	ON d_user (
		user_access_key ASC
		, user_login_expire_date ASC
);

DO $$ BEGIN
  IF (SELECT COUNT(*) FROM d_user WHERE user_login = 'admin') = 0 THEN
    INSERT INTO d_user(user_login, user_pwd)
    VALUES('admin', 'CZfxoPXlBrg7qcUA4Y2JQg==');
  END IF;
END $$;