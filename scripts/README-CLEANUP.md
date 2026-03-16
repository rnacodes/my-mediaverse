# Database Cleanup Scripts

This directory contains SQL cleanup scripts and backend API endpoints for managing test data in your MyMediaVerse database.

## Overview

During development and testing, you'll accumulate test data that needs to be cleaned up before importing real data. These scripts provide safe, organized ways to delete specific types of data or perform a complete database cleanup.

## Available Cleanup Methods

### 1. Frontend UI (Recommended for Testing)

The easiest way to cleanup data is through the frontend UI:

1. Start your backend API (if not running):
   ```powershell
   cd C:\Users\rashi\source\repos\MyMediaVerse\src\MyMediaVerse\MyMediaVerse.Web.API
   dotnet run
   ```

2. Start your frontend (if not running):
   ```powershell
   cd C:\Users\rashi\source\repos\MyMediaVerse\frontend
   npm start
   ```

3. Navigate to: http://localhost:3000/cleanup

4. Click the appropriate cleanup button for the data type you want to delete

**Features:**
- ✅ Visual confirmation dialogs
- ✅ Real-time feedback with counts of deleted items
- ✅ Color-coded severity levels
- ✅ Safe and easy to use

### 2. Backend API Endpoints (Swagger/Postman)

All cleanup operations are available as POST endpoints under `/api/dev/`:

#### Individual Data Type Cleanup

- **YouTube Data**: `POST /api/dev/cleanup-youtube-data`
  - Deletes: Channels, Playlists, Videos, Playlist-Video associations
  
- **Podcasts**: `POST /api/dev/cleanup-podcasts`
  - Deletes: Podcast series and all episodes
  
- **Books**: `POST /api/dev/cleanup-books`
  - Deletes: All books (highlights are unlinked, not deleted)
  
- **Movies**: `POST /api/dev/cleanup-movies`
  - Deletes: All movies
  
- **TV Shows**: `POST /api/dev/cleanup-tvshows`
  - Deletes: All TV shows
  
- **Articles**: `POST /api/dev/cleanup-articles`
  - Deletes: All articles (highlights are unlinked, not deleted)
  
- **Highlights**: `POST /api/dev/cleanup-highlights`
  - Deletes: All highlights from all sources
  
- **Mixlists**: `POST /api/dev/cleanup-mixlists`
  - Deletes: All mixlists (media items are preserved)

#### Taxonomy Cleanup

- **All Topics**: `POST /api/dev/cleanup-all-topics`
  - Deletes: ALL topics (media items remain, lose topic associations)
  
- **All Genres**: `POST /api/dev/cleanup-all-genres`
  - Deletes: ALL genres (media items remain, lose genre associations)

#### Maintenance Cleanup

- **Orphaned Topics**: `POST /api/dev/cleanup-orphaned-topics`
  - Deletes: Topics not associated with any media items
  
- **Orphaned Genres**: `POST /api/dev/cleanup-orphaned-genres`
  - Deletes: Genres not associated with any media items

#### Nuclear Option

- **All Media**: `POST /api/dev/cleanup-all-media`
  - ⚠️ **WARNING**: Deletes EVERYTHING except Topics and Genres
  - Use this to completely reset your database after testing
  - Inverse of "All Topics/Genres" - this keeps taxonomy, deletes media

**Example using PowerShell:**
```powershell
Invoke-WebRequest -Uri 'http://localhost:5033/api/dev/cleanup-youtube-data' -Method POST
```

### 3. Direct SQL Scripts

For advanced users or direct database access, you can run the SQL scripts in `cleanup-scripts.sql`.

**To run a specific cleanup:**

1. Open your PostgreSQL client (pgAdmin, DBeaver, etc.)
2. Connect to your MyMediaVerse database
3. Copy the relevant section from `cleanup-scripts.sql`
4. Execute the SQL code

**Example sections:**
- `CLEANUP: YouTube Channels`
- `CLEANUP: Podcast Series`
- `CLEANUP: Books`
- `NUCLEAR OPTION: Delete ALL Media Items`

## What Gets Deleted

### Data Relationships

