ALTER TABLE mypo_ownings ADD COLUMN item_metadata jsonb NULL;
UPDATE mypo_ownings SET item_metadata = '{}'::jsonb;
UPDATE mypo_ownings SET item_metadata = jsonb_set(item_metadata, '{tags}', to_jsonb(string_to_array(tags, ',')));
ALTER TABLE mypo_ownings DROP COLUMN tags;
