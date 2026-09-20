#!/bin/bash
# =============================================================================
# Podcast Enrichment Cron Job
# =============================================================================
# This script calls the podcast enrichment API endpoint to fill in missing
# metadata for podcast series that have not been enriched yet.
#
# The run spaces out its external calls and stops early when a batch has
# nothing left it can enrich.
#
# SETUP INSTRUCTIONS:
# 1. Copy this script to your DigitalOcean VM
# 2. Make it executable: chmod +x podcast-enrichment-cron.sh
# 3. Edit the configuration variables below
# 4. Test manually: ./podcast-enrichment-cron.sh
# 5. Add to crontab: crontab -e
#
# CRON EXAMPLES:
#   Run every 3 days at 5 AM:
#     0 5 */3 * * /path/to/podcast-enrichment-cron.sh >> /var/log/podcast-enrichment.log 2>&1
#
#   Run every Sunday at 2 AM:
#     0 2 * * 0 /path/to/podcast-enrichment-cron.sh >> /var/log/podcast-enrichment.log 2>&1
#
#   Run on the 1st and 15th of each month at 3 AM:
#     0 3 1,15 * * /path/to/podcast-enrichment-cron.sh >> /var/log/podcast-enrichment.log 2>&1
#
# =============================================================================

# -----------------------------------------------------------------------------
# CONFIGURATION - Edit these values
# -----------------------------------------------------------------------------

# Your API base URL (include /api)
API_URL="https://www.api.mymediaverseuniverse.com/api"

# Your JWT access token (get this from logging in)
# For long-running cron jobs, consider implementing a service account or
# using refresh tokens
API_TOKEN="YOUR_JWT_TOKEN_HERE"

# Enrichment parameters
BATCH_SIZE=25                    # Podcasts per batch (1-50)
DELAY_MS=1500                    # Delay between API calls in ms (minimum 200ms, recommend 1000+)
MAX_PODCASTS=100                 # Maximum podcasts to process per run
PAUSE_BETWEEN_BATCHES=60         # Seconds to pause between batches

# Logging
LOG_PREFIX="[PodcastEnrichment]"

# -----------------------------------------------------------------------------
# SCRIPT - Don't edit below unless you know what you're doing
# -----------------------------------------------------------------------------

echo ""
echo "=============================================="
echo "$LOG_PREFIX Starting podcast enrichment run"
echo "$LOG_PREFIX $(date '+%Y-%m-%d %H:%M:%S')"
echo "=============================================="

# Check if token is set
if [ "$API_TOKEN" = "YOUR_JWT_TOKEN_HERE" ]; then
    echo "$LOG_PREFIX ERROR: API_TOKEN not configured!"
    echo "$LOG_PREFIX Please edit this script and set your JWT token."
    exit 1
fi

# First, check the current status
echo "$LOG_PREFIX Checking enrichment status..."
STATUS_RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X GET "${API_URL}/podcastenrichment/status" \
    -H "Authorization: Bearer ${API_TOKEN}" \
    -H "Content-Type: application/json")

HTTP_CODE=$(echo "$STATUS_RESPONSE" | tail -n1)
STATUS_BODY=$(echo "$STATUS_RESPONSE" | sed '$d')

if [ "$HTTP_CODE" != "200" ]; then
    echo "$LOG_PREFIX ERROR: Failed to get status (HTTP $HTTP_CODE)"
    echo "$LOG_PREFIX Response: $STATUS_BODY"
    exit 1
fi

PODCASTS_NEEDING=$(echo "$STATUS_BODY" | grep -o '"podcastsNeedingEnrichment":[0-9]*' | cut -d':' -f2)
echo "$LOG_PREFIX Podcasts needing enrichment: $PODCASTS_NEEDING"

if [ "$PODCASTS_NEEDING" = "0" ]; then
    echo "$LOG_PREFIX No podcasts need enrichment. Exiting."
    exit 0
fi

# Run the enrichment
echo "$LOG_PREFIX Starting enrichment with:"
echo "$LOG_PREFIX   - Batch size: $BATCH_SIZE"
echo "$LOG_PREFIX   - Max podcasts: $MAX_PODCASTS"
echo "$LOG_PREFIX   - Delay: ${DELAY_MS}ms"
echo "$LOG_PREFIX   - Pause between batches: ${PAUSE_BETWEEN_BATCHES}s"
echo ""

RESPONSE=$(curl -s -w "\n%{http_code}" \
    -X POST "${API_URL}/podcastenrichment/run-all" \
    -H "Authorization: Bearer ${API_TOKEN}" \
    -H "Content-Type: application/json" \
    -d "{
        \"batchSize\": ${BATCH_SIZE},
        \"delayBetweenCallsMs\": ${DELAY_MS},
        \"maxPodcasts\": ${MAX_PODCASTS},
        \"pauseBetweenBatchesSeconds\": ${PAUSE_BETWEEN_BATCHES}
    }")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

echo "$LOG_PREFIX HTTP Response Code: $HTTP_CODE"

if [ "$HTTP_CODE" = "200" ]; then
    echo "$LOG_PREFIX Enrichment completed successfully!"
    echo "$LOG_PREFIX Response: $BODY"

    # Extract and display key metrics
    PROCESSED=$(echo "$BODY" | grep -o '"totalProcessed":[0-9]*' | cut -d':' -f2)
    ENRICHED=$(echo "$BODY" | grep -o '"totalEnriched":[0-9]*' | cut -d':' -f2)
    NOTFOUND=$(echo "$BODY" | grep -o '"totalNotFound":[0-9]*' | cut -d':' -f2)
    FAILED=$(echo "$BODY" | grep -o '"totalFailed":[0-9]*' | cut -d':' -f2)
    BATCHES=$(echo "$BODY" | grep -o '"batchesRun":[0-9]*' | cut -d':' -f2)
    REMAINING=$(echo "$BODY" | grep -o '"remainingPodcasts":[0-9]*' | cut -d':' -f2)

    echo ""
    echo "$LOG_PREFIX Summary:"
    echo "$LOG_PREFIX   - Batches run: $BATCHES"
    echo "$LOG_PREFIX   - Processed: $PROCESSED"
    echo "$LOG_PREFIX   - Enriched: $ENRICHED"
    echo "$LOG_PREFIX   - Not Found: $NOTFOUND"
    echo "$LOG_PREFIX   - Failed: $FAILED"
    echo "$LOG_PREFIX   - Remaining: $REMAINING"
else
    echo "$LOG_PREFIX ERROR: Enrichment failed!"
    echo "$LOG_PREFIX Response: $BODY"
    exit 1
fi

echo ""
echo "$LOG_PREFIX Completed at $(date '+%Y-%m-%d %H:%M:%S')"
echo "=============================================="
