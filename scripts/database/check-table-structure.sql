-- Check the actual column names in junction tables
-- Run this to see what the real column names are

-- Check MediaItemTopics table structure
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'MediaItemTopics'
ORDER BY ordinal_position;

-- Check MediaItemGenres table structure  
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'MediaItemGenres'
ORDER BY ordinal_position;

-- Check MixlistMediaItems table structure
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'MixlistMediaItems'
ORDER BY ordinal_position;

-- Check YouTubePlaylistVideos table structure
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name = 'YouTubePlaylistVideos'
ORDER BY ordinal_position;










