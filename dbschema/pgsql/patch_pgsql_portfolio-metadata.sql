ALTER TABLE mypo_portfolio ADD COLUMN portfolio_metadata jsonb NULL;
CREATE INDEX idx_mypo_portfolio_portfolio_metadata ON mypo_portfolio USING GIN (portfolio_metadata);
CREATE INDEX idx_mypo_portfolio_portfolio_metadata_viewers ON mypo_portfolio ((portfolio_metadata->>'viewers'));

