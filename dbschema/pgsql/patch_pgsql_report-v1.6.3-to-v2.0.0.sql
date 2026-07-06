CREATE TABLE mypo_report (
    report_id varchar(48) NOT NULL,
    report_type varchar(16) NOT NULL,           -- WEEKLY, MONTHLY, QUARTERLY, YEARLY
    report_period_start varchar(10) NOT NULL,   -- YYYY-MM-DD, mark the start date of the report period,
    report_period varchar(16) NOT NULL,          -- Period label: FY2024-25-W01 for weekly, 2024-01 for monthly, FY2024-25-Q1 for quarterly, FY2024-25 for yearly
    portfolio_id varchar(48) NOT NULL,
    item_code varchar(16) NOT NULL,             -- in EXCHANGE:SYMBOL format
    tx_type varchar(16) NOT NULL,               -- BUY, SELL, TAX, etc
    item_quantity numeric(20,6) NOT NULL DEFAULT 0,
    item_cost numeric(20,6) NOT NULL DEFAULT 0,
    open_value numeric(20,6) NOT NULL DEFAULT 0,
    close_value numeric(20,6) NOT NULL DEFAULT 0,
    is_final boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_report PRIMARY KEY (report_id)
);
CREATE INDEX uidx_mypo_report ON mypo_report (portfolio_id, report_type, report_period_start, item_code, tx_type);
