import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Button, Box, Typography, Divider,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Language, ExpandMore, Link as LinkIcon, FormatListBulleted } from '@mui/icons-material';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_SECTION_BLOCKED } from '@/features/demo/demoMessages';
import BookmarkFileImport from '@/features/imports/pages/WebsiteImport/BookmarkFileImport';

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
                        Save web pages three ways: one address at a time, a bookmarks file exported from your
                        browser or bookmark manager, or a pasted list of links. Titles, descriptions, images,
                        RSS feeds and archived copies are filled in afterwards by enrichment.
                    </Typography>

                    <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 1 }}>
                        Import a bookmarks file
                    </Typography>
                    <BookmarkFileImport compact />

                    <Divider sx={{ my: 3 }} />

                    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                        <DemoWriteGuard title={DEMO_SECTION_BLOCKED}>
                            <Button
                                variant="contained"
                                startIcon={<LinkIcon />}
                                onClick={() => navigate('/import-website?tab=url')}
                            >
                                Import a single website
                            </Button>
                        </DemoWriteGuard>
                        <DemoWriteGuard title={DEMO_SECTION_BLOCKED}>
                            <Button
                                variant="outlined"
                                startIcon={<FormatListBulleted />}
                                onClick={() => navigate('/import-website?tab=paste')}
                            >
                                Paste a list of links
                            </Button>
                        </DemoWriteGuard>
                    </Box>
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default WebsiteImportSection;
