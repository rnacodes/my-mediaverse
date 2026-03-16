#!/usr/bin/env python3
"""
Batch CSV Upload Script for MyMediaVerse

This script allows you to upload large CSV files in smaller batches
to avoid timeouts and make the process more manageable.

Usage:
    python scripts/batch-csv-upload.py <path-to-csv> [options]

Options:
    --batch-size <number>   Number of rows per batch (default: 10)
    --delay <seconds>       Delay between batches in seconds (default: 1)
    --api-url <url>         API base URL (default: http://localhost:5033)
    --auth-token <token>    Authentication token (required if using auth)

Example:
    python scripts/batch-csv-upload.py my-books.csv --batch-size 10 --delay 2 --auth-token "your-jwt-token"
"""

import argparse
import csv
import os
import sys
import time
import requests
from pathlib import Path
from typing import List, Dict, Any


def parse_args() -> argparse.Namespace:
    """Parse command line arguments."""
    parser = argparse.ArgumentParser(
        description='Batch CSV Upload for MyMediaVerse',
        formatter_class=argparse.RawDescriptionHelpFormatter
    )
    
    parser.add_argument('csv_file', help='Path to the CSV file to upload')
    parser.add_argument('--batch-size', type=int, default=10,
                       help='Number of rows per batch (default: 10)')
    parser.add_argument('--delay', type=float, default=1.0,
                       help='Delay between batches in seconds (default: 1)')
    parser.add_argument('--api-url', default='http://localhost:5033',
                       help='API base URL (default: http://localhost:5033)')
    parser.add_argument('--auth-token', help='Authentication token')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.csv_file):
        print(f"Error: File not found: {args.csv_file}")
        sys.exit(1)
    
    return args


def split_csv_into_batches(csv_file: str, batch_size: int) -> List[Dict[str, Any]]:
    """Split CSV file into batches."""
    batches = []
    
    with open(csv_file, 'r', encoding='utf-8') as f:
        reader = csv.reader(f)
        header = next(reader)
        rows = list(reader)
    
    # Filter out empty rows
    rows = [row for row in rows if any(cell.strip() for cell in row)]
    
    total_rows = len(rows)
    
    for i in range(0, total_rows, batch_size):
        batch_rows = rows[i:i + batch_size]
        batches.append({
            'number': (i // batch_size) + 1,
            'start_row': i + 1,
            'end_row': min(i + batch_size, total_rows),
            'header': header,
            'rows': batch_rows,
            'row_count': len(batch_rows)
        })
    
    return batches


def create_temp_csv_file(batch: Dict[str, Any], batch_number: int) -> str:
    """Create a temporary CSV file for a batch."""
    temp_dir = Path(__file__).parent / 'temp-batches'
    temp_dir.mkdir(exist_ok=True)
    
    temp_file = temp_dir / f'batch-{batch_number}.csv'
    
    with open(temp_file, 'w', newline='', encoding='utf-8') as f:
        writer = csv.writer(f)
        writer.writerow(batch['header'])
        writer.writerows(batch['rows'])
    
    return str(temp_file)


def upload_batch(api_url: str, batch_file: str, auth_token: str = None) -> Dict[str, Any]:
    """Upload a batch to the API."""
    url = f"{api_url}/api/upload/csv"
    
    headers = {}
    if auth_token:
        headers['Authorization'] = f'Bearer {auth_token}'
    
    with open(batch_file, 'rb') as f:
        files = {'file': (os.path.basename(batch_file), f, 'text/csv')}
        response = requests.post(url, files=files, headers=headers)
    
    response.raise_for_status()
    return response.json()


def main():
    """Main execution."""
    args = parse_args()
    
    print('=' * 60)
    print('Batch CSV Upload for MyMediaVerse')
    print('=' * 60)
    print(f"CSV File:       {args.csv_file}")
    print(f"Batch Size:     {args.batch_size} rows")
    print(f"Delay:          {args.delay}s")
    print(f"API URL:        {args.api_url}")
    print(f"Auth:           {'Yes (token provided)' if args.auth_token else 'No'}")
    print('=' * 60)
    print()
    
    # Read and split CSV
    print('Reading CSV file...')
    batches = split_csv_into_batches(args.csv_file, args.batch_size)
    
    total_rows = sum(b['row_count'] for b in batches)
    print(f"Total rows to process: {total_rows}")
    print(f"Split into {len(batches)} batches")
    print()
    
    # Track results
    results = {
        'total_batches': len(batches),
        'successful_batches': 0,
        'failed_batches': 0,
        'total_successful': 0,
        'total_errors': 0,
        'batch_results': []
    }
    
    # Process each batch
    for batch in batches:
        print(f"Processing batch {batch['number']}/{len(batches)} "
              f"(rows {batch['start_row']}-{batch['end_row']})...")
        
        temp_file = create_temp_csv_file(batch, batch['number'])
        
        try:
            response = upload_batch(args.api_url, temp_file, args.auth_token)
            
            results['successful_batches'] += 1
            success_count = response.get('SuccessCount') or response.get('successCount') or 0
            error_count = response.get('ErrorCount') or response.get('errorCount') or 0
            
            results['total_successful'] += success_count
            results['total_errors'] += error_count
            
            results['batch_results'].append({
                'batch': batch['number'],
                'success': True,
                'success_count': success_count,
                'error_count': error_count,
                'message': response.get('Message') or response.get('message')
            })
            
            print(f"  ✓ Success: {success_count} items imported")
            if error_count > 0:
                print(f"  ⚠ Warnings: {error_count} errors in batch")
                errors = response.get('Errors', [])
                for err in errors[:3]:
                    print(f"    - {err}")
                if len(errors) > 3:
                    print(f"    ... and {len(errors) - 3} more errors")
            
        except requests.exceptions.RequestException as error:
            results['failed_batches'] += 1
            results['batch_results'].append({
                'batch': batch['number'],
                'success': False,
                'error': str(error)
            })
            
            print(f"  ✗ Failed: {error}")
        
        except Exception as error:
            results['failed_batches'] += 1
            results['batch_results'].append({
                'batch': batch['number'],
                'success': False,
                'error': str(error)
            })
            
            print(f"  ✗ Failed: {error}")
        
        finally:
            # Clean up temp file
            if os.path.exists(temp_file):
                os.remove(temp_file)
        
        # Wait before next batch (except for the last one)
        if batch['number'] < len(batches):
            print(f"  Waiting {args.delay}s before next batch...")
            time.sleep(args.delay)
            print()
    
    # Clean up temp directory
    temp_dir = Path(__file__).parent / 'temp-batches'
    if temp_dir.exists():
        try:
            temp_dir.rmdir()
        except OSError:
            pass  # Directory not empty or doesn't exist
    
    # Print summary
    print()
    print('=' * 60)
    print('UPLOAD SUMMARY')
    print('=' * 60)
    print(f"Total Batches:        {results['total_batches']}")
    print(f"Successful Batches:   {results['successful_batches']}")
    print(f"Failed Batches:       {results['failed_batches']}")
    print(f"Total Items Imported: {results['total_successful']}")
    print(f"Total Errors:         {results['total_errors']}")
    print('=' * 60)
    
    if results['failed_batches'] > 0:
        print('\nFailed Batches:')
        for result in results['batch_results']:
            if not result['success']:
                print(f"  Batch {result['batch']}: {result['error']}")
    
    print('\nDone!')


if __name__ == '__main__':
    try:
        main()
    except KeyboardInterrupt:
        print('\n\nUpload interrupted by user.')
        sys.exit(1)
    except Exception as error:
        print(f'\nFatal Error: {error}')
        sys.exit(1)











