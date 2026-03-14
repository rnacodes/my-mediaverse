import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box, Typography, Container, Divider, Snackbar, Alert
} from '@mui/material';
import WhiteOutlineButton from '../shared/WhiteOutlineButton';

// Import section components
import BookImportSection from './BookImportSection';
import TmdbImportSection from './TmdbImportSection';
import PodcastImportSection from './PodcastImportSection';
import ReadwiseImportSection from './ReadwiseImportSection';
import TraktImportSection from './TraktImportSection';
import WebsiteImportSection from './WebsiteImportSection';
import YouTubeImportSection from './YouTubeImportSection';

function ImportMediaPage() {
    const [expanded, setExpanded] = useState(false);
    const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
    const navigate = useNavigate();

    const handleAccordionChange = (panel) => (event, isExpanded) => {
        setExpanded(isExpanded ? panel : false);
    };

    const handleSnackbar = (snackbarState) => {
        setSnackbar(snackbarState);
    };

    return (
        <Container maxWidth="lg" sx={{
            '& .MuiInputLabel-root': {
                color: 'white'
            },
            '& .MuiInputLabel-root.Mui-focused': {
                color: 'white'
            }
        }}>
            <Typography variant="h4" gutterBottom sx={{ mt: 4, mb: 3 }}>
                Import Media
            </Typography>

            <Typography variant="body1" sx={{ mb: 4 }}>
                Import media from external sources into your library.
            </Typography>

            {/* Book Import Section */}
            <BookImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
            />

            {/* TMDB Movies & TV Shows Import Section */}
            <TmdbImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
            />

            {/* Podcast Import Section */}
            <PodcastImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
                onSnackbar={handleSnackbar}
            />

            {/* Readwise Import Section */}
            <ReadwiseImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
            />

            {/* Trakt Import Section */}
            <TraktImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
            />

            {/* Websites Import Section */}
            <WebsiteImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
            />

            {/* YouTube Import Section */}
            <YouTubeImportSection
                expanded={expanded}
                onAccordionChange={handleAccordionChange}
                onSnackbar={handleSnackbar}
            />

            <Divider sx={{ my: 4 }} />

            <Box sx={{ textAlign: 'center' }}>
                <WhiteOutlineButton
                    onClick={() => navigate(-1)}
                    sx={{ mr: 2 }}
                >
                    Go Back
                </WhiteOutlineButton>
            </Box>

            <Snackbar
                open={snackbar.open}
                autoHideDuration={6000}
                onClose={() => setSnackbar({ ...snackbar, open: false })}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert onClose={() => setSnackbar({ ...snackbar, open: false })} severity={snackbar.severity} sx={{ width: '100%' }}>
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Container>
    );
}

export default ImportMediaPage;
