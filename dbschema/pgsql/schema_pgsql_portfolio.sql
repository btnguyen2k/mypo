-- Database: PostgreSQL (min version 15)

DROP TABLE IF EXISTS mypo_transactions;
DROP TABLE IF EXISTS mypo_roi;
DROP TABLE IF EXISTS mypo_ownings;
DROP TABLE IF EXISTS mypo_portfolio;

CREATE TABLE mypo_portfolio (
    portfolio_id varchar(48) NOT NULL,
    parent_id varchar(48) NULL,
    portfolio_name varchar(64) NOT NULL,
    portfolio_desc varchar(256) NULL,
    portfolio_currency varchar(8) NOT NULL,
    owner_id varchar(48) NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    portfolio_meta jsonb NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_portfolio PRIMARY KEY (portfolio_id),
    CONSTRAINT fk_mypo_portfolio_parent_id_mypo_portfolio_id FOREIGN KEY (parent_id) REFERENCES mypo_portfolio (portfolio_id) ON DELETE RESTRICT
);
CREATE INDEX idx_mypo_portfolio_owner_id ON mypo_portfolio (owner_id);
CREATE INDEX idx_mypo_portfolio_parent_id ON mypo_portfolio (parent_id);
CREATE INDEX idx_mypo_portfolio_portfolio_metadata ON mypo_portfolio USING GIN (portfolio_metadata);
CREATE INDEX idx_mypo_portfolio_portfolio_metadata_viewers ON mypo_portfolio ((portfolio_metadata->'viewers'));

CREATE TABLE mypo_transactions (
    tx_id varchar(48) NOT NULL,
    portfolio_id varchar(48) NOT NULL,
    tx_type varchar(8) NOT NULL,
    tx_time timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tx_quantity numeric(20,6) NOT NULL DEFAULT 0,
    tx_price numeric(20,6) NOT NULL DEFAULT 0,
    tx_notes varchar(256) NULL,
    fee_tx numeric(20,6) NOT NULL DEFAULT 0,
    fee_tax numeric(20,6) NOT NULL DEFAULT 0,
    fee_other numeric(20,6) NOT NULL DEFAULT 0,
    item_type varchar(16) NOT NULL,
    item_code varchar(16) NOT NULL,
    market_id varchar(16) NULL,
    is_settled boolean NOT NULL DEFAULT FALSE,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_transactions PRIMARY KEY (tx_id),
    CONSTRAINT fk_mypo_transactions_portfolio_id_mypo_portfolio_id FOREIGN KEY (portfolio_id) REFERENCES mypo_portfolio (portfolio_id) ON DELETE CASCADE
);
CREATE INDEX idx_mypo_transactions_portfolio_id ON mypo_transactions (portfolio_id);
CREATE INDEX idx_mypo_transactions_tx_time ON mypo_transactions (tx_time);

CREATE TABLE mypo_ownings (
    owning_id varchar(48) NOT NULL,
    portfolio_id varchar(48) NOT NULL,
    item_type varchar(16) NOT NULL,
    item_code varchar(16) NOT NULL,
    market_id varchar(16) NULL,
    item_quantity numeric(20,6) NOT NULL DEFAULT 0,
    average_price numeric(20,6) NOT NULL DEFAULT 0,
    tags varchar(256) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_ownings PRIMARY KEY (owning_id),
    CONSTRAINT fk_mypo_ownings_portfolio_id_mypo_portfolio_id FOREIGN KEY (portfolio_id) REFERENCES mypo_portfolio (portfolio_id) ON DELETE CASCADE
);
CREATE INDEX idx_mypo_ownings_portfolio_id ON mypo_ownings (portfolio_id);
CREATE UNIQUE INDEX uidx_mypo_ownings_portfolio_item ON mypo_ownings (portfolio_id, item_type, item_code, market_id);

CREATE TABLE mypo_roi (
    roi_id varchar(48) NOT NULL,
    tx_status varchar(8) NOT NULL DEFAULT 'NEW',
    portfolio_id varchar(48) NOT NULL,
    tx_type varchar(8) NOT NULL,
    tx_time timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    tx_value numeric(20,6) NOT NULL DEFAULT 0,
    ref_tx_id varchar(48) NULL,
    ref_item_type varchar(16) NULL,
    ref_item_code varchar(16) NULL,
    ref_market_id varchar(16) NULL,
    tx_desc varchar(256) NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_roi PRIMARY KEY (roi_id),
    CONSTRAINT fk_mypo_roi_portfolio_id_mypo_portfolio_id FOREIGN KEY (portfolio_id) REFERENCES mypo_portfolio (portfolio_id) ON DELETE CASCADE
);
CREATE INDEX idx_mypo_roi_portfolio_id ON mypo_roi (portfolio_id);
CREATE INDEX idx_mypo_roi_portfolio_id_tx_time ON mypo_roi (portfolio_id, tx_time);
CREATE INDEX idx_mypo_roi_portfolio_id_tx_type ON mypo_roi (portfolio_id, tx_type);
-- CREATE INDEX idx_mypo_roi_tx_time ON mypo_roi (tx_time);
-- CREATE INDEX idx_mypo_roi_tx_type ON mypo_roi (tx_type);
