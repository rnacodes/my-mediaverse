import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Button, Box, Typography,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { AutoStories, ExpandMore, OpenInNew } from '@mui/icons-material';

function ReadwiseImportSection({ expanded, onAccordionChange }) {
    const navigate = useNavigate();

    return (
        <Accordion
            expanded={expanded === 'readwise'}
            onChange={onAccordionChange('readwise')}
            sx={{ mb: 2 }}
        >
            <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls="readwise-content"
                id="readwise-header"
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                    <AutoStories />
                    <Typography variant="h6">
                        Articles and Highlights from Readwise
                    </Typography>
                    <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                            Powered by
                        </Typography>
                        <Button
                            variant="text"
                            size="small"
                            href="https://readwise.io"
                            target="_blank"
                            rel="noopener noreferrer"
                            endIcon={<OpenInNew fontSize="small" />}
                            sx={{
                                minWidth: 'auto',
                                textTransform: 'none',
                                color: '#ffffff',
                                '&:hover': { backgroundColor: 'transparent', textDecoration: 'underline' }
                            }}
                            onClick={(e) => e.stopPropagation()}
                        >
                            Readwise
                        </Button>
                    </Box>
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <Typography variant="body1" paragraph>
                        Sync your highlights and documents from Readwise and Readwise Reader:
                    </Typography>
                    <Box component="ul" sx={{ mb: 2, pl: 3 }}>
                        <li>
                            <Typography variant="body2">
                                Import highlights from Kindle, Reader, and more
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Sync documents saved in Readwise Reader
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Validate your API connection before syncing
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Track sync history and manage imported content
                            </Typography>
                        </li>
                    </Box>
                    <Button
                        variant="contained"
                        startIcon={<AutoStories />}
                        onClick={() => navigate('/readwise-sync')}
                        sx={{ mt: 2 }}
                    >
                        Go to Readwise Sync
                    </Button>
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default ReadwiseImportSection;
