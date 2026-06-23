import { Box, Typography } from '@mui/material';
import { ControlledTextField } from '@/shared/form/controls';
import { sectionHeadingSx } from '@/shared/form/styles';

function MovieFields() {
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        Movie Details
      </Typography>

      <ControlledTextField name="director" label="Director" placeholder="Enter director name..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="cast" label="Cast" placeholder="Comma-separated main cast..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="releaseYear" label="Release Year" placeholder="e.g., 1999" variant="outlined" fullWidth margin="normal" type="number" />
      <ControlledTextField name="runtimeMinutes" label="Runtime (minutes)" placeholder="e.g., 136" variant="outlined" fullWidth margin="normal" type="number" />
      <ControlledTextField name="mpaaRating" label="MPAA Rating" placeholder="e.g., PG-13" variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="tagline" label="Tagline" placeholder="Movie tagline..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="homepage" label="Homepage" placeholder="https://..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="originalLanguage" label="Original Language" placeholder="e.g., en" variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="originalTitle" label="Original Title" placeholder="Original-language title..." variant="outlined" fullWidth margin="normal" />
    </Box>
  );
}

export default MovieFields;
