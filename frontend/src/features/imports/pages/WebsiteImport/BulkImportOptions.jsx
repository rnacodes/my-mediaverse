import { Box, FormControl, FormControlLabel, InputLabel, MenuItem, Select, Switch, Typography } from '@mui/material';
import TagAutocomplete from './TagAutocomplete';

const STATUSES = [
  { value: 'Uncharted', label: 'Uncharted' },
  { value: 'ActivelyExploring', label: 'Actively Exploring' },
  { value: 'Completed', label: 'Completed' },
  { value: 'Abandoned', label: 'Abandoned' },
];

/**
 * How a bookmark file or URL list becomes websites (defaults in DEFAULT_IMPORT_OPTIONS).
 * `showFolderOptions` is false for pasted lists, which carry no folders or tags.
 */
function BulkImportOptions({ value, onChange, disabled = false, showFolderOptions = true }) {
  const set = (patch) => onChange({ ...value, ...patch });

  return (
    <Box>
      <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 1 }}>
        Options
      </Typography>

      {showFolderOptions && (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 2 }}>
          <FormControlLabel
            control={<Switch checked={value.foldersAsTopics} onChange={(e) => set({ foldersAsTopics: e.target.checked })} disabled={disabled} />}
            label="Folders become topics"
          />
          <FormControlLabel
            control={<Switch checked={value.tagsAsTopics} onChange={(e) => set({ tagsAsTopics: e.target.checked })} disabled={disabled} />}
            label="Tags become topics"
          />
        </Box>
      )}

      <FormControl size="small" sx={{ minWidth: 220, mb: 2 }} disabled={disabled}>
        <InputLabel id="bulk-import-status-label">Status for new websites</InputLabel>
        <Select
          labelId="bulk-import-status-label"
          label="Status for new websites"
          value={value.defaultStatus}
          onChange={(e) => set({ defaultStatus: e.target.value })}
        >
          {STATUSES.map((s) => (
            <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>
          ))}
        </Select>
      </FormControl>

      <TagAutocomplete
        kind="topic"
        label="Add these topics to every website"
        value={value.extraTopics}
        onChange={(extraTopics) => set({ extraTopics })}
        disabled={disabled}
        placeholder="e.g. imported, to-sort"
      />
      <TagAutocomplete
        kind="genre"
        label="Add these genres to every website"
        value={value.extraGenres}
        onChange={(extraGenres) => set({ extraGenres })}
        disabled={disabled}
        placeholder="Optional"
      />
    </Box>
  );
}

export default BulkImportOptions;
