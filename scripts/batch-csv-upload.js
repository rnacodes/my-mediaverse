/**
 * Batch CSV Upload Script for MyMediaVerse
 * 
 * This script allows you to upload large CSV files in smaller batches
 * to avoid timeouts and make the process more manageable.
 * 
 * Usage:
 *   node scripts/batch-csv-upload.js <path-to-csv> [options]
 * 
 * Options:
 *   --batch-size <number>   Number of rows per batch (default: 10)
 *   --delay <ms>            Delay between batches in milliseconds (default: 1000)
 *   --api-url <url>         API base URL (default: http://localhost:5033)
 *   --auth-token <token>    Authentication token (required if using auth)
 * 
 * Example:
 *   node scripts/batch-csv-upload.js my-books.csv --batch-size 10 --delay 2000 --auth-token "your-jwt-token"
 */

const fs = require('fs');
const path = require('path');
const https = require('https');
const http = require('http');

// Parse command line arguments
function parseArgs() {
    const args = process.argv.slice(2);
    const config = {
        csvFile: null,
        batchSize: 10,
        delay: 1000,
        apiUrl: 'http://localhost:5033',
        authToken: null
    };

    for (let i = 0; i < args.length; i++) {
        const arg = args[i];
        
        if (arg === '--batch-size' && args[i + 1]) {
            config.batchSize = parseInt(args[i + 1]);
            i++;
        } else if (arg === '--delay' && args[i + 1]) {
            config.delay = parseInt(args[i + 1]);
            i++;
        } else if (arg === '--api-url' && args[i + 1]) {
            config.apiUrl = args[i + 1];
            i++;
        } else if (arg === '--auth-token' && args[i + 1]) {
            config.authToken = args[i + 1];
            i++;
        } else if (!arg.startsWith('--') && !config.csvFile) {
            config.csvFile = arg;
        }
    }

    if (!config.csvFile) {
        console.error('Error: CSV file path is required');
        console.log('\nUsage: node batch-csv-upload.js <path-to-csv> [options]');
        process.exit(1);
    }

    if (!fs.existsSync(config.csvFile)) {
        console.error(`Error: File not found: ${config.csvFile}`);
        process.exit(1);
    }

    return config;
}

// Split CSV into batches
function splitCsvIntoBatches(csvContent, batchSize) {
    const lines = csvContent.split('\n');
    const header = lines[0];
    const dataLines = lines.slice(1).filter(line => line.trim() !== '');
    
    const batches = [];
    for (let i = 0; i < dataLines.length; i += batchSize) {
        const batchLines = dataLines.slice(i, i + batchSize);
        const batchCsv = [header, ...batchLines].join('\n');
        batches.push({
            number: Math.floor(i / batchSize) + 1,
            startRow: i + 1,
            endRow: Math.min(i + batchSize, dataLines.length),
            content: batchCsv,
            rowCount: batchLines.length
        });
    }
    
    return batches;
}

// Create a temporary CSV file for a batch
function createTempCsvFile(batchContent, batchNumber) {
    const tempDir = path.join(__dirname, 'temp-batches');
    if (!fs.existsSync(tempDir)) {
        fs.mkdirSync(tempDir, { recursive: true });
    }
    
    const tempFile = path.join(tempDir, `batch-${batchNumber}.csv`);
    fs.writeFileSync(tempFile, batchContent);
    return tempFile;
}

// Upload a batch using native http/https
function uploadBatch(config, batchFile, batchInfo) {
    return new Promise((resolve, reject) => {
        const boundary = '----WebKitFormBoundary' + Math.random().toString(36).substring(2);
        const fileContent = fs.readFileSync(batchFile);
        const fileName = path.basename(batchFile);
        
        // Build multipart form data
        let body = '';
        body += `--${boundary}\r\n`;
        body += `Content-Disposition: form-data; name="file"; filename="${fileName}"\r\n`;
        body += 'Content-Type: text/csv\r\n\r\n';
        
        const header = Buffer.from(body, 'utf8');
        const footer = Buffer.from(`\r\n--${boundary}--\r\n`, 'utf8');
        const bodyBuffer = Buffer.concat([header, fileContent, footer]);
        
        const url = new URL(`${config.apiUrl}/api/upload/csv`);
        const protocol = url.protocol === 'https:' ? https : http;
        
        const options = {
            hostname: url.hostname,
            port: url.port || (url.protocol === 'https:' ? 443 : 80),
            path: url.pathname,
            method: 'POST',
            headers: {
                'Content-Type': `multipart/form-data; boundary=${boundary}`,
                'Content-Length': bodyBuffer.length,
                ...(config.authToken && { 'Authorization': `Bearer ${config.authToken}` })
            }
        };
        
        const req = protocol.request(options, (res) => {
            let data = '';
            
            res.on('data', (chunk) => {
                data += chunk;
            });
            
            res.on('end', () => {
                try {
                    const response = JSON.parse(data);
                    if (res.statusCode >= 200 && res.statusCode < 300) {
                        resolve(response);
                    } else {
                        reject(new Error(`HTTP ${res.statusCode}: ${response.error || response.message || data}`));
                    }
                } catch (e) {
                    if (res.statusCode >= 200 && res.statusCode < 300) {
                        resolve({ message: 'Upload successful', rawResponse: data });
                    } else {
                        reject(new Error(`Failed to parse response: ${data}`));
                    }
                }
            });
        });
        
        req.on('error', (error) => {
            reject(error);
        });
        
        req.write(bodyBuffer);
        req.end();
    });
}

