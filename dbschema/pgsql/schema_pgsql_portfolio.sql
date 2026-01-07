-- Database: PostgreSQL (min version 15)

DROP TABLE IF EXISTS mypo_portfolio;

CREATE TABLE mypo_portfolio (
    portfolio_id varchar(48) NOT NULL,
    parent_id varchar(48) NULL,
    portfolio_name varchar(64) NOT NULL,
    portfolio_desc varchar(256) NULL,
    portfolio_currency varchar(8) NOT NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    owner_id varchar(48) NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_portfolio PRIMARY KEY (portfolio_id),
    CONSTRAINT fk_mypo_portfolio_parent_id_mypo_portfolio_id FOREIGN KEY (parent_id) REFERENCES mypo_portfolio (portfolio_id) ON DELETE CASCADE
);
CREATE INDEX idx_mypo_portfolio_owner_id ON mypo_portfolio (owner_id);
CREATE INDEX idx_mypo_portfolio_parent_id ON mypo_portfolio (parent_id);
