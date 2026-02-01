ALTER TABLE IF EXISTS mypo_transactions RENAME TO mypo_buys_sells;
ALTER TABLE IF EXISTS mypo_buys_sells
    RENAME CONSTRAINT fk_mypo_transactions_portfolio_id_mypo_portfolio_id TO fk_mypo_buys_sells_portfolio_id_mypo_portfolio_id;
ALTER TABLE IF EXISTS mypo_buys_sells
    RENAME CONSTRAINT pk_mypo_transactions TO pk_mypo_buys_sells;
ALTER INDEX IF EXISTS idx_mypo_transactions_portfolio_id
    RENAME TO idx_mypo_buys_sells_portfolio_id;
ALTER INDEX IF EXISTS idx_mypo_transactions_tx_time
    RENAME TO idx_mypo_buys_sells_tx_time;

ALTER TABLE IF EXISTS mypo_roi RENAME TO mypo_settlements;
ALTER TABLE IF EXISTS mypo_settlements
    RENAME COLUMN roi_id TO tx_id;
ALTER TABLE IF EXISTS mypo_settlements
    RENAME CONSTRAINT fk_mypo_roi_portfolio_id_mypo_portfolio_id TO fk_mypo_settlements_portfolio_id_mypo_portfolio_id;
ALTER TABLE IF EXISTS mypo_settlements
    RENAME CONSTRAINT pk_mypo_roi TO pk_mypo_settlements;
ALTER INDEX IF EXISTS idx_mypo_roi_portfolio_id RENAME TO idx_mypo_settlements_portfolio_id;
ALTER INDEX IF EXISTS idx_mypo_roi_portfolio_id_tx_time RENAME TO idx_mypo_settlements_portfolio_id_tx_time;
ALTER INDEX IF EXISTS idx_mypo_roi_portfolio_id_tx_type RENAME TO idx_mypo_settlements_portfolio_id_tx_type;
