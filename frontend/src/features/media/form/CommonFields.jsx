import { useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import {
  TextField, Typography, Box, Button, Chip, Autocomplete,
  FormControl, InputLabel, Select, MenuItem, RadioGroup, FormControlLabel, Radio,
} from '@mui/material';
import { useTopicSearch, useGenreSearch } from '@/hooks/useTopicGenre';
import { useUploadThumbnail } from '@/hooks/useUpload';
import { ControlledTextField } from '@/shared/form/controls';
import { fieldSx, selectFormSx } from '@/shared/form/styles';

// Types that keep no ownership status.
const NO_OWNERSHIP_TYPES = ['Video', 'Website', 'Article', 'Channel', 'Playlist'];

// lockedFields names the source-owned inputs to disable (title, link, description,
// thumbnail, status). Their values stay in the form, so they are saved back unchanged.
function CommonFields({ lockMediaType = false, lockedFields = [] }) {
  const isLocked = (name) => lockedFields.includes(name);
  const { control, watch, setValue, formState: { errors } } = useFormContext();
  const status = watch('status');
  const thumbnail = watch('thumbnail');
  const mediaType = watch('mediaType');

  // Local inputs that drive the topic/genre search queries.
  const [genreInput, setGenreInput] = useState('');
  const [topicInput, setTopicInput] = useState('');
  const [thumbnailFile, setThumbnailFile] = useState(null);

  const genreSuggestions = useGenreSearch(genreInput).data ?? [];
  const topicSuggestions = useTopicSearch(topicInput).data ?? [];
  const uploadThumbnail = useUploadThumbnail();

  const handleThumbnailUpload = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    setThumbnailFile(file);
    uploadThumbnail.mutate(file, {
      onSuccess: (data) => setValue('thumbnail', data.url),
      onError: (error) => {
        console.error('Error uploading thumbnail:', error);
        alert('Failed to upload thumbnail. Please try again.');
        setThumbnailFile(null);
      },
    });
  };

  return (
    <>
      {/* Title */}
      <Typography variant="h5" sx={{ fontSize: '20px', fontWeight: 'bold', mb: 1, color: '#ffffff' }}>
        Title
      </Typography>
      <Controller
        name="title"
        control={control}
        render={({ field }) => (
          <TextField
            {...field}
            placeholder="Enter media title..."
            variant="outlined"
            fullWidth
            required
            disabled={isLocked('title')}
            margin="normal"
            sx={{
              mb: 3,
              '& .MuiInputBase-input': { fontSize: '16px' },
              '& .MuiInputBase-input::placeholder': { color: '#ffffff', opacity: 1 },
            }}
          />
        )}
      />
      {errors.title && (
        <Typography color="error" variant="body2" sx={{ mt: 1, mb: 2 }} data-testid="title-error">
          {errors.title.message}
        </Typography>
      )}

      {/* Media Type */}
      <FormControl fullWidth margin="normal" required sx={{ mb: 3, ...selectFormSx, '& .MuiSelect-select': { fontSize: '16px' } }}>
        <InputLabel id="media-type-label" data-testid="media-type-label">Media Type</InputLabel>
        <Controller
          name="mediaType"
          control={control}
          render={({ field }) => (
            <Select labelId="media-type-label" label="Media Type" data-testid="media-type-select" disabled={lockMediaType} {...field}>
              {/* Articles and Websites are added from their source (see AddedFromSourcePanel). */}
              <MenuItem value="Book">Book</MenuItem>
              <MenuItem value="Movie">Movie</MenuItem>
              <MenuItem value="Podcast">Podcast</MenuItem>
              <MenuItem value="TVShow">TV Show</MenuItem>
              <MenuItem value="Video">Video</MenuItem>
              {/* Shown on the locked edit form only, so the field can name these types. */}
              {lockMediaType && [
                <MenuItem key="Article" value="Article">Article</MenuItem>,
                <MenuItem key="Website" value="Website">Website</MenuItem>,
                <MenuItem key="Channel" value="Channel">Channel</MenuItem>,
                <MenuItem key="Playlist" value="Playlist">Playlist</MenuItem>,
              ]}
            </Select>
          )}
        />
      </FormControl>
      {errors.mediaType && (
        <Typography color="error" variant="body2" sx={{ mt: 1, mb: 2 }} data-testid="media-type-error">
          {errors.mediaType.message}
        </Typography>
      )}

      {/* Link */}
      <ControlledTextField name="link" label="Link" placeholder="https://example.com" variant="outlined" fullWidth margin="normal" disabled={isLocked('link')} sx={{ mb: 3 }} />

      {/* Description */}
      <ControlledTextField
        name="description"
        label="Description"
        placeholder="Brief description of the media..."
        variant="outlined"
        fullWidth
        multiline
        rows={3}
        margin="normal"
        disabled={isLocked('description')}
        sx={{ mb: 3 }}
      />

      {/* Status */}
      <Box sx={{ mb: 3 }}>
        <Typography variant="h6" sx={{ fontSize: '18px', fontWeight: 'bold', mb: 2, color: '#ffffff' }}>
          Status
        </Typography>
        <FormControl component="fieldset" fullWidth>
          <Controller
            name="status"
            control={control}
            render={({ field }) => (
              <RadioGroup {...field} row sx={{ gap: 2, '& .MuiFormControlLabel-label': { fontSize: '14px' } }}>
                <FormControlLabel value="Uncharted" control={<Radio size="small" />} label="Uncharted" disabled={isLocked('status')} />
                <FormControlLabel value="ActivelyExploring" control={<Radio size="small" />} label="Actively Exploring" disabled={isLocked('status')} />
                <FormControlLabel value="Completed" control={<Radio size="small" />} label="Completed" disabled={isLocked('status')} />
                <FormControlLabel value="Abandoned" control={<Radio size="small" />} label="Abandoned" disabled={isLocked('status')} />
              </RadioGroup>
            )}
          />
        </FormControl>
      </Box>

      {/* Date Completed (only when Completed) */}
      {status === 'Completed' && (
        <ControlledTextField
          name="dateCompleted"
          label="Date Completed"
          type="date"
          variant="outlined"
          fullWidth
          margin="normal"
          InputLabelProps={{ shrink: true }}
        />
      )}

      {/* Rating (only when Completed) */}
      {status === 'Completed' && (
        <FormControl fullWidth margin="normal" sx={{ mb: 3, ...selectFormSx }}>
          <InputLabel id="rating-label">Rating</InputLabel>
          <Controller
            name="rating"
            control={control}
            render={({ field }) => (
              <Select labelId="rating-label" label="Rating" {...field}>
                <MenuItem value="">None</MenuItem>
                <MenuItem value="SuperLike">Super Like</MenuItem>
                <MenuItem value="Like">Like</MenuItem>
                <MenuItem value="Neutral">Neutral</MenuItem>
                <MenuItem value="Dislike">Dislike</MenuItem>
              </Select>
            )}
          />
        </FormControl>
      )}

      {/* Ownership Status — hidden for the types that do not keep one */}
      {!NO_OWNERSHIP_TYPES.includes(mediaType) && (
        <FormControl fullWidth margin="normal" sx={{ mb: 3, ...selectFormSx }}>
          <InputLabel id="ownership-label">Ownership Status</InputLabel>
          <Controller
            name="ownershipStatus"
            control={control}
            render={({ field }) => (
              <Select labelId="ownership-label" label="Ownership Status" {...field}>
                <MenuItem value="">None</MenuItem>
                <MenuItem value="Own">Own</MenuItem>
                <MenuItem value="Rented">Rented</MenuItem>
                <MenuItem value="Streamed">Streamed</MenuItem>
              </Select>
            )}
          />
        </FormControl>
      )}

      {/* Thumbnail URL */}
      <ControlledTextField
        name="thumbnail"
        label="Thumbnail URL"
        placeholder="https://example.com/thumbnail.jpg"
        variant="outlined"
        fullWidth
        margin="normal"
        disabled={isLocked('thumbnail')}
      />

      {/* Thumbnail Upload */}
      {!isLocked('thumbnail') && (
        <Box sx={{ mb: 3 }}>
          <Typography variant="body1" sx={{ mb: 2, fontSize: '16px', fontWeight: 'bold', color: '#ffffff' }}>
            Upload Thumbnail
          </Typography>
          <Button
            variant="contained"
            color="secondary"
            component="label"
            sx={{ fontSize: '16px', fontWeight: 'bold', textTransform: 'none', py: 1.5, px: 3, borderRadius: '8px', color: '#ffffff' }}
          >
            Choose File
            <input type="file" accept="image/*" hidden onChange={handleThumbnailUpload} />
          </Button>
          {thumbnailFile && (
            <Typography variant="body2" sx={{ mt: 1, fontSize: '14px', color: '#ffffff' }}>
              Selected: {thumbnailFile.name}
            </Typography>
          )}
          {!thumbnailFile && thumbnail && (
            <Typography variant="body2" sx={{ mt: 1, fontSize: '14px', color: '#ffffff' }}>
              Current: {thumbnail}
            </Typography>
          )}
        </Box>
      )}

      {/* Genres */}
      <Box sx={{ mb: 3 }}>
        <Controller
          name="genres"
          control={control}
          render={({ field }) => (
            <Autocomplete
              multiple
              freeSolo
              options={genreSuggestions.map((o) => o.name || o.Name)}
              value={field.value}
              onChange={(_e, newValue) => field.onChange(newValue.map((g) => g.toLowerCase()))}
              onInputChange={(_e, v) => setGenreInput(v)}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => {
                  // getTagProps returns a `key`; pass it directly rather than via spread.
                  const { key, ...tagProps } = getTagProps({ index });
                  return <Chip key={key} variant="outlined" label={option} size="small" sx={{ fontSize: '12px' }} {...tagProps} />;
                })
              }
              renderInput={(params) => (
                <TextField {...params} label="Genres" placeholder="Type to search genres or add new..." variant="outlined" sx={fieldSx} />
              )}
            />
          )}
        />
      </Box>

      {/* Topics */}
      <Box sx={{ mb: 3 }}>
        <Controller
          name="topics"
          control={control}
          render={({ field }) => (
            <Autocomplete
              multiple
              freeSolo
              options={topicSuggestions.map((o) => o.name || o.Name)}
              value={field.value}
              onChange={(_e, newValue) => field.onChange(newValue.map((t) => t.toLowerCase()))}
              onInputChange={(_e, v) => setTopicInput(v)}
              renderTags={(value, getTagProps) =>
                value.map((option, index) => {
                  // getTagProps returns a `key`; pass it directly rather than via spread.
                  const { key, ...tagProps } = getTagProps({ index });
                  return <Chip key={key} variant="outlined" label={option} size="small" sx={{ fontSize: '12px' }} {...tagProps} />;
                })
              }
              renderInput={(params) => (
                <TextField {...params} label="Topics" placeholder="Type to search topics or add new..." variant="outlined" sx={fieldSx} />
              )}
            />
          )}
        />
      </Box>

      {/* Notes */}
      <ControlledTextField
        name="notes"
        label="Notes"
        placeholder="Add any notes or thoughts about this media..."
        variant="outlined"
        fullWidth
        multiline
        rows={4}
        margin="normal"
        sx={{ mb: 3 }}
      />
    </>
  );
}

export default CommonFields;
