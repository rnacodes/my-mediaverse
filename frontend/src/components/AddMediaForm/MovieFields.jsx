import { Box, Typography } from '@mui/material';
import { ControlledTextField } from './controls';
import { sectionHeadingSx } from './styles';

// Parity with the legacy form: only Director is surfaced. The other Movie
// columns the backend accepts (cast, releaseYear, etc.) had no inputs and were
// always posted as null, so they are intentionally omitted here.
function MovieFields() {
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        Movie Details
      </Typography>

      <ControlledTextField
        name="director"
        label="Director"
        placeholder="Enter director name..."
        variant="outlined"
        fullWidth
        margin="normal"
      />
    </Box>
  );
}

export default MovieFields;
