ALTER TABLE mypo_report
    ADD CONSTRAINT fk_mypo_report_portfolio_id_mypo_portfolio_id
    FOREIGN KEY (portfolio_id)
    REFERENCES mypo_portfolio (portfolio_id)
    ON DELETE CASCADE;

CREATE INDEX idx_mypo_report_portfolio_id ON mypo_report (portfolio_id);

DROP INDEX uidx_mypo_report;
CREATE UNIQUE INDEX uidx_mypo_report ON mypo_report (portfolio_id, report_type, report_period_start, item_code, tx_type);
