import { useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import { Box, Typography, FormControl, FormLabel, RadioGroup, FormControlLabel, Radio, Autocomplete, TextField } from '@mui/material';
import { usePodcastSeriesSearch } from '@/hooks/usePodcast';
import { ControlledTextField } from '@/shared/form/controls';
import { fieldSx, choiceLabelSx } from '@/shared/form/styles';

// lockType disables the Series/Episode discriminator + parent-series selector
// in edit mode, where a podcast item's kind and parent can no longer change.
function PodcastFields({ lockType = false }) {
  const { control, watch, setValue } = useFormContext();
  const podcastType = watch('podcastType');
  const durationInSeconds = watch('durationInSeconds');

  // Local input that drives the series search query (not part of submitted data).
  const [seriesInput, setSeriesInput] = useState('');
  const seriesSearch = usePodcastSeriesSearch(seriesInput);
  const seriesSuggestions = seriesSearch.data ?? [];

  const durationMinutes = durationInSeconds ? (parseInt(durationInSeconds, 10) / 60).toString() : '';
  const handleDurationChange = (e) => {
    const minutes = e.target.value;
    setValue('durationInSeconds', minutes ? (parseFloat(minutes) * 60).toString() : '');
  };

  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={{ mb: 2, fontSize: '18px', fontWeight: 'bold' }}>
        Podcast Type
      </Typography>

      <FormControl component="fieldset" fullWidth margin="normal">
        <FormLabel component="legend" sx={{ color: '#ffffff', fontSize: '14px', '&.Mui-focused': { color: '#ffffff' } }}>
          Choose podcast type:
        </FormLabel>
        <Controller
          name="podcastType"
          control={control}
          render={({ field }) => (
            <RadioGroup {...field} row sx={{ mt: 1, ...choiceLabelSx }}>
              <FormControlLabel value="Series" control={<Radio />} label="Series" disabled={lockType} />
              <FormControlLabel value="Episode" control={<Radio />} label="Episode" disabled={lockType} />
            </RadioGroup>
          )}
        />
      </FormControl>

      {podcastType === 'Series' && (
        <ControlledTextField name="publisher" label="Publisher" placeholder="Publisher name..." variant="outlined" fullWidth margin="normal" />
      )}

      {podcastType === 'Episode' && (
        <>
          <Controller
            name="selectedPodcastSeries"
            control={control}
            render={({ field }) => (
              <Autocomplete
                options={seriesSuggestions}
                getOptionLabel={(option) => option.title || option.Title || ''}
                value={field.value}
                disabled={lockType}
                onChange={(_event, newValue) => {
                  field.onChange(newValue);
                  setValue('podcastSeriesId', newValue?.id || newValue?.Id || '');
                }}
                onInputChange={(_event, newInputValue) => setSeriesInput(newInputValue)}
                renderInput={(params) => (
                  <TextField
                    {...params}
                    label="Podcast Series"
                    placeholder="Search for podcast series..."
                    variant="outlined"
                    fullWidth
                    margin="normal"
                    sx={fieldSx}
                  />
                )}
              />
            )}
          />
          <TextField
            label="Duration (Minutes)"
            placeholder="60"
            type="number"
            variant="outlined"
            fullWidth
            margin="normal"
            value={durationMinutes}
            onChange={handleDurationChange}
            sx={fieldSx}
          />
          <ControlledTextField name="episodeNumber" label="Episode Number" placeholder="e.g., 12" variant="outlined" fullWidth margin="normal" type="number" />
          <ControlledTextField name="seasonNumber" label="Season Number" placeholder="e.g., 2" variant="outlined" fullWidth margin="normal" type="number" />
          <ControlledTextField name="releaseDate" label="Release Date" type="date" variant="outlined" fullWidth margin="normal" InputLabelProps={{ shrink: true }} />
          <ControlledTextField name="audioLink" label="Audio Link" placeholder="https://...mp3" variant="outlined" fullWidth margin="normal" />
        </>
      )}
    </Box>
  );
}

export default PodcastFields;
