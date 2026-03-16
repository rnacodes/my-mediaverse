-- =========================================================
-- FIX DISCRIMINATOR ISSUE - Empty Discriminator Values
-- =========================================================
-- Problem: MediaItems table has records with empty discriminators
-- which causes EF Core to fail when trying to materialize entities
-- =========================================================

-- Step 1: Diagnose the problem
-- Check for media items with empty or null discriminators
SELECT 
    "Id", 
    "Title", 
    "Discriminator", 
    "MediaType",
    "DateAdded"
FROM "MediaItems"
WHERE "Discriminator" IS NULL 
   OR "Discriminator" = ''
ORDER BY "DateAdded" DESC;

-- Step 2: Check total count of problematic records
SELECT COUNT(*) as "ProblematicRecordsCount"
FROM "MediaItems"
WHERE "Discriminator" IS NULL OR "Discriminator" = '';

-- Step 3: Check what discriminator values currently exist
SELECT 
    "Discriminator", 
    COUNT(*) as "Count"
FROM "MediaItems"
GROUP BY "Discriminator"
ORDER BY "Count" DESC;

-- =========================================================
-- SOLUTION OPTIONS (Choose one based on your situation)
-- =========================================================

-- OPTION 1: Delete records with empty discriminators
-- Use this if these are invalid/corrupt records
-- WARNING: This will permanently delete data!
/*
DELETE FROM "MediaItems"
WHERE "Discriminator" IS NULL OR "Discriminator" = '';
*/

-- OPTION 2: Fix discriminators based on MediaType enum value
-- This attempts to set the correct discriminator based on the MediaType field
-- Only run this if your MediaType values are reliable
/*
UPDATE "MediaItems"
SET "Discriminator" = 
    CASE "MediaType"
        WHEN 0 THEN 'Book'
        WHEN 1 THEN 'Movie'
        WHEN 2 THEN 'TvShow'
        WHEN 3 THEN 'Video'
        WHEN 4 THEN 'PodcastSeries'
        WHEN 5 THEN 'PodcastEpisode'
        WHEN 6 THEN 'Article'
        WHEN 7 THEN 'Website'
        WHEN 8 THEN 'YouTubeChannel'
        WHEN 9 THEN 'YouTubePlaylist'
        ELSE 'BaseMediaItem'  -- Fallback
    END
WHERE "Discriminator" IS NULL OR "Discriminator" = '';
*/

-- OPTION 3: Move records with missing discriminators to a default type
-- This converts all problematic records to a specific type (e.g., Article)
/*
UPDATE "MediaItems"
SET "Discriminator" = 'Article',
    "MediaType" = 6  -- Article enum value
WHERE "Discriminator" IS NULL OR "Discriminator" = '';
*/

-- =========================================================
-- VERIFICATION
-- =========================================================
-- After applying a fix, verify there are no more empty discriminators
SELECT COUNT(*) as "RemainingProblematicRecords"
FROM "MediaItems"
WHERE "Discriminator" IS NULL OR "Discriminator" = '';

-- This should return 0 after fix is applied

-- =========================================================
-- RECOMMENDED SAFE APPROACH
-- =========================================================
-- 1. First, identify which records are problematic
-- 2. Backup those records or export them
-- 3. Decide if they should be kept (fix) or removed (delete)
-- 4. Apply the appropriate solution
-- 5. Verify the fix worked
-- 6. Test the API endpoint
-- =========================================================

