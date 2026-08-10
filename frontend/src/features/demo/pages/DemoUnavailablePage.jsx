import { Link } from 'react-router-dom';
import { Box, Button, Container, Paper, Typography } from '@mui/material';
import { LockOutlined } from '@mui/icons-material';

const DemoUnavailablePage = () => (
    <Container maxWidth="sm" sx={{ py: 8 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
            <LockOutlined sx={{ fontSize: 48, color: 'warning.main', mb: 2 }} />
            <Typography variant="h4" gutterBottom>
                Not available in Demo
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                This section manages real library data — imports, syncs, and
                administrative tools — so it&apos;s disabled in the public demo.
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
                <Button component={Link} to="/" variant="contained">
                    Back to Home
                </Button>
                <Button component={Link} to="/search" variant="contained">
                    Browse the library
                </Button>
            </Box>
        </Paper>
    </Container>
);

export default DemoUnavailablePage;