| Cleanup Type | What's Deleted | What's Preserved |
|-------------|----------------|------------------|
| YouTube Data | Channels, Playlists, Videos | Topics, Genres, Other media |
| Podcasts | Series, Episodes | Topics, Genres, Other media |
| Books | Book records | Highlights (unlinked), Topics, Genres |
| Movies | Movie records | Topics, Genres, Other media |
| TV Shows | TV show records | Topics, Genres, Other media |
| Articles | Article records | Highlights (unlinked), Topics, Genres |
| Highlights | All highlights | Books, Articles, Other media |
| Mixlists | All mixlists | All media items |
| **All Topics** | **All topics** | **All media (without topic associations)** |
| **All Genres** | **All genres** | **All media (without genre associations)** |
| Orphaned Topics | Unused topics only | Topics linked to media |
| Orphaned Genres | Unused genres only | Genres linked to media |
| **All Media (Nuclear)** | **Everything** | **Only Topics and Genres** |

### Cascade Behavior

The cleanup operations respect your database relationships:

- ✅ **Podcast Episodes** are deleted when their **Series** is deleted
- ✅ **Playlist-Video associations** are deleted when **Playlists** are deleted
- ✅ **Video ChannelId** is set to NULL when **Channels** are deleted (videos remain)
- ✅ **Highlights** are unlinked (not deleted) when **Books/Articles** are deleted
- ✅ **Join table entries** (MediaItemTopics, MediaItemGenres, MixlistMediaItems) are automatically cleaned up

## Understanding Topics & Genres Cleanup

You have four options for managing Topics and Genres:

### All Topics vs. Orphaned Topics

- **All Topics** (`cleanup-all-topics`): 
  - Deletes **every single topic** in your database
  - Media items remain but lose all topic associations
  - Use when: Starting fresh with a new taxonomy system
  
- **Orphaned Topics** (`cleanup-orphaned-topics`):
  - Deletes **only topics** that aren't linked to any media
  - Media items and their topic associations are unaffected
  - Use when: Cleaning up unused/duplicate topics

### All Genres vs. Orphaned Genres

- **All Genres** (`cleanup-all-genres`):
  - Deletes **every single genre** in your database
  - Media items remain but lose all genre associations
  - Use when: Starting fresh with a new genre system
  
- **Orphaned Genres** (`cleanup-orphaned-genres`):
  - Deletes **only genres** that aren't linked to any media
  - Media items and their genre associations are unaffected
  - Use when: Cleaning up unused/duplicate genres

## Recommended Workflow

### After Testing Phase

When you're done testing and ready to import real data:

1. **Option A: Selective Cleanup** (Recommended)
   - Clean up specific test data types you don't want
   - Keep any manually curated data
   - Example: Clean up YouTube data but keep books

2. **Option B: Complete Reset**
   - Use the "Nuclear Option" to delete all media
   - Topics and Genres are preserved
   - Good for a fresh start

### During Testing

- Use individual cleanup endpoints to remove specific test data
- Clean up orphaned topics/genres periodically to keep the database tidy
- Use mixlist cleanup to reset playlists without losing media items

## Safety Features

All cleanup operations include:

- ✅ Confirmation dialogs (in UI)
- ✅ Detailed logging
- ✅ Transaction support (all-or-nothing)
- ✅ Count of deleted items in response
- ✅ Proper cascade handling

## Important Notes

⚠️ **These operations are PERMANENT and cannot be undone**

- Always backup your database before major cleanup operations
- Test cleanup operations on a copy of your database first
- The "Nuclear Option" is extremely destructive - use with caution
- Topics and Genres are preserved to maintain your taxonomy

## Support

If you encounter issues with cleanup operations:

1. Check the backend logs for detailed error messages
2. Verify your database connection
3. Ensure no other processes are accessing the database
4. Check for foreign key constraints that might prevent deletion

## Files

- `cleanup-scripts.sql` - Raw SQL scripts for direct database execution
- `README-CLEANUP.md` - This documentation
- Backend: `MyMediaVerse.Web.API/Controllers/DevController.cs` - API endpoints
- Frontend: `frontend/src/components/CleanupManagementPage.jsx` - UI page

