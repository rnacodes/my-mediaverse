import { Box, Typography, MenuItem } from '@mui/material';
import { ControlledTextField, ControlledSelect } from '@/shared/form/controls';
import { sectionHeadingSx } from '@/shared/form/styles';

function VideoFields() {
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        Video Details
      </Typography>

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
