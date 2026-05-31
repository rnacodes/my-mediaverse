import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Button, Box, Typography,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Language, ExpandMore } from '@mui/icons-material';

function WebsiteImportSection({ expanded, onAccordionChange }) {
    const navigate = useNavigate();

    return (
        <Accordion
            expanded={expanded === 'websites'}
            onChange={onAccordionChange('websites')}
            sx={{ mb: 2 }}
        >
            <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls="websites-content"
                id="websites-header"
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                    <Language />
                    <Typography variant="h6">
                        Websites
                    </Typography>
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <Typography variant="body1" paragraph>
                        Import websites and web pages to track and organize:
                    </Typography>
                    <Box component="ul" sx={{ mb: 2, pl: 3 }}>
                        <li>
                            <Typography variant="body2">
                                Scrape and preview website metadata before importing
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Automatically extract title, description, and images
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Track RSS feeds for websites with available feeds
                            </Typography>
                        </li>
                        <li>
                            <Typography variant="body2">
                                Organize and manage your saved websites
                            </Typography>
                        </li>
                    </Box>
                    <Button
                        variant="contained"
                        startIcon={<Language />}
                        onClick={() => navigate('/import-website')}
                        sx={{ mt: 2 }}
                    >
                        Import Website
                    </Button>
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default WebsiteImportSection;
