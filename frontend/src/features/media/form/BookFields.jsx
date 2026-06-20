import { Controller, useFormContext } from 'react-hook-form';
import { Box, Typography, MenuItem, FormControlLabel, Checkbox } from '@mui/material';
import { ControlledTextField, ControlledSelect } from '@/shared/form/controls';
import { sectionHeadingSx, choiceLabelSx } from '@/shared/form/styles';

function BookFields() {
  const { control } = useFormContext();
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        Book Details
      </Typography>

      <ControlledTextField
        name="author"
        label="Author"
        placeholder="Enter author name..."
        variant="outlined"
        fullWidth
        required
        margin="normal"
      />

      <ControlledTextField
        name="isbn"
        label="ISBN"
        placeholder="978-0123456789"
        variant="outlined"
        fullWidth
        margin="normal"
      />

      <ControlledTextField
        name="asin"
        label="ASIN"
        placeholder="B0010SKUYM"
        variant="outlined"
        fullWidth
        margin="normal"
      />

      <ControlledTextField
        name="goodreadsRating"
        label="Goodreads Rating (1-5)"
        placeholder="e.g., 4.5"
        variant="outlined"
        fullWidth
        margin="normal"
        type="number"
        inputProps={{ min: 1, max: 5, step: 0.1 }}
        helperText="Will auto-convert to MMV rating if not set manually"
      />

      <ControlledTextField
        name="publisher"
        label="Publisher"
        placeholder="Publisher name..."
        variant="outlined"
        fullWidth
        margin="normal"
      />

      <ControlledTextField
        name="yearPublished"
        label="Year Published"
        placeholder="e.g., 2014"
        variant="outlined"
        fullWidth
        margin="normal"
        type="number"
      />

      <ControlledTextField
        name="dateRead"
        label="Date Read"
        type="date"
        variant="outlined"
        fullWidth
        margin="normal"
        InputLabelProps={{ shrink: true }}
      />

      <ControlledSelect name="format" label="Format">
        <MenuItem value="Digital">Digital</MenuItem>
        <MenuItem value="Physical">Physical</MenuItem>
      </ControlledSelect>

      <Controller
        name="partOfSeries"
        control={control}
        render={({ field }) => (
          <FormControlLabel
            control={<Checkbox checked={!!field.value} onChange={(e) => field.onChange(e.target.checked)} />}
            label="Part of Series"
            sx={{ mt: 1, ...choiceLabelSx }}
          />
        )}
      />

      <ControlledTextField
        name="myReview"
        label="My Review"
        placeholder="Your review or thoughts..."
        variant="outlined"
        fullWidth
        multiline
        rows={4}
        margin="normal"
      />
    </Box>
  );
}

export default BookFields;
