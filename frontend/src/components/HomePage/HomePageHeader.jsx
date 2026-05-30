import { Box, Typography } from '@mui/material';
import SearchBar from '@/shared/SearchBar';

// Page title plus the primary search bar. Calls onSearch with the submitted query.
const HomePageHeader = ({ onSearch }) => (
  <Box sx={{ textAlign: 'center', my: { xs: 2, sm: 3, md: 4 }, px: { xs: 1, sm: 2 } }}>
    <Typography
      variant="h1"
      sx={{
        fontSize: { xs: '2.5rem', sm: '3.5rem', md: '4rem' },
        mb: { xs: 2, sm: 3 }
      }}
    >
      My MediaVerse
    </Typography>
    <SearchBar
      placeholder="Your next adventure awaits..."
      onSearch={onSearch}
      showSuggestions={true}
    />
  </Box>
);

export default HomePageHeader;
