import { Box, Typography } from '@mui/material';
import { ControlledTextField } from '@/shared/form/controls';
import { sectionHeadingSx } from '@/shared/form/styles';

function TVShowFields() {
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        TV Show Details
      </Typography>

      <ControlledTextField name="creator" label="Creator" placeholder="Enter creator name..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="cast" label="Cast" placeholder="Comma-separated main cast..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="firstAirYear" label="First Air Year" placeholder="e.g., 2008" variant="outlined" fullWidth margin="normal" type="number" />
      <ControlledTextField name="lastAirYear" label="Last Air Year" placeholder="e.g., 2013" variant="outlined" fullWidth margin="normal" type="number" />
      <ControlledTextField name="numberOfSeasons" label="Number of Seasons" placeholder="e.g., 5" variant="outlined" fullWidth margin="normal" type="number" />
      <ControlledTextField name="numberOfEpisodes" label="Number of Episodes" placeholder="e.g., 62" variant="outlined" fullWidth margin="normal" type="number" />
      <ControlledTextField name="contentRating" label="Content Rating" placeholder="e.g., TV-14" variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="tagline" label="Tagline" placeholder="Show tagline..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="homepage" label="Homepage" placeholder="https://..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="originalLanguage" label="Original Language" placeholder="e.g., en" variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="originalName" label="Original Name" placeholder="Original-language name..." variant="outlined" fullWidth margin="normal" />
    </Box>
  );
}

export default TVShowFields;
