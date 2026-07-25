-- Quick check for test data in production database
-- Run this to see if any test data leaked into production

-- Check for recently added media (last 3 hours)
SELECT 
    'Media added in last 3 hours' as category,
    COUNT(*) as count,
    STRING_AGG("Title", ', ') as titles
FROM "MediaItems" 
WHERE "DateAdded" > NOW() - INTERVAL '3 hours'
UNION ALL

-- Check for test-like titles
SELECT 
    'Media with test-like titles' as category,
    COUNT(*) as count,
    STRING_AGG("Title", ', ') as titles
FROM "MediaItems" 
WHERE 
    "Title" ILIKE '%test%' OR
    "Title" ILIKE '%mock%' OR
    "Title" ILIKE '%placeholder%'
UNION ALL

-- Check for Rick Astley (common test video)
SELECT 
    'Rick Astley videos (common test data)' as category,
    COUNT(*) as count,
    STRING_AGG("Title", ', ') as titles
FROM "MediaItems"
WHERE "Title" ILIKE '%rick astley%' OR "Title" ILIKE '%never gonna give you up%';