// Sleep utility
function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// Main execution
async function main() {
    const config = parseArgs();
    
    console.log('='.repeat(60));
    console.log('Batch CSV Upload for MyMediaVerse');
    console.log('='.repeat(60));
    console.log(`CSV File:       ${config.csvFile}`);
    console.log(`Batch Size:     ${config.batchSize} rows`);
    console.log(`Delay:          ${config.delay}ms`);
    console.log(`API URL:        ${config.apiUrl}`);
    console.log(`Auth:           ${config.authToken ? 'Yes (token provided)' : 'No'}`);
    console.log('='.repeat(60));
    console.log('');
    
    // Read and split CSV
    console.log('Reading CSV file...');
    const csvContent = fs.readFileSync(config.csvFile, 'utf8');
    const batches = splitCsvIntoBatches(csvContent, config.batchSize);
    
    console.log(`Total rows to process: ${batches.reduce((sum, b) => sum + b.rowCount, 0)}`);
    console.log(`Split into ${batches.length} batches`);
    console.log('');
    
    // Track results
    const results = {
        totalBatches: batches.length,
        successfulBatches: 0,
        failedBatches: 0,
        totalSuccessful: 0,
        totalErrors: 0,
        batchResults: []
    };
    
    // Process each batch
    for (const batch of batches) {
        console.log(`Processing batch ${batch.number}/${batches.length} (rows ${batch.startRow}-${batch.endRow})...`);
        
        const tempFile = createTempCsvFile(batch.content, batch.number);
        
        try {
            const response = await uploadBatch(config, tempFile, batch);
            
            results.successfulBatches++;
            results.totalSuccessful += (response.SuccessCount || response.successCount || 0);
            results.totalErrors += (response.ErrorCount || response.errorCount || 0);
            
            results.batchResults.push({
                batch: batch.number,
                success: true,
                successCount: response.SuccessCount || response.successCount || 0,
                errorCount: response.ErrorCount || response.errorCount || 0,
                message: response.Message || response.message
            });
            
            console.log(`  ✓ Success: ${response.SuccessCount || response.successCount || 0} items imported`);
            if ((response.ErrorCount || response.errorCount || 0) > 0) {
                console.log(`  ⚠ Warnings: ${response.ErrorCount || response.errorCount} errors in batch`);
                if (response.Errors && response.Errors.length > 0) {
                    response.Errors.slice(0, 3).forEach(err => console.log(`    - ${err}`));
                    if (response.Errors.length > 3) {
                        console.log(`    ... and ${response.Errors.length - 3} more errors`);
                    }
                }
            }
            
        } catch (error) {
            results.failedBatches++;
            results.batchResults.push({
                batch: batch.number,
                success: false,
                error: error.message
            });
            
            console.log(`  ✗ Failed: ${error.message}`);
        }
        
        // Clean up temp file
        fs.unlinkSync(tempFile);
        
        // Wait before next batch (except for the last one)
        if (batch.number < batches.length) {
            console.log(`  Waiting ${config.delay}ms before next batch...`);
            await sleep(config.delay);
            console.log('');
        }
    }
    
    // Clean up temp directory
    const tempDir = path.join(__dirname, 'temp-batches');
    if (fs.existsSync(tempDir)) {
        fs.rmdirSync(tempDir);
    }
    
    // Print summary
    console.log('');
    console.log('='.repeat(60));
    console.log('UPLOAD SUMMARY');
    console.log('='.repeat(60));
    console.log(`Total Batches:        ${results.totalBatches}`);
    console.log(`Successful Batches:   ${results.successfulBatches}`);
    console.log(`Failed Batches:       ${results.failedBatches}`);
    console.log(`Total Items Imported: ${results.totalSuccessful}`);
    console.log(`Total Errors:         ${results.totalErrors}`);
    console.log('='.repeat(60));
    
    if (results.failedBatches > 0) {
        console.log('\nFailed Batches:');
        results.batchResults
            .filter(r => !r.success)
            .forEach(r => {
                console.log(`  Batch ${r.batch}: ${r.error}`);
            });
    }
    
    console.log('\nDone!');
}

// Run the script
main().catch(error => {
    console.error('\nFatal Error:', error.message);
    process.exit(1);
});











