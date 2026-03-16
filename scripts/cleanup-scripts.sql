-- ============================================
-- Database Cleanup Scripts for MyMediaVerse
-- ============================================
-- These scripts allow you to clean up test data from your database
-- Run individual sections as needed during development/testing
-- ============================================

-- ============================================
-- CLEANUP: YouTube Channels
-- ============================================
-- Deletes all YouTube channels and sets ChannelId to NULL on associated videos
-- Videos will remain in the database
DO $$
BEGIN
    -- Videos have ON DELETE SET NULL for ChannelId, so they'll remain
    DELETE FROM "YouTubeChannels";
    RAISE NOTICE 'All YouTube channels deleted. Associated videos remain with ChannelId set to NULL.';
END $$;

-- ============================================
-- CLEANUP: YouTube Playlists
-- ============================================
-- Deletes all YouTube playlists and their video associations
-- Videos will remain in the database
DO $$
BEGIN
    -- Junction table will cascade delete
    DELETE FROM "YouTubePlaylist";
    RAISE NOTICE 'All YouTube playlists deleted. Associated videos remain in the database.';
END $$;

-- ============================================
-- CLEANUP: Podcast Episodes
-- ============================================
-- Deletes all podcast episodes (but keeps series)
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    -- First delete from join tables
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastEpisodes");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastEpisodes");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastEpisodes");
    
    -- Get count before deletion
    SELECT COUNT(*) INTO deleted_count FROM "PodcastEpisodes";
    
    -- Delete podcast episodes (this will cascade to MediaItems due to TPT)
    DELETE FROM "PodcastEpisodes";
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Books"
        UNION SELECT "Id" FROM "Movies"
        UNION SELECT "Id" FROM "TvShows"
        UNION SELECT "Id" FROM "Videos"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "Articles"
        UNION SELECT "Id" FROM "PodcastSeries"
    );
    
    RAISE NOTICE 'Deleted % podcast episodes.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: Podcast Series (and all episodes)
-- ============================================
-- Deletes all podcast series and their episodes
DO $$
DECLARE
    deleted_series INTEGER;
    deleted_episodes INTEGER;
BEGIN
    -- Count before deletion
    SELECT COUNT(*) INTO deleted_episodes FROM "PodcastEpisodes";
    SELECT COUNT(*) INTO deleted_series FROM "PodcastSeries";
    
    -- Delete from join tables for episodes
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastEpisodes");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastEpisodes");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastEpisodes");
    
    -- Delete from join tables for series
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastSeries");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastSeries");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "PodcastSeries");
    
    -- Delete episodes first (cascade delete is configured)
    DELETE FROM "PodcastEpisodes";
    
    -- Delete series
    DELETE FROM "PodcastSeries";
    
    -- Clean up orphaned MediaItems
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Books"
        UNION SELECT "Id" FROM "Movies"
        UNION SELECT "Id" FROM "TvShows"
        UNION SELECT "Id" FROM "Videos"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "Articles"
    );
    
    RAISE NOTICE 'Deleted % podcast series and % episodes.', deleted_series, deleted_episodes;
END $$;

-- ============================================
-- CLEANUP: Books (and their highlights)
-- ============================================
-- Deletes all books and their associated highlights
DO $$
DECLARE
    deleted_books INTEGER;
    deleted_highlights INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_books FROM "Books";
    SELECT COUNT(*) INTO deleted_highlights FROM "Highlights" WHERE "BookId" IS NOT NULL;
    
    -- Delete highlights associated with books (ON DELETE SET NULL)
    UPDATE "Highlights" SET "BookId" = NULL WHERE "BookId" IN (SELECT "Id" FROM "Books");
    
    -- Delete from join tables
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Books");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Books");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Books");
    
    -- Delete books
    DELETE FROM "Books";
    
    -- Clean up orphaned MediaItems
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Movies"
        UNION SELECT "Id" FROM "TvShows"
        UNION SELECT "Id" FROM "Videos"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "Articles"
        UNION SELECT "Id" FROM "PodcastSeries"
        UNION SELECT "Id" FROM "PodcastEpisodes"
    );
    
    RAISE NOTICE 'Deleted % books. % highlights now unlinked from books.', deleted_books, deleted_highlights;
