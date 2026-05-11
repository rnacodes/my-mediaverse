import React, { useState, useRef } from 'react';
import {
    Container, Box, Typography, Button, Paper, Alert, AlertTitle,
    CircularProgress, List, ListItem, ListItemText, Divider,
    Card, CardContent, Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { CloudUpload, FileUpload, CheckCircle, Error, ExpandMore, MenuBook } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { uploadCsv } from '../api/uploadService';

function UploadMediaPage() {
    const navigate = useNavigate();
    const fileInputRef = useRef(null);
    const [file, setFile] = useState(null);
    const [uploading, setUploading] = useState(false);
    const [uploadResult, setUploadResult] = useState(null);
    const [error, setError] = useState('');

    const handleFileSelect = (event) => {
        const selectedFile = event.target.files[0];
        if (selectedFile) {
            if (selectedFile.name.endsWith('.csv')) {
                setFile(selectedFile);
                setError('');
                setUploadResult(null);
            } else {
                setError('Please select a CSV file');
                setFile(null);
            }
        }
    };

    const handleUpload = async () => {
        if (!file) {
            setError('Please select a file first');
            return;
        }

        setUploading(true);
        setError('');
        setUploadResult(null);

        try {
            // Read the first line of the CSV to detect if MediaType column exists
            const text = await file.text();
            const lines = text.split('\n');
            
            if (lines.length < 2) {
                throw new Error('CSV file must have a header row and at least one data row');
            }
            
            const headers = lines[0].toLowerCase().split(',');
            
            // Check if MediaType column exists
            const mediaTypeIndex = headers.findIndex(h => h.trim() === 'mediatype');
            
            if (mediaTypeIndex === -1) {
                throw new Error('CSV file must include a "MediaType" column');
            }

            // Upload the CSV - backend will read MediaType from each row
            // We pass null as mediaType to let the backend handle per-row media types
            const response = await uploadCsv(file, null);
            console.log('Upload response:', response.data);
            setUploadResult(response.data);
        } catch (err) {
            console.error('Upload error:', err);
            if (err.response?.data?.error) {
                setError(err.response.data.error + (err.response.data.details ? ': ' + err.response.data.details : ''));
            } else if (err.message) {
                setError(err.message);
            } else {
                setError('Failed to upload file. Please try again.');
            }
        } finally {
            setUploading(false);
        }
    };

    const resetUpload = () => {
        setFile(null);
        setUploadResult(null);
        setError('');
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Typography variant="h4" component="h1" gutterBottom>
                Upload Media
            </Typography>
            
            <Typography variant="body1" color="text.secondary" paragraph>
                Upload a CSV file to import multiple media items at once. Your CSV must include a MediaType column to specify the type of each item.
                You can mix different media types in a single CSV file!
            </Typography>

            <Alert severity="info" sx={{ mb: 3 }}>
                <AlertTitle>CSV Format Requirement</AlertTitle>
                Your CSV file must include <strong>MediaType</strong> as a column. Each row can have a different type! 
                <br />
                <strong>Supported types via CSV:</strong> Article, Book, Movie, TVShow, Video, Website
                <br />
                <strong>Not supported via CSV:</strong> Podcast, Channel, Document, Music, Other, Playlist, VideoGame
                <br />
                <em>For podcasts, please use the Import Media page. Other types will be added in future updates.</em>
            </Alert>

            <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
                <Box sx={{ textAlign: 'center', mb: 3 }}>
                    <CloudUpload sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                    <Typography variant="h6" gutterBottom>
                        Select CSV File
                    </Typography>
                </Box>

                <Box sx={{ mb: 3 }}>
                    <input
                        ref={fileInputRef}
                        id="csv-file-input"
                        type="file"
                        accept=".csv"
                        onChange={handleFileSelect}
                        style={{ display: 'none' }}
                    />
                    <label htmlFor="csv-file-input">
                        <Button
                            variant="outlined"
                            component="span"
                            startIcon={<FileUpload />}
                            fullWidth
                            sx={{ 
                                mb: 2,
                                borderColor: 'rgba(255, 255, 255, 0.7)',
                                color: 'text.primary',
                                '&:hover': {
                                    borderColor: 'rgba(255, 255, 255, 1)',
                                    backgroundColor: 'rgba(255, 255, 255, 0.05)'
                                }
                            }}
                        >
                            Choose CSV File
                        </Button>
                    </label>

                    {file && (
                        <Alert severity="info" sx={{ mb: 2 }}>
                            <AlertTitle>File Selected</AlertTitle>
                            {file.name} ({(file.size / 1024).toFixed(1)} KB)
                        </Alert>
                    )}
                </Box>

                <Box sx={{ display: 'flex', gap: 2, justifyContent: 'center' }}>
                    <Button
                        variant="contained"
                        onClick={handleUpload}
                        disabled={!file || uploading}
                        startIcon={uploading ? <CircularProgress size={20} /> : <CloudUpload />}
                    >
                        {uploading ? 'Uploading...' : 'Upload CSV'}
                    </Button>
                    
                    <Button
                        variant="outlined"
                        onClick={resetUpload}
                        disabled={uploading}
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.7)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 1)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        Reset
                    </Button>
                </Box>
            </Paper>

            {/* Goodreads Import Section */}
            <Paper elevation={3} sx={{ p: 4, mb: 4 }}>
                <Box sx={{ textAlign: 'center', mb: 3 }}>
                    <MenuBook sx={{ fontSize: 64, color: 'primary.main', mb: 2 }} />
                    <Typography variant="h6" gutterBottom>
                        Import from Goodreads
                    </Typography>
                    <Typography variant="body2" color="text.secondary" paragraph>
                        Import your entire Goodreads library from a CSV export
                    </Typography>
                </Box>
                <Button
                    variant="contained"
                    onClick={() => navigate('/upload-goodreads')}
                    fullWidth
                    startIcon={<MenuBook />}
                    sx={{ mb: 2 }}
                >
                    Import Goodreads Library
                </Button>
                <Alert severity="info">
                    eBook Management Coming Soon
                </Alert>
            </Paper>

            {error && (
                <Alert severity="error" sx={{ mb: 3 }}>
                    <AlertTitle>Upload Error</AlertTitle>
                    {error}
                </Alert>
            )}

            {uploadResult && (
                <Paper elevation={3} sx={{ p: 3, mb: 4, border: '2px solid', borderColor: uploadResult.ErrorCount === 0 ? 'success.main' : 'warning.main' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', mb: 3 }}>
                        {uploadResult.ErrorCount === 0 ? (
                            <CheckCircle sx={{ color: 'success.main', mr: 2, fontSize: 32 }} />
                        ) : (
                            <Error sx={{ color: 'warning.main', mr: 2, fontSize: 32 }} />
                        )}
                        <Box>
                            <Typography variant="h5" sx={{ fontWeight: 600, color: uploadResult.ErrorCount === 0 ? 'success.main' : 'warning.main' }}>
                                Upload Complete
                            </Typography>
                            <Typography variant="body1" color="text.secondary">
                                {uploadResult.Message}
                            </Typography>
                        </Box>
                    </Box>

                    <Box sx={{ display: 'flex', gap: 2, mb: 3 }}>
                        <Card sx={{ 
                            flex: 1, 
                            border: '2px solid',
                            borderColor: 'success.main',
                            backgroundColor: 'rgba(76, 175, 80, 0.1)'
                        }}>
                            <CardContent sx={{ textAlign: 'center', py: 3 }}>
                                <Typography variant="h3" color="success.main" sx={{ fontWeight: 700, mb: 1 }}>
                                    {uploadResult.successCount || uploadResult.SuccessCount || 0}
                                </Typography>
                                <Typography variant="h6" sx={{ fontWeight: 500, color: 'success.main' }}>
                                    Successful
                                </Typography>
                            </CardContent>
                        </Card>
                        
                        <Card sx={{ 
                            flex: 1, 
                            border: '2px solid',
                            borderColor: uploadResult.ErrorCount === 0 ? 'transparent' : 'warning.main',
                            backgroundColor: uploadResult.ErrorCount === 0 ? 'transparent' : 'rgba(255, 152, 0, 0.1)'
                        }}>
                            <CardContent sx={{ textAlign: 'center', py: 3 }}>
                                <Typography variant="h3" color={uploadResult.ErrorCount === 0 ? 'text.secondary' : 'warning.main'} sx={{ fontWeight: 700, mb: 1 }}>
                                    {uploadResult.errorCount || uploadResult.ErrorCount || 0}
                                </Typography>
                                <Typography variant="h6" sx={{ fontWeight: 500, color: uploadResult.ErrorCount === 0 ? 'text.secondary' : 'warning.main' }}>
                                    Errors
                                </Typography>
                            </CardContent>
                        </Card>
                    </Box>



                    {/* Display imported items with thumbnails */}

                    

                    
                    {/* Display imported items as a simple list */}
                    {uploadResult.importedItems && uploadResult.importedItems.length > 0 && (
                        <Box sx={{ mb: 3 }}>
                            <Typography variant="h6" gutterBottom sx={{ 
                                color: 'success.main', 
                                fontWeight: 600,
                                display: 'flex',
                                alignItems: 'center',
                                gap: 1
                            }}>
                                <CheckCircle sx={{ fontSize: 20 }} />
                                Successfully Imported ({uploadResult.importedItems.length} items)
                            </Typography>
                            
                            <Paper elevation={1} sx={{ 
                                p: 2, 
                                border: '1px solid',
                                borderColor: 'success.light',
                                backgroundColor: 'rgba(76, 175, 80, 0.05)'
                            }}>
                                <List>
                                    {uploadResult.importedItems.map((item, index) => (
                                        <React.Fragment key={item.id}>
                                            <ListItem 
                                                sx={{ 
                                                    cursor: 'pointer',
                                                    borderRadius: 1,
                                                    '&:hover': { 
                                                        bgcolor: 'rgba(76, 175, 80, 0.1)',
                                                        transform: 'translateX(4px)',
                                                        transition: 'all 0.2s'
                                                    }
                                                }}
                                                onClick={() => {
                                                    // Navigate to the item's profile page
                                                    let route = `/media/${item.id}`;
                                                    // Check if it's a podcast series (episodes have seriesId, series don't)
                                                    if (item.mediaType === 'Podcast' && !item.seriesId) {
                                                        route = `/podcast-series/${item.id}`;
                                                    } else if (item.mediaType === 'Channel') {
                                                        route = `/youtube-channel/${item.id}`;
                                                    }
                                                    window.location.href = route;
                                                }}
                                            >
                                                <ListItemText
                                                    primary={item.title}
                                                    secondary={item.author ? `by ${item.author}` : item.mediaType}
                                                    primaryTypographyProps={{ 
                                                        variant: 'body1',
                                                        fontWeight: 500
                                                    }}
                                                    secondaryTypographyProps={{ 
                                                        variant: 'body2',
                                                        color: 'text.secondary'
                                                    }}
                                                />
                                            </ListItem>
                                            {index < uploadResult.importedItems.length - 1 && <Divider />}
                                        </React.Fragment>
                                    ))}
                                </List>
                            </Paper>
                            
                            <Typography variant="body2" color="text.secondary" sx={{ mt: 1, textAlign: 'center' }}>
                                Click on any item to view its profile page
                            </Typography>
                        </Box>
                    )}

                    {uploadResult.Errors && uploadResult.Errors.length > 0 && (
                        <Accordion sx={{ 
                            border: '1px solid',
                            borderColor: 'warning.light',
                            backgroundColor: 'rgba(255, 152, 0, 0.05)'
                        }}>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography variant="h6" sx={{ 
                                    color: 'warning.main',
                                    fontWeight: 600,
                                    display: 'flex',
                                    alignItems: 'center',
                                    gap: 1
                                }}>
                                    <Error sx={{ fontSize: 20 }} />
                                    Error Details ({uploadResult.Errors.length})
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails>
                                <List>
                                    {uploadResult.Errors.map((error, index) => (
                                        <React.Fragment key={index}>
                                            <ListItem>
                                                <ListItemText
                                                    primary={error}
                                                    primaryTypographyProps={{ 
                                                        variant: 'body2',
                                                        color: 'error.main'
                                                    }}
                                                />
                                            </ListItem>
                                            {index < uploadResult.Errors.length - 1 && <Divider />}
                                        </React.Fragment>
                                    ))}
                                </List>
                            </AccordionDetails>
                        </Accordion>
                    )}
                </Paper>
            )}

            {/* CSV Format Instructions */}
            <Paper elevation={1} sx={{ p: 3 }}>
                <Typography variant="h6" gutterBottom>
                    CSV Format Instructions
                </Typography>
                
                <Typography variant="body2" paragraph>
                    Your CSV file must include a header row with column names. The <strong>MediaType</strong> column 
                    is required as the first column and specifies the type of media for each row.
                </Typography>

                <Alert severity="info" sx={{ mb: 2 }}>
                    <Typography variant="body2">
                        <strong>Common Columns (All Media Types):</strong>
                        <br />• <strong>MediaType</strong> (Required): Article, Book, Movie, TVShow, Video, Website
                        <br />• <strong>Title</strong> (Required): The title of the media item
                        <br />• <strong>Description</strong> (Optional): A description of the media item
                        <br />• <strong>Link</strong> (Optional): URL to the media item
                        <br />• <strong>Notes</strong> (Optional): Your personal notes
                        <br />• <strong>RelatedNotes</strong> (Optional): Links to Obsidian or other related notes
                        <br />• <strong>Thumbnail</strong> (Optional): URL to thumbnail image
                        <br />• <strong>Status</strong> (Optional): Uncharted, ActivelyExploring, Completed, Abandoned
                        <br />• <strong>Rating</strong> (Optional): SuperLike, Like, Neutral, Dislike
                        <br />• <strong>OwnershipStatus</strong> (Optional): Own, Rented, Streamed
                        <br />• <strong>DateCompleted</strong> (Optional): Date format like "2024-01-15"
                        <br />• <strong>Topics</strong> (Optional): Comma-separated topic names (will be converted to lowercase)
                        <br />• <strong>Genres</strong> (Optional): Comma-separated genre names (will be converted to lowercase)
                    </Typography>
                </Alert>

                <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle1">📚 Book Format</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography variant="body2" paragraph>
                            <strong>Required:</strong> MediaType (Book), Title, Author
                        </Typography>
                        <Typography variant="body2" paragraph>
                            <strong>Book-Specific Columns:</strong> Author, ISBN, ASIN, Format (Digital/Physical), 
                            PartOfSeries (true/false), SeriesName, PositionInSeries
                        </Typography>
                    </AccordionDetails>
                </Accordion>

                <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle1">🎬 Movie Format</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography variant="body2" paragraph>
                            <strong>Required:</strong> MediaType (Movie), Title
                        </Typography>
                        <Typography variant="body2" paragraph>
                            <strong>Movie-Specific Columns:</strong> Director, ReleaseYear, RuntimeMinutes, 
                            TmdbId (The Movie Database ID)
                        </Typography>
                    </AccordionDetails>
                </Accordion>

                <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle1">📺 TV Show Format</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography variant="body2" paragraph>
                            <strong>Required:</strong> MediaType (TVShow), Title
                        </Typography>
                        <Typography variant="body2" paragraph>
                            <strong>TV Show-Specific Columns:</strong> Creator, FirstAirYear, LastAirYear, 
                            NumberOfSeasons, NumberOfEpisodes, TmdbId
                        </Typography>
                    </AccordionDetails>
                </Accordion>

                <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle1">📹 Video Format</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography variant="body2" paragraph>
                            <strong>Required:</strong> MediaType (Video), Title
                        </Typography>
                        <Typography variant="body2" paragraph>
                            <strong>Video-Specific Columns:</strong> ChannelName, VideoId (YouTube ID), 
                            ChannelId (YouTube Channel ID), DurationInSeconds, ViewCount, PublishedAt
                        </Typography>
                    </AccordionDetails>
                </Accordion>

                <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle1">📰 Article Format</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography variant="body2" paragraph>
                            <strong>Required:</strong> MediaType (Article), Title
                        </Typography>
                        <Typography variant="body2" paragraph>
                            <strong>Article-Specific Columns:</strong> Author, Link (required for articles), 
                            PublicationDate, Domain, IsArchived (true/false), IsStarred (true/false), 
                            ReadingProgress (0-100), WordCount, ExternalId (Readwise ID)
                        </Typography>
                    </AccordionDetails>
                </Accordion>

                <Accordion>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle1">🌐 Website Format</Typography>
                    </AccordionSummary>
                    <AccordionDetails>
                        <Typography variant="body2" paragraph>
                            <strong>Required:</strong> MediaType (Website), Title, Link
                        </Typography>
                        <Typography variant="body2" paragraph>
                            <strong>Website-Specific Columns:</strong> Domain, HasRssFeed (true/false), 
                            RssFeedUrl, FaviconUrl, Type (Blog/Portfolio/Documentation/Tool/Resource/Other)
                        </Typography>
                    </AccordionDetails>
                </Accordion>

                <Alert severity="warning" sx={{ mt: 2, mb: 2 }}>
                    <Typography variant="body2">
                        <strong>Important Tips:</strong>
                        <br />• Your CSV file MUST include a "MediaType" column
                        <br />• <strong>NEW:</strong> You can mix different media types in the same CSV! Each row can have its own MediaType.
                        <br />• Only these types are supported via CSV: Article, Book, Movie, TVShow, Video, Website
                        <br />• For Podcasts, use the Import Media page (ListenNotes integration)
                        <br />• Ensure your CSV file is properly formatted with commas as separators
                        <br />• Use quotes around text that contains commas
                        <br />• For boolean fields, use "TRUE" or "FALSE" (case-insensitive)
                        <br />• For date fields, use formats like "2024-01-15" or "01/15/2024"
                        <br />• Multiple topics/genres within a cell can be separated by semicolons (they will be converted to lowercase)
                        <br />• Leave optional columns empty if not applicable
                    </Typography>
                </Alert>

                <Typography variant="subtitle2" gutterBottom sx={{ mt: 3, fontWeight: 600 }}>
                    Download Sample CSV Templates
                </Typography>
                <Box sx={{ mt: 2, display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                    <Button
                        variant="outlined"
                        size="small"
                        href="/sample-book-import.csv"
                        download="sample-book-import.csv"
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.5)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 0.8)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        📚 Book Sample
                    </Button>
                    <Button
                        variant="outlined"
                        size="small"
                        href="/sample-movie-import.csv"
                        download="sample-movie-import.csv"
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.5)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 0.8)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        🎬 Movie Sample
                    </Button>
                    <Button
                        variant="outlined"
                        size="small"
                        href="/sample-tvshow-import.csv"
                        download="sample-tvshow-import.csv"
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.5)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 0.8)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        📺 TV Show Sample
                    </Button>
                    <Button
                        variant="outlined"
                        size="small"
                        href="/sample-video-import.csv"
                        download="sample-video-import.csv"
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.5)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 0.8)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        📹 Video Sample
                    </Button>
                    <Button
                        variant="outlined"
                        size="small"
                        href="/sample-article-import.csv"
                        download="sample-article-import.csv"
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.5)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 0.8)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        📰 Article Sample
                    </Button>
                    <Button
                        variant="outlined"
                        size="small"
                        href="/sample-website-import.csv"
                        download="sample-website-import.csv"
                        sx={{ 
                            borderColor: 'rgba(255, 255, 255, 0.5)',
                            color: 'text.primary',
                            '&:hover': {
                                borderColor: 'rgba(255, 255, 255, 0.8)',
                                backgroundColor: 'rgba(255, 255, 255, 0.05)'
                            }
                        }}
                    >
                        🌐 Website Sample
                    </Button>
                </Box>
            </Paper>

            {/* Go Back Button */}
            <Box sx={{ textAlign: 'center', mt: 4 }}>
                <Button 
                    variant="outlined" 
                    onClick={() => window.history.back()}
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
