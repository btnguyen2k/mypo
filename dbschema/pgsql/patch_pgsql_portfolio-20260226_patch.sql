CREATE TABLE mypo_checkpoints (
    checkpoint_id varchar(48) NOT NULL,
    owner_id varchar(48) NOT NULL,
    portfolio_id varchar(48) NOT NULL,
    market_id varchar(16) NOT NULL,
    item_code varchar(16) NOT NULL,
    checkpoint_type varchar(32) NOT NULL,
    checkpoint_time timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    checkpoint_metadata jsonb NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_checkpoints PRIMARY KEY (checkpoint_id)
);
CREATE UNIQUE INDEX uidx_pk_mypo_checkpoints ON mypo_checkpoints (owner_id, portfolio_id, market_id, item_code, checkpoint_type);

CREATE TABLE mypo_market_events (
    event_id varchar(48) NOT NULL,
    owner_id varchar(48) NOT NULL,
    market_id varchar(16) NOT NULL,
    item_code varchar(16) NOT NULL,
    event_type varchar(16) NOT NULL,
    event_time timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    event_metadata jsonb NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_market_events PRIMARY KEY (event_id)
);
CREATE UNIQUE INDEX uidx_pk_mypo_market_events ON mypo_market_events (owner_id, market_id, item_code, event_type);
