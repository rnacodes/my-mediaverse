import { Container, Paper, Typography, Link, Box } from '@mui/material';

// Public terms and privacy page, following YouTube's API Services policies 
function PrivacyPage() {
  return (
    <Container maxWidth="md" sx={{ py: 4 }}>
      <Paper sx={{ p: { xs: 3, sm: 4 } }}>
        <Typography variant="h4" component="h1" gutterBottom>
          Terms &amp; Privacy
        </Typography>

        <Box sx={{ mt: 3 }}>
          <Typography variant="h6" component="h2" gutterBottom>
            Terms
          </Typography>
          <Typography variant="body1" paragraph>
            My MediaVerse shows video, channel and playlist information from YouTube through YouTube API
            Services. By using those features you agree to be bound by the{' '}
            <Link href="https://www.youtube.com/t/terms" target="_blank" rel="noopener noreferrer">
              YouTube Terms of Service
            </Link>
            .
          </Typography>
        </Box>

        <Box sx={{ mt: 3 }}>
          <Typography variant="h6" component="h2" gutterBottom>
            Privacy
          </Typography>
          <Typography variant="body1" paragraph>
            My MediaVerse uses YouTube API Services. Google&apos;s privacy policy is at{' '}
            <Link href="http://www.google.com/policies/privacy" target="_blank" rel="noopener noreferrer">
              google.com/policies/privacy
            </Link>
            . The app stores only public details of the videos, channels and playlists added to the
            library (title, description, thumbnail address, length, counts and publish date) and refreshes
            them at least every 30 days. It does not sign in to, read, or change any YouTube or Google
            account, and it does not share this information with anyone.
          </Typography>
        </Box>
      </Paper>
    </Container>
  );
}

export default PrivacyPage;
