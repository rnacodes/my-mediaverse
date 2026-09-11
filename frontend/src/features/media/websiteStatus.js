// Link health as recorded by the website link check: the last HTTP status, where 0 means the
// host never answered and null means the link has not been checked yet.

// A 403 or 429 from a scraper almost always means the site blocks automated fetches.
// They are shown as blocked rather than broken.
const BLOCKED_STATUSES = new Set([403, 429]);

export function isBlockedLink(status) {
  return status !== null && status !== undefined && BLOCKED_STATUSES.has(status);
}

export function isBrokenLink(status) {
  return status !== null && status !== undefined && (status === 0 || (status >= 400 && !BLOCKED_STATUSES.has(status)));
}

export function describeLinkStatus(status) {
  if (status === null || status === undefined) return null;
  if (status === 0) return { label: 'Unreachable', tone: 'error' };
  if (BLOCKED_STATUSES.has(status)) return { label: `Blocked (${status})`, tone: 'warning' };
  if (status >= 400) return { label: `Broken (${status})`, tone: 'error' };
  if (status >= 300) return { label: `Redirects (${status})`, tone: 'warning' };
  return { label: `Reachable (${status})`, tone: 'success' };
}
