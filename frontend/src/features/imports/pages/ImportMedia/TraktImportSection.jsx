import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Button, Box, Typography,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { LiveTv, ExpandMore, OpenInNew } from '@mui/icons-material';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_SECTION_BLOCKED } from '@/features/demo/demoMessages';

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
                                color: '#FFFFFF',
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
                <div className="trakt-attribution">
      <a href="https://trakt.tv" target="_blank" rel="noopener noreferrer">
      <img src="/trakt-logo-dark.svg" alt="Trakt logo" style={{ height: '40px', width: 'auto' }} />
      </a>
      <br />
      <p style={{ fontSize: '13px', marginTop: '0px' }}>  Powered by <a href="https://trakt.tv" target="_blank" rel="noopener noreferrer" style={{ color: '#FFFFFF' }}>Trakt</a></p>
      </div>
      <Typography variant="body1" paragraph>
                        Sync your movie and TV show data from Trakt:
                    </Typography>
      <Box component="ul" sx={{ mb: 2, pl: 3 }}>
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
                    <DemoWriteGuard title={DEMO_SECTION_BLOCKED}>
                        <Button
                            variant="contained"
                            startIcon={<LiveTv />}
                            onClick={() => navigate('/trakt-sync')}
                            sx={{ mt: 2 }}
                        >
                            Go to Trakt Sync
                        </Button>
                    </DemoWriteGuard>
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default TraktImportSection;
