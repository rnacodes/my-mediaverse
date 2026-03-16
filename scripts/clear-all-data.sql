-- ============================================================================
-- MyMediaVerse Database - DELETE ALL DATA
-- ============================================================================
-- WARNING: This script will DELETE ALL DATA from your database!
-- Use this only when you want to completely reset your database.
-- The table structure will remain intact, but all rows will be deleted.
-- ============================================================================

-- Print start message
DO $$ 
BEGIN 
    RAISE NOTICE 'Starting database cleanup...';
    RAISE NOTICE 'This will delete ALL data from the database.';
END $$;

-- ============================================================================
-- Execute the cleanup in a single transaction
-- ============================================================================

DO $$ 
DECLARE
    genre_media_count INT;
    media_topic_count INT;
    mixlist_media_count INT;
    highlight_count INT;
    mixlist_count INT;
    media_count INT;
    genre_count INT;
    topic_count INT;
    user_count INT;
BEGIN 
    -- ============================================================================
    -- STEP 1: Delete from junction/join tables first (many-to-many relationships)
    -- ============================================================================
    
    RAISE NOTICE 'Deleting junction table data...';
    
    -- Genre-MediaItem junction table
    SELECT COUNT(*) INTO genre_media_count FROM "GenreMediaItem";
    DELETE FROM "GenreMediaItem";
    RAISE NOTICE 'Deleted % rows from GenreMediaItem', genre_media_count;
    
    -- Topic-MediaItem junction table
    SELECT COUNT(*) INTO media_topic_count FROM "MediaItemTopic";
    DELETE FROM "MediaItemTopic";
    RAISE NOTICE 'Deleted % rows from MediaItemTopic', media_topic_count;
    
    -- ============================================================================
    -- STEP 2: Delete from child/dependent tables
    -- ============================================================================
    
    RAISE NOTICE 'Deleting dependent table data...';
    
    -- Mixlist items (relationship between Mixlists and MediaItems)
    SELECT COUNT(*) INTO mixlist_media_count FROM "MixlistMediaItem";
    DELETE FROM "MixlistMediaItem";
    RAISE NOTICE 'Deleted % rows from MixlistMediaItem', mixlist_media_count;
    
    -- Highlights (Readwise highlights)
    SELECT COUNT(*) INTO highlight_count FROM "Highlights";
    DELETE FROM "Highlights";
    RAISE NOTICE 'Deleted % rows from Highlights', highlight_count;
    
    -- ============================================================================
    -- STEP 3: Delete from main tables
    -- ============================================================================
    
    RAISE NOTICE 'Deleting main table data...';
    
    -- Mixlists
    SELECT COUNT(*) INTO mixlist_count FROM "Mixlists";
    DELETE FROM "Mixlists";
    RAISE NOTICE 'Deleted % rows from Mixlists', mixlist_count;
    
    -- All Media Items (includes Podcasts, Videos, Books, Articles, etc.)
    SELECT COUNT(*) INTO media_count FROM "MediaItems";
    DELETE FROM "MediaItems";
    RAISE NOTICE 'Deleted % rows from MediaItems', media_count;
    
    -- Genres
    SELECT COUNT(*) INTO genre_count FROM "Genres";
    DELETE FROM "Genres";
    RAISE NOTICE 'Deleted % rows from Genres', genre_count;
    
    -- Topics
    SELECT COUNT(*) INTO topic_count FROM "Topics";
    DELETE FROM "Topics";
    RAISE NOTICE 'Deleted % rows from Topics', topic_count;
    
    -- Users (if authentication is enabled)
    SELECT COUNT(*) INTO user_count FROM "Users";
    DELETE FROM "Users";
    RAISE NOTICE 'Deleted % rows from Users', user_count;
    
    RAISE NOTICE 'Data deletion complete.';
END $$;

-- ============================================================================
-- VERIFICATION: Count remaining rows in all tables
-- ============================================================================

DO $$ 
DECLARE
    media_count INT;
    genre_count INT;
    topic_count INT;
    mixlist_count INT;
    user_count INT;
    highlight_count INT;
BEGIN 
    SELECT COUNT(*) INTO media_count FROM "MediaItems";
    SELECT COUNT(*) INTO genre_count FROM "Genres";
    SELECT COUNT(*) INTO topic_count FROM "Topics";
    SELECT COUNT(*) INTO mixlist_count FROM "Mixlists";
    SELECT COUNT(*) INTO user_count FROM "Users";
    SELECT COUNT(*) INTO highlight_count FROM "Highlights";
    
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'DATABASE CLEANUP COMPLETE';
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'Remaining rows:';
    RAISE NOTICE '  MediaItems: %', media_count;
    RAISE NOTICE '  Genres: %', genre_count;
    RAISE NOTICE '  Topics: %', topic_count;
    RAISE NOTICE '  Mixlists: %', mixlist_count;
    RAISE NOTICE '  Users: %', user_count;
    RAISE NOTICE '  Highlights: %', highlight_count;
    RAISE NOTICE '==========================================';
    
    IF media_count = 0 AND genre_count = 0 AND topic_count = 0 THEN
        RAISE NOTICE 'SUCCESS: All data has been deleted.';
    ELSE
        RAISE WARNING 'Some data may still remain. Check the counts above.';
    END IF;
END $$;

-- ============================================================================
-- OPTIONAL: Vacuum tables to reclaim space
-- ============================================================================

-- Uncomment the lines below if you want to reclaim disk space
-- This can take time on large databases

-- VACUUM FULL "MediaItems";
-- VACUUM FULL "Genres";
-- VACUUM FULL "Topics";
-- VACUUM FULL "Mixlists";
-- VACUUM FULL "Users";
-- VACUUM FULL "Highlights";
-- VACUUM FULL "GenreMediaItem";
-- VACUUM FULL "MediaItemTopic";
-- VACUUM FULL "MixlistMediaItem";

DO $$ 
BEGIN 
    RAISE NOTICE 'Script execution complete.';
END $$;

