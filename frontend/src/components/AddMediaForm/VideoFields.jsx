import { Controller, useFormContext } from 'react-hook-form';
import { Box, Typography, FormControl, FormLabel, RadioGroup, FormControlLabel, Radio, MenuItem } from '@mui/material';
import { ControlledTextField, ControlledSelect } from './controls';
import { sectionHeadingSx, choiceLabelSx } from './styles';

function VideoFields() {
  const { control } = useFormContext();
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        Video Details
      </Typography>

      <FormControl component="fieldset" fullWidth margin="normal">
        <FormLabel component="legend" sx={{ color: '#ffffff', fontSize: '14px', '&.Mui-focused': { color: '#ffffff' } }}>
          Video Type:
        </FormLabel>
        <Controller
          name="videoType"
          control={control}
          render={({ field }) => (
            <RadioGroup {...field} sx={choiceLabelSx}>
              <FormControlLabel value="Series" control={<Radio />} label="Series" />
              <FormControlLabel value="Episode" control={<Radio />} label="Episode" />
              <FormControlLabel value="Channel" control={<Radio />} label="Channel" />
            </RadioGroup>
          )}
        />
      </FormControl>

      <ControlledSelect name="platform" label="Platform *" required>
        <MenuItem value="YouTube">YouTube</MenuItem>
        <MenuItem value="Vimeo">Vimeo</MenuItem>
        <MenuItem value="Twitch">Twitch</MenuItem>
        <MenuItem value="Instagram">Instagram</MenuItem>
        <MenuItem value="Facebook">Facebook</MenuItem>
        <MenuItem value="Other">Other</MenuItem>
      </ControlledSelect>

      <ControlledTextField
        name="channelName"
        label="Channel Name (Optional)"
        placeholder="Enter channel/creator name..."
        variant="outlined"
        fullWidth
        margin="normal"
      />

      <ControlledTextField
        name="lengthInSeconds"
        label="Length (seconds)"
        placeholder="Enter video length in seconds..."
        variant="outlined"
        fullWidth
        margin="normal"
        type="number"
      />

      <ControlledTextField
        name="externalId"
        label="External ID"
        placeholder="Enter external platform ID (optional)..."
        variant="outlined"
        fullWidth
        margin="normal"
      />
    </Box>
  );
}

export default VideoFields;
