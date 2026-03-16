-- ============================================================================
-- MyMediaVerse Database - DELETE ALL DATA (SAFE VERSION)
-- ============================================================================
-- This version checks if tables exist before trying to delete from them
-- ============================================================================

DO $$ 
DECLARE
    table_exists BOOLEAN;
    rows_deleted INT;
BEGIN 
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'Starting database cleanup...';
    RAISE NOTICE '==========================================';
    
    -- ============================================================================
    -- Delete from potential junction/join tables
    -- ============================================================================
    
    RAISE NOTICE 'Checking for junction tables...';
    
    -- Check and delete from GenreMediaItem or GenresMediaItems
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'GenreMediaItem') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "GenreMediaItem";
        DELETE FROM "GenreMediaItem";
        RAISE NOTICE 'Deleted % rows from GenreMediaItem', rows_deleted;
    ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'GenresMediaItems') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "GenresMediaItems";
        DELETE FROM "GenresMediaItems";
        RAISE NOTICE 'Deleted % rows from GenresMediaItems', rows_deleted;
    END IF;
    
    -- Check and delete from MediaItemTopic or similar
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MediaItemTopic') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "MediaItemTopic";
        DELETE FROM "MediaItemTopic";
        RAISE NOTICE 'Deleted % rows from MediaItemTopic', rows_deleted;
    ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MediaItemsTopics') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "MediaItemsTopics";
        DELETE FROM "MediaItemsTopics";
        RAISE NOTICE 'Deleted % rows from MediaItemsTopics', rows_deleted;
    ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'TopicsMediaItems') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "TopicsMediaItems";
        DELETE FROM "TopicsMediaItems";
        RAISE NOTICE 'Deleted % rows from TopicsMediaItems', rows_deleted;
    END IF;
    
    -- Check and delete from MixlistMediaItem or similar
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MixlistMediaItem') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "MixlistMediaItem";
        DELETE FROM "MixlistMediaItem";
        RAISE NOTICE 'Deleted % rows from MixlistMediaItem', rows_deleted;
    ELSIF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MixlistsMediaItems') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "MixlistsMediaItems";
        DELETE FROM "MixlistsMediaItems";
        RAISE NOTICE 'Deleted % rows from MixlistsMediaItems', rows_deleted;
    END IF;
    
    -- ============================================================================
    -- Delete from child/dependent tables
    -- ============================================================================
    
    RAISE NOTICE 'Deleting from dependent tables...';
    
    -- Highlights
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Highlights') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "Highlights";
        DELETE FROM "Highlights";
        RAISE NOTICE 'Deleted % rows from Highlights', rows_deleted;
    END IF;
    
    -- ============================================================================
    -- Delete from main tables
    -- ============================================================================
    
    RAISE NOTICE 'Deleting from main tables...';
    
    -- Mixlists
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Mixlists') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "Mixlists";
        DELETE FROM "Mixlists";
        RAISE NOTICE 'Deleted % rows from Mixlists', rows_deleted;
    END IF;
    
    -- MediaItems (includes all media types)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MediaItems') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "MediaItems";
        DELETE FROM "MediaItems";
        RAISE NOTICE 'Deleted % rows from MediaItems', rows_deleted;
    END IF;
    
    -- Genres
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Genres') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "Genres";
        DELETE FROM "Genres";
        RAISE NOTICE 'Deleted % rows from Genres', rows_deleted;
    END IF;
    
    -- Topics
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Topics') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "Topics";
        DELETE FROM "Topics";
        RAISE NOTICE 'Deleted % rows from Topics', rows_deleted;
    END IF;
    
    -- Users
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Users') THEN
        SELECT COUNT(*) INTO rows_deleted FROM "Users";
        DELETE FROM "Users";
        RAISE NOTICE 'Deleted % rows from Users', rows_deleted;
    END IF;
    
    RAISE NOTICE '==========================================';
    RAISE NOTICE 'Data deletion complete!';
    RAISE NOTICE '==========================================';
END $$;

-- ============================================================================
-- Verification: Show remaining row counts
-- ============================================================================

DO $$ 
DECLARE
    media_count INT := 0;
    genre_count INT := 0;
    topic_count INT := 0;
    mixlist_count INT := 0;
    user_count INT := 0;
BEGIN 
    -- Only count if tables exist
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'MediaItems') THEN
        SELECT COUNT(*) INTO media_count FROM "MediaItems";
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Genres') THEN
        SELECT COUNT(*) INTO genre_count FROM "Genres";
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Topics') THEN
        SELECT COUNT(*) INTO topic_count FROM "Topics";
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Mixlists') THEN
        SELECT COUNT(*) INTO mixlist_count FROM "Mixlists";
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Users') THEN
        SELECT COUNT(*) INTO user_count FROM "Users";
    END IF;
    
    RAISE NOTICE 'Remaining rows:';
    RAISE NOTICE '  MediaItems: %', media_count;
    RAISE NOTICE '  Genres: %', genre_count;
    RAISE NOTICE '  Topics: %', topic_count;
    RAISE NOTICE '  Mixlists: %', mixlist_count;
    RAISE NOTICE '  Users: %', user_count;
    RAISE NOTICE '==========================================';
    
    IF media_count = 0 AND genre_count = 0 AND topic_count = 0 THEN
        RAISE NOTICE 'SUCCESS: All data has been deleted!';
    ELSE
        RAISE WARNING 'Some data may still remain. This could be normal if you have other tables.';
    END IF;
END $$;














