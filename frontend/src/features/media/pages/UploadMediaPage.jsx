import { Container, Box, Typography, Button, Paper } from '@mui/material';
import { Book, Language } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_IMPORT_BLOCKED } from '@/features/demo/demoMessages';
import BookmarkFileImport from '@/features/imports/pages/WebsiteImport/BookmarkFileImport';
import { PROFILE_WIDTH_SX } from '@/features/imports/pages/WebsiteImport/importPageStyles';
import CsvUploadSection from './CsvUploadSection';
import PodcastOpmlImportSection from './PodcastOpmlImportSection';

function UploadMediaPage() {
    const navigate = useNavigate();

    return (
        <Container maxWidth={false} disableGutters sx={{ py: 4, ...PROFILE_WIDTH_SX }}>
            <Typography variant="h4" component="h1" gutterBottom>
                Bulk Upload
            </Typography>

            <Typography variant="body1" color="text.secondary" paragraph>
                Add many items at once from a file: a CSV of books, movies, TV shows or videos, a Goodreads
                export, a podcast subscription list, or a bookmarks file.
            </Typography>

            <CsvUploadSection />

            <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
                <Box sx={{ textAlign: 'center' }}>
                    <Book sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                    <Typography variant="h6" gutterBottom>
                        Books from Goodreads
                    </Typography>
                    <Typography variant="body2" color="text.secondary" paragraph>
                        Import your entire Goodreads library from its CSV export.
                    </Typography>
                    <DemoWriteGuard title={DEMO_IMPORT_BLOCKED}>
                        <Button
                            variant="contained"
                            onClick={() => navigate('/upload-goodreads')}
                            startIcon={<Book />}
                        >
                            Import Goodreads Library
                        </Button>
                    </DemoWriteGuard>
                </Box>
            </Paper>

            <PodcastOpmlImportSection />

            <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
                <Box sx={{ textAlign: 'center', mb: 3 }}>
                    <Language sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                    <Typography variant="h6" gutterBottom>
                        Websites from a Bookmarks File
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                        Upload a bookmarks <strong>.html</strong> file exported from a browser or bookmark
                        manager. Folders and tags can become topics, and a site already in the library is skipped.
                    </Typography>
                </Box>
                <BookmarkFileImport compact />
            </Paper>

            <Box sx={{ textAlign: 'center', mt: 4 }}>
                <Button
                    variant="outlined"
                    onClick={() => navigate(-1)}
                    sx={{
                        px: 4,
                        py: 1.5,
                        fontSize: '16px',
                        fontWeight: 'bold',
                        borderColor: 'rgba(255, 255, 255, 0.7)',
                        color: 'text.primary',
                        '&:hover': {
                            borderColor: 'rgba(255, 255, 255, 1)',
                            backgroundColor: 'rgba(255, 255, 255, 0.05)'
                        }
                    }}
                >
                    Go Back
                </Button>
            </Box>
        </Container>
    );
}

export default UploadMediaPage;
