import { Box, Typography } from '@mui/material';
import { ControlledTextField } from '@/shared/form/controls';
import { sectionHeadingSx } from '@/shared/form/styles';

function WebsiteFields() {
  return (
    <Box sx={{ mt: 3, mb: 2 }}>
      <Typography variant="h6" sx={sectionHeadingSx}>
        Website Details
      </Typography>

      <ControlledTextField name="rssFeedUrl" label="RSS Feed URL" placeholder="https://example.com/feed.xml" variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="author" label="Author" placeholder="Enter author name..." variant="outlined" fullWidth margin="normal" />
      <ControlledTextField name="publication" label="Publication" placeholder="Site or publication name..." variant="outlined" fullWidth margin="normal" />
    </Box>
  );
}

export default WebsiteFields;
