import { Box, Typography } from '@mui/material';
import { ControlledTextField } from '@/shared/form/controls';
import { sectionHeadingSx } from '@/shared/form/styles';

// Parity with the legacy form: only Creator is surfaced. The remaining TV Show
// columns (cast, seasons, air years, etc.) had no inputs and were always posted
// as null, so they are intentionally omitted here.
function TVShowFields() {
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        TV Show Details
      </Typography>

      <ControlledTextField
        name="creator"
        label="Creator"
        placeholder="Enter creator name..."
        variant="outlined"
        fullWidth
        margin="normal"
      />
    </Box>
  );
}

export default TVShowFields;
