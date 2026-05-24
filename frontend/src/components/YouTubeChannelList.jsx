import React, { useState, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Box, Typography, Button, Card, CardContent, CardMedia, CardActions,
    Grid, CircularProgress, Alert, TextField, InputAdornment
} from '@mui/material';
import { Add, Search, YouTube } from '@mui/icons-material';
import { useAllYouTubeChannels } from '../hooks/useYoutube';

function YouTubeChannelList() {
    const [searchQuery, setSearchQuery] = useState('');
    const navigate = useNavigate();

    const channelsQuery = useAllYouTubeChannels();
    const channels = useMemo(() => channelsQuery.data ?? [], [channelsQuery.data]);
    const loading = channelsQuery.isLoading;

    const filteredChannels = useMemo(() => {
        if (!searchQuery.trim()) return channels;
        const query = searchQuery.toLowerCase();
        return channels.filter(channel =>
            channel.title.toLowerCase().includes(query) ||
            channel.description?.toLowerCase().includes(query) ||
            channel.customUrl?.toLowerCase().includes(query)
        );
    }, [searchQuery, channels]);

    if (loading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Box p={3}>
            {/* Header */}
            <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
                <Typography variant="h4">YouTube Channels</Typography>
                <Button
                    variant="contained"
                    startIcon={<Add />}
                    onClick={() => navigate('/import-media?tab=youtube')}
                >
                    Import Channel
                </Button>
            </Box>

            {/* Search */}
            <TextField
                fullWidth
                placeholder="Search channels..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                sx={{ mb: 3 }}
                InputProps={{
                    startAdornment: (
                        <InputAdornment position="start">
                            <Search />
                        </InputAdornment>
                    ),
                }}
            />

            {/* Channels Grid */}
            {filteredChannels.length === 0 ? (
                <Alert severity="info">
                    {channels.length === 0
                        ? 'No YouTube channels yet. Import one to get started!'
                        : 'No channels match your search.'
                    }
                </Alert>
            ) : (
                <Grid container spacing={3}>
                    {filteredChannels.map((channel) => (
                        <Grid item xs={12} sm={6} md={4} lg={3} key={channel.id}>
                            <Card
                                sx={{
                                    height: '100%',
                                    display: 'flex',
                                    flexDirection: 'column',
                                    cursor: 'pointer',
                                    '&:hover': { boxShadow: 6 }
                                }}
                                onClick={() => navigate(`/youtube-channel/${channel.id}`)}
                            >
                                {channel.thumbnail && (
                                    <CardMedia
                                        component="img"
                                        height="140"
                                        image={channel.thumbnail}
                                        alt={channel.title}
                                        crossOrigin="anonymous"
                                    />
                                )}
                                <CardContent sx={{ flexGrow: 1 }}>
                                    <Typography variant="h6" gutterBottom noWrap>
                                        {channel.title}
                                    </Typography>
                                    
                                    {channel.customUrl && (
                                        <Typography variant="body2" color="text.secondary" gutterBottom>
                                            {channel.customUrl}
                                        </Typography>
                                    )}

                                    {channel.description && (
                                        <Typography 
                                            variant="body2" 
                                            color="text.secondary"
                                            sx={{
                                                overflow: 'hidden',
                                                textOverflow: 'ellipsis',
                                                display: '-webkit-box',
                                                WebkitLineClamp: 2,
                                                WebkitBoxOrient: 'vertical',
                                            }}
                                        >
                                            {channel.description}
                                        </Typography>
                                    )}

                                    <Box mt={2} display="flex" flexDirection="column" gap={0.5}>
                                        {channel.subscriberCount && (
                                            <Typography variant="caption" color="text.secondary">
                                                📊 {channel.subscriberCount.toLocaleString()} subscribers
                                            </Typography>
                                        )}
                                        {channel.videoCountInDb > 0 && (
                                            <Typography variant="caption" color="primary">
                                                🎬 {channel.videoCountInDb} videos in database
                                            </Typography>
                                        )}
                                    </Box>
                                </CardContent>
                                <CardActions>
                                    <Button
                                        size="small"
                                        startIcon={<YouTube />}
                                        href={channel.link}
                                        target="_blank"
                                        onClick={(e) => e.stopPropagation()}
                                    >
                                        View on YouTube
                                    </Button>
                                </CardActions>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            )}
        </Box>
    );
}

export default YouTubeChannelList;

