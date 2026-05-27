import { useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import { Box, Typography, FormControl, FormLabel, RadioGroup, FormControlLabel, Radio, Autocomplete, TextField } from '@mui/material';
import { usePodcastSeriesSearch } from '../../hooks/usePodcast';
import { fieldSx, choiceLabelSx } from '../shared/form/styles';

function PodcastFields() {
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
              <FormControlLabel value="Series" control={<Radio />} label="Series" />
              <FormControlLabel value="Episode" control={<Radio />} label="Episode" />
            </RadioGroup>
          )}
        />
      </FormControl>

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
        </>
      )}
    </Box>
  );
}

export default PodcastFields;
