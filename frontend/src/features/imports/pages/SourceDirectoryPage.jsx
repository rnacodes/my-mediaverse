import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Container, Box, Typography, Card, CardContent, CardActions,
    Grid, Button, Chip, Divider
} from '@mui/material';
import {
    Podcasts, MenuBook, MovieFilter, VideoLibrary, Article, Language,
    ArrowForward, CheckCircle, OpenInNew, AutoStories
} from '@mui/icons-material';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';

function SourceDirectoryPage() {
    const navigate = useNavigate();

    // Define all available import sources
    const importSources = [
        {
            id: 'podcasts',
            name: 'Podcasts',
            provider: 'ListenNotes',
            providerUrl: 'https://www.listennotes.com',
            icon: <Podcasts sx={{ fontSize: 48 }} />,
            description: 'Search and import podcasts from the largest podcast database. Import individual episodes or entire series.',
            color: '#9C27B0',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Search & import series or episodes',
                'Auto-fetch metadata & artwork',
                'Episode tracking & sync',
                'Background enrichment for metadata'
            ]
        },
        {
            id: 'books',
            name: 'Books',
            provider: 'Google Books',
            providerUrl: 'https://books.google.com',
            icon: <MenuBook sx={{ fontSize: 48 }} />,
            description: 'Import books from Google Books\' extensive catalog. Search by title, author, or ISBN.',
            color: '#FF9800',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Search millions of books',
                'Import by ISBN, title, or author',
                'Auto-fetch metadata & cover images',
                'Import from Goodreads CSV export'
            ]
        },

        {
            id: 'movies-tv',
            name: 'Movies & TV Shows',
            provider: 'TMDB',
            providerUrl: 'https://www.themoviedb.org',
            icon: <MovieFilter sx={{ fontSize: 48 }} />,
            description: 'Search and import movies & TV shows from The Movie Database (TMDB). This product uses the TMDB API but is not endorsed or certified by TMDB.',
            color: '#01D277',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Search movies & TV shows on TMDB',
                'Auto-fetch metadata, cast & artwork',
                'Upload watchlist CSV from external providers',
                'Background enrichment for uploaded lists'
            ]
        },
        {
            id: 'youtube',
            name: 'YouTube Videos',
            provider: 'YouTube',
            providerUrl: 'https://www.youtube.com',
            icon: <VideoLibrary sx={{ fontSize: 48 }} />,
            description: 'Import YouTube videos, playlists, or entire channels to organize your video content.',
            color: '#FF0000',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Import videos by URL',
                'Search YouTube content',
                'Import playlists',
                'Channel subscriptions'
            ]
        },
        {
            id: 'readwise',
            name: 'Articles & Highlights',
            provider: 'Readwise',
            providerUrl: 'https://readwise.io',
            icon: <Article sx={{ fontSize: 48 }} />,
            description: 'Sync articles from Readwise Reader and highlights from Readwise to your library.',
            color: '#D6A84E',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Sync articles from Readwise Reader',
                'Import & sync highlights across sources',
                'Full content archival',
                'Link highlights to books & articles'
            ]
        },
        {
            id: 'websites',
            name: 'Websites',
            provider: 'Web Scraper',
            providerUrl: '',
            icon: <Language sx={{ fontSize: 48 }} />,
            description: 'Save and organize websites with automatic metadata extraction and RSS feed detection.',
            color: '#26A69A',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Import by URL with preview',
                'Auto-extract metadata & favicon',
                'RSS feed detection',
                'Organize by domain'
            ]
        },
        {
            id: 'goodreads',
            name: 'Goodreads Import',
            provider: 'Goodreads',
            providerUrl: 'https://www.goodreads.com',
            icon: <AutoStories sx={{ fontSize: 48 }} />,
            description: 'Import your Goodreads library by uploading a CSV export from your Goodreads account.',
            color: '#553B08',
            available: true,
            connected: false,
            action: () => navigate('/import-media'),
            features: [
                'Upload Goodreads CSV export',
                'Import shelves & reading status',
                'Auto-enrich with Google Books metadata',
                'Bulk import your entire library'
            ]
        }
    ];

    return (
        <Box sx={{ minHeight: '100vh', py: 4, px: 2 }}>
            <Container maxWidth="lg">
                {/* Header Section */}
                <Box sx={{ mb: 6, textAlign: 'center' }}>
                    <Typography 
                        variant="h3" 
                        gutterBottom 
                        fontWeight="bold"
                        sx={{ 
                            background: 'linear-gradient(45deg, #9C27B0 30%, #FF9800 90%)',
                            WebkitBackgroundClip: 'text',
                            WebkitTextFillColor: 'transparent',
                            mb: 2
                        }}
                    >
                        Source Directory
                    </Typography>
                    <Typography 
                        variant="h6" 
                        color="text.secondary" 
                        sx={{ mb: 3, maxWidth: '800px', mx: 'auto' }}
                    >
                        Connect your favorite media sources to automatically import and organize your content
                    </Typography>
                    <Divider sx={{ maxWidth: '200px', mx: 'auto', borderWidth: 2, borderColor: 'primary.main' }} />
                </Box>

                {/* Import Sources Grid */}
                <Grid container spacing={3} sx={{ mb: 4 }}>
                    {importSources.map((source) => (
                        <Grid item xs={12} sm={6} md={4} key={source.id}>
                            <Card
                                sx={{
                                    height: '100%',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    transition: 'all 0.3s ease',
                                    border: `2px solid transparent`,
                                    '&:hover': {
                                        transform: 'translateY(-8px)',
                                        boxShadow: `0 12px 24px ${source.color}40`,
                                        borderColor: source.color
                                    }
                                }}
                            >
                                <CardContent sx={{ flex: 1, p: 3 }}>
                                    {/* Icon and Status */}
                                    <Box sx={{ 
                                        display: 'flex', 
                                        justifyContent: 'space-between', 
                                        alignItems: 'flex-start',
                                        mb: 2 
                                    }}>
                                        <Box sx={{ 
                                            color: source.color,
                                            p: 1.5,
                                            borderRadius: '12px',
                                            bgcolor: `${source.color}20`,
                                            display: 'flex'
                                        }}>
                                            {source.icon}
                                        </Box>
                                        {source.connected && (
                                            <Chip
                                                icon={<CheckCircle />}
                                                label="Connected"
                                                color="success"
                                                size="small"
                                            />
                                        )}
                                        {!source.available && (
                                            <Chip
                                                label="Coming Soon"
                                                size="small"
                                                variant="outlined"
                                            />
                                        )}
                                    </Box>

                                    {/* Service Name and Provider */}
                                    <Typography 
                                        variant="h5" 
                                        gutterBottom 
                                        fontWeight="bold"
                                        sx={{ color: source.color }}
                                    >
                                        {source.name}
                                    </Typography>
                                    
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 2 }}>
                                        <Typography variant="caption" color="text.secondary">
                                            Powered by
                                        </Typography>
                                        <Button
                                            variant="text"
                                            size="small"
                                            href={source.providerUrl}
                                            target="_blank"
                                            rel="noopener noreferrer"
                                            endIcon={<OpenInNew sx={{ fontSize: 12 }} />}
                                            sx={{ 
                                                minWidth: 'auto',
                                                textTransform: 'none',
                                                fontSize: '0.75rem',
                                                p: 0.25,
                                                color: 'text.secondary',
                                                '&:hover': { 
                                                    backgroundColor: 'transparent', 
                                                    color: source.color,
                                                    textDecoration: 'underline' 
                                                }
                                            }}
                                        >
                                            {source.provider}
                                        </Button>
                                    </Box>

                                    {/* Description */}
                                    <Typography 
                                        variant="body2" 
                                        color="text.secondary" 
                                        sx={{ mb: 2, minHeight: '60px' }}
                                    >
                                        {source.description}
                                    </Typography>

                                    {/* Features List */}
                                    <Box component="ul" sx={{ 
                                        pl: 2, 
                                        m: 0,
                                        '& li': { 
                                            mb: 0.5,
                                            fontSize: '0.875rem',
                                            color: 'text.secondary'
                                        }
                                    }}>
                                        {source.features.map((feature) => (
                                            <li key={`feat-${feature}`}>
                                                <Typography variant="body2" component="span">
                                                    {feature}
                                                </Typography>
                                            </li>
                                        ))}
                                    </Box>
                                </CardContent>

                                <CardActions sx={{ p: 3, pt: 0 }}>
                                    <Button
                                        fullWidth
                                        variant="contained"
                                        endIcon={<ArrowForward />}
                                        onClick={source.action}
                                        disabled={!source.available}
                                        sx={{
                                            bgcolor: source.color,
                                            '&:hover': {
                                                bgcolor: source.color,
                                                filter: 'brightness(0.9)'
                                            }
                                        }}
                                    >
                                        {source.connected ? 'Manage Import' : 'Start Importing'}
                                    </Button>
                                </CardActions>
                            </Card>
                        </Grid>
                    ))}
                </Grid>

                {/* Info Section */}
                <Card sx={{ 
                    mt: 6, 
                    bgcolor: 'rgba(156, 39, 176, 0.1)',
                    border: '1px solid rgba(156, 39, 176, 0.3)'
                }}>
                    <CardContent sx={{ p: 4 }}>
                        <Typography variant="h5" gutterBottom fontWeight="bold">
                            How It Works
                        </Typography>
                        <Grid container spacing={3} sx={{ mt: 1 }}>
                            <Grid item xs={12} md={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Box sx={{ 
                                        width: 60, 
                                        height: 60, 
                                        borderRadius: '50%', 
                                        bgcolor: 'primary.main',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        mx: 'auto',
                                        mb: 2,
                                        fontSize: '1.5rem',
                                        fontWeight: 'bold'
                                    }}>
                                        1
                                    </Box>
                                    <Typography variant="h6" gutterBottom>
                                        Choose a Source
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Select from our integrated media sources above
                                    </Typography>
                                </Box>
                            </Grid>
                            <Grid item xs={12} md={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Box sx={{ 
                                        width: 60, 
                                        height: 60, 
                                        borderRadius: '50%', 
                                        bgcolor: 'primary.main',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        mx: 'auto',
                                        mb: 2,
                                        fontSize: '1.5rem',
                                        fontWeight: 'bold'
                                    }}>
                                        2
                                    </Box>
                                    <Typography variant="h6" gutterBottom>
                                        Search & Select
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        Find the content you want to add to your library
                                    </Typography>
                                </Box>
                            </Grid>
                            <Grid item xs={12} md={4}>
                                <Box sx={{ textAlign: 'center' }}>
                                    <Box sx={{ 
                                        width: 60, 
                                        height: 60, 
                                        borderRadius: '50%', 
                                        bgcolor: 'primary.main',
                                        display: 'flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        mx: 'auto',
                                        mb: 2,
                                        fontSize: '1.5rem',
                                        fontWeight: 'bold'
                                    }}>
                                        3
                                    </Box>
                                    <Typography variant="h6" gutterBottom>
                                        Import & Organize
                                    </Typography>
                                    <Typography variant="body2" color="text.secondary">
                                        All metadata is automatically imported and organized
                                    </Typography>
                                </Box>
                            </Grid>
                        </Grid>
                    </CardContent>
                </Card>

                {/* Navigation Buttons */}
                <Box sx={{ mt: 4, display: 'flex', gap: 2, justifyContent: 'center', flexWrap: 'wrap' }}>
                    <WhiteOutlineButton 
                        onClick={() => navigate('/')}
                    >
                        Back to Home
                    </WhiteOutlineButton>
                    <Button
                        variant="contained"
                        onClick={() => navigate('/all-media')}
                    >
                        View My Library
                    </Button>
                </Box>
            </Container>
        </Box>
    );
}

export default SourceDirectoryPage;
