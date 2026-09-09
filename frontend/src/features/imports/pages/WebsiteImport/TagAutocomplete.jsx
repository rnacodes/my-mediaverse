import { useState } from 'react';
import { Autocomplete, Box, Chip, TextField, Typography } from '@mui/material';
import { useTopicSearch, useGenreSearch } from '@/hooks/useTopicGenre';

const CHIP_COLORS = {
  topic: 'primary.main',
  genre: '#4b6aa2',
};

/**
 * Multi-value topic or genre picker: searches existing names as the user types and
 * accepts new ones. Values are stored lowercase, matching how the backend stores tags.
 */
function TagAutocomplete({ kind, label, value, onChange, disabled = false, placeholder }) {
  const [input, setInput] = useState('');
  const useSearch = kind === 'genre' ? useGenreSearch : useTopicSearch;
  const suggestions = useSearch(input).data ?? [];

  return (
    <Box sx={{ mb: 2 }}>
      <Typography variant="body2" sx={{ mb: 1, fontWeight: 'bold' }}>
        {label}
      </Typography>
      <Autocomplete
        multiple
        freeSolo
        options={suggestions.map((option) => option.name || option.Name)}
        value={value}
        onChange={(event, newValue) => onChange(newValue.map((v) => v.toLowerCase()))}
        onInputChange={(event, newInputValue) => setInput(newInputValue)}
        disabled={disabled}
        renderTags={(tagValue, getTagProps) =>
          tagValue.map((option, index) => (
            <Chip
              key={`${kind}-${option}`}
              label={option}
              size="small"
              sx={{ backgroundColor: CHIP_COLORS[kind], color: 'white', fontSize: '0.75rem' }}
              {...getTagProps({ index })}
            />
          ))
        }
        renderInput={(params) => (
          <TextField
            {...params}
            placeholder={placeholder ?? `Type to search or add ${kind}s...`}
            variant="outlined"
            inputProps={{ ...params.inputProps, 'aria-label': label }}
          />
        )}
      />
    </Box>
  );
}

export default TagAutocomplete;
