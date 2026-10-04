import { Box, Typography, TextField } from '@mui/material';
import { fieldSx, sectionHeadingSx } from '@/shared/form/styles';

const formatDate = (v) => (v ? new Date(v).toLocaleDateString() : '');

// The values each type's source owns, as [label, value] rows.
function rowsFor(m, lastSyncedAt) {
  switch (m.mediaType) {
    case 'Movie':
      return [['TMDB ID', m.tmdbId], ['IMDb ID', m.imdbId], ['TMDB Rating', m.tmdbRating]];
    case 'TVShow':
      return [['TMDB ID', m.tmdbId], ['TMDB Rating', m.tmdbRating]];
    case 'Podcast':
      // An episode's parent series already shows, locked, in the podcast fields.
      if (m.podcastType === 'Episode') return [];
      return [
        ['Apple Podcasts ID', m.applePodcastsId],
        ['Language', m.language],
        ['Last Synced', formatDate(m.lastSyncDate)],
      ];
    case 'Video':
      return [['Channel', m.channel?.title]];
    case 'Book':
      return [['Average Rating', m.averageRating], ['Original Publication Year', m.originalPublicationYear]];
    case 'Website':
      return [['Domain', m.domain], ['Last Checked', formatDate(m.lastCheckedDate)]];
    case 'Article':
      return [
        ['Author', m.author],
        ['Publication', m.publication],
        ['Reading Progress', m.readingProgress != null ? `${m.readingProgress}%` : ''],
      ];
    case 'Channel':
    case 'Playlist':
      return [['Last Synced', formatDate(lastSyncedAt)]];
    default:
      return [];
  }
}

// Read-only values filled in by the item's source (TMDB, the podcast feed, YouTube,
// Readwise, ...). Shown for reference on the edit form and never submitted.
function ManagedBySourceFields({ mediaItem, lastSyncedAt }) {
  if (!mediaItem) return null;
  const rows = rowsFor(mediaItem, lastSyncedAt).filter(([, value]) => value !== null && value !== undefined && value !== '');
  if (rows.length === 0) return null;

  return (
    <Box sx={{ mt: 3, mb: 2 }} data-testid="managed-by-source">
      <Typography variant="h6" sx={{ ...sectionHeadingSx, mb: 0.5 }}>
        Managed by source
      </Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        These values come from the item&apos;s source and can&apos;t be edited here.
      </Typography>
      {rows.map(([label, value]) => (
        <TextField key={label} label={label} value={String(value)} disabled variant="outlined" fullWidth margin="normal" sx={fieldSx} />
      ))}
    </Box>
  );
}

export default ManagedBySourceFields;