END $$;

-- ============================================
-- CLEANUP: Movies
-- ============================================
-- Deletes all movies
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "Movies";
    
    -- Delete from join tables
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Movies");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Movies");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Movies");
    
    -- Delete movies
    DELETE FROM "Movies";
    
    -- Clean up orphaned MediaItems
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Books"
        UNION SELECT "Id" FROM "TvShows"
        UNION SELECT "Id" FROM "Videos"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "Articles"
        UNION SELECT "Id" FROM "PodcastSeries"
        UNION SELECT "Id" FROM "PodcastEpisodes"
    );
    
    RAISE NOTICE 'Deleted % movies.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: TV Shows
-- ============================================
-- Deletes all TV shows
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "TvShows";
    
    -- Delete from join tables
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "TvShows");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "TvShows");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "TvShows");
    
    -- Delete TV shows
    DELETE FROM "TvShows";
    
    -- Clean up orphaned MediaItems
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Books"
        UNION SELECT "Id" FROM "Movies"
        UNION SELECT "Id" FROM "Videos"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "Articles"
        UNION SELECT "Id" FROM "PodcastSeries"
        UNION SELECT "Id" FROM "PodcastEpisodes"
    );
    
    RAISE NOTICE 'Deleted % TV shows.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: Videos (Generic Videos, not YouTube Channels)
-- ============================================
-- Deletes all video records (excluding YouTubeChannels)
-- Note: This includes YouTube playlist videos
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "Videos";
    
    -- Delete from YouTube playlist junction table
    DELETE FROM "YouTubePlaylistVideo" 
    WHERE "VideoId" IN (SELECT "Id" FROM "Videos");
    
    -- Delete from join tables
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Videos");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Videos");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Videos");
    
    -- Delete videos
    DELETE FROM "Videos";
    
    -- Clean up orphaned MediaItems
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Books"
        UNION SELECT "Id" FROM "Movies"
        UNION SELECT "Id" FROM "TvShows"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "Articles"
        UNION SELECT "Id" FROM "PodcastSeries"
        UNION SELECT "Id" FROM "PodcastEpisodes"
    );
    
    RAISE NOTICE 'Deleted % videos.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: Articles (and their highlights)
-- ============================================
-- Deletes all articles and their associated highlights
DO $$
DECLARE
    deleted_articles INTEGER;
    deleted_highlights INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_articles FROM "Articles";
    SELECT COUNT(*) INTO deleted_highlights FROM "Highlights" WHERE "ArticleId" IS NOT NULL;
    
    -- Update highlights to unlink from articles (ON DELETE SET NULL)
    UPDATE "Highlights" SET "ArticleId" = NULL WHERE "ArticleId" IN (SELECT "Id" FROM "Articles");
    
    -- Delete from join tables
    DELETE FROM "MediaItemTopics" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Articles");
    
    DELETE FROM "MediaItemGenres" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Articles");
    
    DELETE FROM "MixlistMediaItems" 
    WHERE "MediaItemId" IN (SELECT "Id" FROM "Articles");
    
    -- Delete articles
    DELETE FROM "Articles";
    
    -- Clean up orphaned MediaItems
    DELETE FROM "MediaItems" 
    WHERE "Id" NOT IN (
        SELECT "Id" FROM "Books"
        UNION SELECT "Id" FROM "Movies"
        UNION SELECT "Id" FROM "TvShows"
        UNION SELECT "Id" FROM "Videos"
        UNION SELECT "Id" FROM "YouTubeChannels"
        UNION SELECT "Id" FROM "PodcastSeries"
        UNION SELECT "Id" FROM "PodcastEpisodes"
    );
    
    RAISE NOTICE 'Deleted % articles. % highlights now unlinked from articles.', deleted_articles, deleted_highlights;
END $$;

