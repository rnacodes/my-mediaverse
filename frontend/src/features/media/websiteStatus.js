// Link health as recorded by the website link check: the last HTTP status, where 0 means the
// host never answered and null means the link has not been checked yet.

export function isBrokenLink(status) {
  return status !== null && status !== undefined && (status === 0 || status >= 400);
}

export function describeLinkStatus(status) {
  if (status === null || status === undefined) return null;
  if (status === 0) return { label: 'Unreachable', tone: 'error' };
  if (status >= 400) return { label: `Broken (${status})`, tone: 'error' };
  if (status >= 300) return { label: `Redirects (${status})`, tone: 'warning' };
  return { label: `Reachable (${status})`, tone: 'success' };
}
