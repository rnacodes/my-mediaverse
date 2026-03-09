import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Button, Box, Typography,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { LiveTv, ExpandMore, OpenInNew } from '@mui/icons-material';

function TraktImportSection({ expanded, onAccordionChange }) {
    const navigate = useNavigate();

    return (
        <Accordion
            expanded={expanded === 'trakt'}
            onChange={onAccordionChange('trakt')}
            sx={{ mb: 2 }}
        >
            <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls="trakt-content"
                id="trakt-header"
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                    <LiveTv />
                    <Typography variant="h6">
                        Watch History from Trakt
                    </Typography>
                    <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                            Powered by
                        </Typography>
                        <Button
                            variant="text"
                            size="small"
                            href="https://trakt.tv"
                            target="_blank"
                            rel="noopener noreferrer"
                            endIcon={<OpenInNew fontSize="small" />}
                            sx={{
                                minWidth: 'auto',
                                textTransform: 'none',
                                color: '#ed1c24',
                                '&:hover': { backgroundColor: 'transparent', textDecoration: 'underline' }
                            }}
                            onClick={(e) => e.stopPropagation()}
                        >
                            Trakt
                        </Button>
                    </Box>
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <Typography variant="body1" paragraph>
                        Sync your movie and TV show data from Trakt:
                    </Typography>
                    <Box component="ul" sx={{ mb: 2, pl: 3 }}>
                        <li>
                            <Typography variant="body2">
                                Import watch history with play counts and dates
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Sync your watchlist as new items to explore
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Import ratings (1-10 scale mapped to your preferences)
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Track individual TV show episodes
                            </Typography>
                        </li>
                    </Box>
                    <Button
                        variant="contained"
                        startIcon={<LiveTv />}
                        onClick={() => navigate('/trakt-sync')}
                        sx={{ mt: 2 }}
                    >
                        Go to Trakt Sync
                    </Button>
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default TraktImportSection;
