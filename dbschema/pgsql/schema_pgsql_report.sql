-- Database: PostgreSQL (min version 15)

DROP TABLE IF EXISTS mypo_report;

CREATE TABLE mypo_report (
    report_id varchar(48) NOT NULL,
    report_type varchar(16) NOT NULL,           -- WEEKLY, MONTHLY, QUARTERLY, YEARLY
    report_period_start varchar(10) NOT NULL,   -- YYYY-MM-DD, mark the start date of the report period,
    report_period varchar(8) NOT NULL,          -- ISO 8601 format: 2024-W01 for weekly, 2024-01 for monthly, 2024-Q1 for quarterly, 2024 for yearly
    portfolio_id varchar(48) NOT NULL,
    item_code varchar(16) NOT NULL,             -- in EXCHANGE:SYMBOL format
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
CREATE INDEX idx_mypo_report ON mypo_report (portfolio_id, report_type, report_period_start, item_code);
