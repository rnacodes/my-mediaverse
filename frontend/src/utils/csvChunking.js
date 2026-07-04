import Papa from 'papaparse';

/**
 * Split raw CSV text into smaller CSV chunks, each carrying the original header
 * row followed by up to `rowsPerChunk` data rows. Parsing goes through papaparse
 * so quoted fields containing commas or newlines (common in Goodreads reviews)
 * stay intact.
 *
 * @param {string} csvText - The full CSV file contents.
 * @param {number} rowsPerChunk - Max data rows per chunk (default 200).
 * @returns {Array<{ csv: string, startRow: number, endRow: number, rowCount: number }>}
 *   One entry per chunk. `startRow`/`endRow` are 1-based, inclusive data-row
 *   numbers (excluding the header) for reporting which rows a chunk covered.
 *   Returns an empty array when there are no data rows.
 */
export function splitCsvIntoChunks(csvText, rowsPerChunk = 200) {
  const size = Math.max(1, Math.floor(rowsPerChunk));
  const { data: rows } = Papa.parse(csvText, { skipEmptyLines: true });

  // Need at least a header plus one data row to have anything to import.
  if (!Array.isArray(rows) || rows.length <= 1) {
    return [];
  }

  const header = rows[0];
  const dataRows = rows.slice(1);
  const chunks = [];

  for (let i = 0; i < dataRows.length; i += size) {
    const slice = dataRows.slice(i, i + size);
    chunks.push({
      csv: Papa.unparse([header, ...slice]),
      startRow: i + 1,
      endRow: i + slice.length,
      rowCount: slice.length,
    });
  }

  return chunks;
}
