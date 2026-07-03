import { describe, it, expect } from 'vitest';
import Papa from 'papaparse';
import { splitCsvIntoChunks } from './csvChunking';

const HEADER = 'Title,Author';
const makeCsv = (rowCount) => {
  const rows = Array.from({ length: rowCount }, (_, i) => `Book ${i + 1},Author ${i + 1}`);
  return [HEADER, ...rows].join('\n');
};

// Re-parse a chunk's CSV text back into rows for structural assertions.
const rowsOf = (csv) => Papa.parse(csv, { skipEmptyLines: true }).data;

describe('splitCsvIntoChunks', () => {
  it('splits data rows into chunks of the requested size', () => {
    const chunks = splitCsvIntoChunks(makeCsv(5), 2);
    expect(chunks).toHaveLength(3); // 2 + 2 + 1
    expect(chunks.map((c) => c.rowCount)).toEqual([2, 2, 1]);
  });

  it('repeats the header row on every chunk', () => {
    const chunks = splitCsvIntoChunks(makeCsv(3), 1);
    for (const chunk of chunks) {
      expect(rowsOf(chunk.csv)[0]).toEqual(['Title', 'Author']);
    }
  });

  it('reports 1-based inclusive data-row ranges for each chunk', () => {
    const chunks = splitCsvIntoChunks(makeCsv(5), 2);
    expect(chunks.map((c) => [c.startRow, c.endRow])).toEqual([
      [1, 2],
      [3, 4],
      [5, 5],
    ]);
  });

  it('returns a single chunk when the file fits within one', () => {
    const chunks = splitCsvIntoChunks(makeCsv(3), 200);
    expect(chunks).toHaveLength(1);
    expect(chunks[0].rowCount).toBe(3);
  });

  it('preserves quoted fields containing commas and newlines', () => {
    const csv = 'Title,Review\n"Book A","Line 1\nLine 2"\n"Book B","x, y"';
    const chunks = splitCsvIntoChunks(csv, 1);
    expect(chunks).toHaveLength(2);
    expect(rowsOf(chunks[0].csv)).toEqual([
      ['Title', 'Review'],
      ['Book A', 'Line 1\nLine 2'],
    ]);
    expect(rowsOf(chunks[1].csv)).toEqual([
      ['Title', 'Review'],
      ['Book B', 'x, y'],
    ]);
  });

  it('returns an empty array for a header-only file', () => {
    expect(splitCsvIntoChunks(HEADER)).toEqual([]);
  });

  it('returns an empty array for empty input', () => {
    expect(splitCsvIntoChunks('')).toEqual([]);
  });

  it('defaults to 200 rows per chunk', () => {
    const chunks = splitCsvIntoChunks(makeCsv(201));
    expect(chunks).toHaveLength(2);
    expect(chunks.map((c) => c.rowCount)).toEqual([200, 1]);
  });

  it('guards against a non-positive chunk size', () => {
    const chunks = splitCsvIntoChunks(makeCsv(3), 0);
    expect(chunks).toHaveLength(3);
    expect(chunks.every((c) => c.rowCount === 1)).toBe(true);
  });
});
