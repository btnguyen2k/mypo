CREATE TABLE mypo_portfolio_plans (
    plan_id varchar(48) NOT NULL,
    owner_id varchar(48) NOT NULL,
    portfolio_id varchar(48) NULL,
    plan_name varchar(64) NOT NULL,
    plan_metadata jsonb NULL,
    created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_portfolio_plans PRIMARY KEY (plan_id),
    CONSTRAINT fk_mypo_portfolio_plans_portfolio_id_mypo_portfolio_id FOREIGN KEY (portfolio_id) REFERENCES mypo_portfolio (portfolio_id) ON DELETE SET NULL
);
CREATE INDEX idx_mypo_portfolio_plans_owner_id ON mypo_portfolio_plans (owner_id);
CREATE INDEX idx_mypo_portfolio_plans_portfolio_id ON mypo_portfolio_plans (portfolio_id);
