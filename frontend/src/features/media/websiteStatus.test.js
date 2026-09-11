import { describe, it, expect } from 'vitest';
import { isBrokenLink, isBlockedLink, describeLinkStatus } from './websiteStatus';

// The link check records the last HTTP status: null = never checked, 0 = host never answered,
// anything else = the status the page finally answered with after redirects. A 403/429 is a bot
// block (the page opens in a browser), so it is "blocked", not "broken".
describe('websiteStatus', () => {
  describe('isBrokenLink', () => {
    it.each([
      [null, false],
      [undefined, false],
      [0, true],
      [200, false],
      [301, false],
      [403, false],
      [404, true],
      [429, false],
      [500, true],
    ])('status %s → broken: %s', (status, expected) => {
      expect(isBrokenLink(status)).toBe(expected);
    });
  });

  describe('isBlockedLink', () => {
    it.each([
      [null, false],
      [0, false],
      [200, false],
      [403, true],
      [404, false],
      [429, true],
    ])('status %s → blocked: %s', (status, expected) => {
      expect(isBlockedLink(status)).toBe(expected);
    });
  });

  describe('describeLinkStatus', () => {
    it.each([
      [null, null],
      [undefined, null],
      [0, { label: 'Unreachable', tone: 'error' }],
      [200, { label: 'Reachable (200)', tone: 'success' }],
      [301, { label: 'Redirects (301)', tone: 'warning' }],
      [403, { label: 'Blocked (403)', tone: 'warning' }],
      [404, { label: 'Broken (404)', tone: 'error' }],
      [429, { label: 'Blocked (429)', tone: 'warning' }],
      [503, { label: 'Broken (503)', tone: 'error' }],
    ])('status %s → %o', (status, expected) => {
      expect(describeLinkStatus(status)).toEqual(expected);
    });
  });
});
