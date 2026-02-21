ALTER TABLE mypo_symbol_analysis ADD COLUMN analysis_type varchar(16) NOT NULL;
DROP INDEX IF EXISTS uidx_pk_mypo_symbol_analysis;
CREATE UNIQUE INDEX uidx_pk_mypo_symbol_analysis ON mypo_symbol_analysis (owner_id, market_id, item_type, item_code, analysis_type);