-- ============================================
-- CLEANUP: Highlights
-- ============================================
-- Deletes all highlights (independent of articles/books)
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "Highlights";
    DELETE FROM "Highlights";
    RAISE NOTICE 'Deleted % highlights.', deleted_count;
END $$;

-- ============================================
-- TAXONOMY CLEANUP
-- ============================================
-- Two types of cleanup available for Topics and Genres:
-- 1. "All Topics/Genres" - Deletes EVERYTHING, media remains without associations
-- 2. "Orphaned Topics/Genres" - Deletes ONLY unused items, preserves active associations
-- ============================================

-- ============================================
-- CLEANUP: All Topics
-- ============================================
-- Deletes ALL topics (media items remain, just lose their topic associations)
-- Use this when starting fresh with a new taxonomy system
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "Topics";
    
    -- Cascade delete will handle MediaItemTopics join table
    DELETE FROM "Topics";
    
    RAISE NOTICE 'Deleted % topics. Media items remain without topic associations.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: All Genres
-- ============================================
-- Deletes ALL genres (media items remain, just lose their genre associations)
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "Genres";
    
    -- Cascade delete will handle MediaItemGenres join table
    DELETE FROM "Genres";
    
    RAISE NOTICE 'Deleted % genres. Media items remain without genre associations.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: Orphaned Topics
-- ============================================
-- Deletes topics that are not associated with any media items
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count 
    FROM "Topics" 
    WHERE "Id" NOT IN (SELECT DISTINCT "TopicId" FROM "MediaItemTopics");
    
    DELETE FROM "Topics" 
    WHERE "Id" NOT IN (SELECT DISTINCT "TopicId" FROM "MediaItemTopics");
    
    RAISE NOTICE 'Deleted % orphaned topics.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: Orphaned Genres
-- ============================================
-- Deletes genres that are not associated with any media items
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count 
    FROM "Genres" 
    WHERE "Id" NOT IN (SELECT DISTINCT "GenreId" FROM "MediaItemGenres");
    
    DELETE FROM "Genres" 
    WHERE "Id" NOT IN (SELECT DISTINCT "GenreId" FROM "MediaItemGenres");
    
    RAISE NOTICE 'Deleted % orphaned genres.', deleted_count;
END $$;

-- ============================================
-- CLEANUP: All Mixlists
-- ============================================
-- Deletes all mixlists and their associations
-- Media items will remain in the database
DO $$
DECLARE
    deleted_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO deleted_count FROM "Mixlists";
    
    -- Junction table will cascade delete
    DELETE FROM "Mixlists";
    
    RAISE NOTICE 'Deleted % mixlists. Media items remain in the database.', deleted_count;
END $$;

-- ============================================
-- NUCLEAR OPTION: Delete ALL Media Items
-- ============================================
-- WARNING: This deletes EVERYTHING except Topics and Genres
-- Use with extreme caution!
DO $$
DECLARE
    total_deleted INTEGER := 0;
BEGIN
    -- Delete all join table entries
    DELETE FROM "MediaItemTopics";
    DELETE FROM "MediaItemGenres";
    DELETE FROM "MixlistMediaItems";
    DELETE FROM "YouTubePlaylistVideo";
    
    -- Delete all mixlists
    DELETE FROM "Mixlists";
    
    -- Delete all highlights
    DELETE FROM "Highlights";
    
    -- Delete all YouTube playlists
    DELETE FROM "YouTubePlaylist";
    
    -- Delete all media type records
    DELETE FROM "PodcastEpisodes";
    DELETE FROM "PodcastSeries";
    DELETE FROM "Books";
    DELETE FROM "Movies";
    DELETE FROM "TvShows";
    DELETE FROM "Videos";
    DELETE FROM "Articles";
    DELETE FROM "YouTubeChannels";
    
    -- Delete all remaining media items
    DELETE FROM "MediaItems";
    
    RAISE NOTICE 'NUCLEAR CLEANUP COMPLETE: All media items, mixlists, and highlights deleted. Topics and Genres preserved.';
END $$;

