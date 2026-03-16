# Batch CSV Upload Scripts

These scripts allow you to upload large CSV files to MyMediaVerse in smaller batches to avoid timeouts and make the process more manageable.

## Available Scripts

1. **batch-csv-upload.js** - Node.js version
2. **batch-csv-upload.py** - Python version

Both scripts provide the same functionality - choose whichever language you prefer!

## Prerequisites

### For Node.js Script
- Node.js installed (no additional packages required - uses native modules)

### For Python Script
- Python 3.6 or higher
- Install required package:
  ```bash
  pip install requests
  ```

## Usage

### Node.js Version

```bash
node scripts/batch-csv-upload.js <path-to-csv> [options]
```

**Options:**
- `--batch-size <number>` - Number of rows per batch (default: 10)
- `--delay <ms>` - Delay between batches in milliseconds (default: 1000)
- `--api-url <url>` - API base URL (default: http://localhost:5033)
- `--auth-token <token>` - Authentication token (if required)

**Example:**
```bash
node scripts/batch-csv-upload.js my-large-books.csv --batch-size 15 --delay 2000
```

### Python Version

```bash
python scripts/batch-csv-upload.py <path-to-csv> [options]
```

**Options:**
- `--batch-size <number>` - Number of rows per batch (default: 10)
- `--delay <seconds>` - Delay between batches in seconds (default: 1)
- `--api-url <url>` - API base URL (default: http://localhost:5033)
- `--auth-token <token>` - Authentication token (if required)

**Example:**
```bash
python scripts/batch-csv-upload.py my-large-books.csv --batch-size 15 --delay 2
```

## How It Works

1. **Reads your CSV file** - The script reads the entire CSV file
2. **Splits into batches** - Divides the data rows into batches of your specified size (keeping the header for each batch)
3. **Uploads sequentially** - Uploads each batch one at a time to the API endpoint
4. **Waits between batches** - Pauses between uploads to avoid overwhelming the server
5. **Reports progress** - Shows real-time progress for each batch
6. **Provides summary** - Displays final statistics when complete

## Benefits of Batch Uploading

- **Avoids timeouts** - Smaller uploads complete faster
- **Better error handling** - If one batch fails, others can still succeed
- **Progress tracking** - See exactly which rows succeeded/failed
- **Server-friendly** - Doesn't overwhelm your API server
- **Resumable** - If something fails, you know which batches completed

## Recommended Batch Sizes

- **For local development**: 10-25 rows per batch
- **For production**: 25-50 rows per batch (adjust based on your server capacity)
- **For complex media types** (with many topics/genres): 5-10 rows per batch

## Example Scenarios

### Scenario 1: Uploading 1000 books with conservative settings
```bash
node scripts/batch-csv-upload.js books.csv --batch-size 10 --delay 2000
```
This will create 100 batches, with a 2-second delay between each. Total time: ~3-4 minutes.

### Scenario 2: Fast upload of simple articles
```bash
python scripts/batch-csv-upload.py articles.csv --batch-size 25 --delay 500
```
This will process 25 articles at a time with only 0.5s delay between batches.

### Scenario 3: With authentication
```bash
node scripts/batch-csv-upload.js media.csv --auth-token "eyJhbGciOiJIUzI1NiIs..."
```

## Output Example

```
============================================================
Batch CSV Upload for MyMediaVerse
============================================================
CSV File:       books.csv
Batch Size:     10 rows
Delay:          1000ms
API URL:        http://localhost:5033
Auth:           No
============================================================

Reading CSV file...
Total rows to process: 47
Split into 5 batches

Processing batch 1/5 (rows 1-10)...
  ✓ Success: 10 items imported
  Waiting 1000ms before next batch...

Processing batch 2/5 (rows 11-20)...
  ✓ Success: 10 items imported
  Waiting 1000ms before next batch...

...

============================================================
UPLOAD SUMMARY
============================================================
Total Batches:        5
Successful Batches:   5
Failed Batches:       0
Total Items Imported: 47
Total Errors:         0
============================================================

Done!
```

## Troubleshooting

### "Authentication Required" Error
If your API requires authentication, make sure to provide the `--auth-token` parameter with your JWT token.

### "Connection Refused" Error
Make sure your API is running on the specified URL. The default is `http://localhost:5033`.

### "Invalid Media Type" Error
Check that all rows in your CSV have a valid MediaType value: Article, Book, Movie, TVShow, Video, or Website.

### Batch Partially Succeeds
The script will show you exactly which rows had errors. You can extract those rows into a new CSV and re-run the script on just those failed items.

## Tips

1. **Test with a small batch first** - Try uploading 5-10 rows to verify your CSV format is correct
2. **Adjust delay based on server load** - If your server is slow, increase the delay
3. **Monitor server resources** - Watch CPU/memory usage during upload
4. **Keep the original CSV** - Don't delete your source file until you verify the upload succeeded
5. **Check the summary** - Review the final statistics to confirm all items were imported

## Supported Media Types (via CSV)

Currently, these media types are supported for CSV upload:
- ✅ Article
- ✅ Book
- ✅ Movie
- ✅ TVShow
- ✅ Video
- ✅ Website

**Not supported via CSV:**
- ❌ Podcast (use the Import Media page instead)
- ❌ Channel, Document, Music, Other, Playlist, VideoGame (not yet implemented)

## Need Help?

If you encounter issues:
1. Check that your CSV format matches the requirements on the Upload Media page
2. Verify your API is running
3. Try a smaller batch size
4. Check the error messages for specific row issues











