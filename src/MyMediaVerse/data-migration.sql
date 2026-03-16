-- TODO: Delete if no longer using
-- Data Migration Script to Convert JSONB Topics/Genres to Separate Tables
-- Run this BEFORE applying the new EF migration

-- First, let's see what data we have
SELECT 
    "Id", 
    "Title", 
    "Topics"::text as topics_json, 
    "Genres"::text as genres_json
FROM "MediaItems" 
WHERE "Topics" IS NOT NULL OR "Genres" IS NOT NULL;

-- Create temporary tables to hold the conversion
CREATE TEMP TABLE temp_topics AS
WITH topic_data AS (
    SELECT 
        "Id" as media_id,
        jsonb_array_elements_text("Topics") as topic_name
    FROM "MediaItems"
    WHERE "Topics" IS NOT NULL 
    AND jsonb_array_length("Topics") > 0
)
SELECT DISTINCT topic_name FROM topic_data WHERE topic_name != '';

CREATE TEMP TABLE temp_genres AS
WITH genre_data AS (
    SELECT 
        "Id" as media_id,
        jsonb_array_elements_text("Genres") as genre_name
    FROM "MediaItems"
    WHERE "Genres" IS NOT NULL 
    AND jsonb_array_length("Genres") > 0
)
SELECT DISTINCT genre_name FROM genre_data WHERE genre_name != '';

-- Show what topics and genres we found
SELECT 'Topics found:' as info, COUNT(*) as count FROM temp_topics;
SELECT * FROM temp_topics ORDER BY topic_name;

SELECT 'Genres found:' as info, COUNT(*) as count FROM temp_genres;
SELECT * FROM temp_genres ORDER BY genre_name;

-- Note: The actual data conversion will be handled by the EF migration
-- This script is just for inspection and preparation
