// Shared styling for the website import page and the pieces embedded elsewhere.

export const OUTLINED_BUTTON_SX = {
  borderColor: '#90caf9',
  color: '#90caf9',
  '&:hover': {
    borderColor: '#64b5f6',
    backgroundColor: 'rgba(144, 202, 249, 0.08)',
  },
  '&.Mui-disabled': {
    borderColor: 'rgba(255, 255, 255, 0.3)',
    color: 'rgba(255, 255, 255, 0.3)',
  },
};

export const PRIMARY_BUTTON_SX = {
  backgroundColor: '#362759',
  '&:hover': { backgroundColor: '#1f1a35' },
};

export const ACCENT_BUTTON_SX = {
  backgroundColor: '#7c4dff',
  '&:hover': { backgroundColor: '#651fff' },
};

export const TABS_SX = {
  mb: 3,
  '& .MuiTab-root': {
    color: 'rgba(255, 255, 255, 0.7)',
    '&.Mui-selected': { color: 'white' },
  },
  '& .MuiTabs-indicator': { backgroundColor: 'white' },
};

export const PAPER_SX = { mb: 3, p: 3, backgroundColor: 'background.paper' };

// Mirrors the backend defaults, so leaving everything alone imports the way the API would
// with no options at all.
export const DEFAULT_IMPORT_OPTIONS = {
  foldersAsTopics: true,
  tagsAsTopics: true,
  defaultStatus: 'Uncharted',
  extraTopics: [],
  extraGenres: [],
};

/**
 * The bookmarklet: on any page, sends the user here with that page's address filled in.
 */
export function buildBookmarklet(origin) {
  return `javascript:location.href='${origin}/import-website?url='+encodeURIComponent(location.href)`;
}

/**
 * Reads the message out of an API error the way the reporting contract shapes it:
 * result DTOs carry `errorMessage`, plain error bodies carry `error`, and axios
 * itself carries `message`.
 */
export function extractErrorMessage(err, fallback) {
  const data = err?.response?.data;
  if (data) {
    if (typeof data === 'string' && data.trim()) return data;
    if (data.errorMessage) return data.errorMessage;
    if (data.error) return data.error;
    if (data.message) return data.message;
  }
  // A response with no usable body (a bare 500) gets the friendly fallback; only a request that
  // never got a response (network down) shows axios's own message.
  if (!err?.response && err?.message) return err.message;
  return fallback;
}
