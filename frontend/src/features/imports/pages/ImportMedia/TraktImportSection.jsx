import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Button, Box, Typography,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { LiveTv, ExpandMore } from '@mui/icons-material';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_SECTION_BLOCKED } from '@/features/demo/demoMessages';
import AttributionBadge from '@/shared/AttributionBadge';

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
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                <AttributionBadge provider="trakt" sx={{ mb: 2 }} />
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
