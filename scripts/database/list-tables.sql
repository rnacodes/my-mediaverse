-- ============================================================================
-- List all tables in the current database with row counts
-- ============================================================================

SELECT 
    t.table_name,
    (xpath('/row/cnt/text()', 
        query_to_xml(format('SELECT COUNT(*) as cnt FROM %I.%I', 
            t.table_schema, t.table_name), false, true, ''))
    )[1]::text::int AS row_count
FROM information_schema.tables t
WHERE t.table_schema = 'public'
  AND t.table_type = 'BASE TABLE'
ORDER BY t.table_name;














