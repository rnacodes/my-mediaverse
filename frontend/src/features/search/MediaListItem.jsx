import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Paper, Typography, Grid, Checkbox, Chip } from '@mui/material';
import { Star } from '@mui/icons-material';
import { formatMediaType, formatStatus, getRatingIcon } from '@/utils/formatters';

export const MediaListItem = React.memo(({ item, isSelected = false, onToggleSelect, showCheckbox = false }) => {
    const navigate = useNavigate();
    
    // Determine navigation path based on item type
    const handleClick = () => {
        if (item.isMixlist) {
            navigate(`/mixlist/${item.id}`);
        } else if (item.isNote) {
            // Navigate notes to their dedicated profile page
            navigate(`/note/${item.id}`);
        } else if (item.isHighlight) {
            // Navigate highlights to their dedicated profile page
            navigate(`/highlight/${item.id}`);
        } else if (item.mediaType === 'Podcast' && !item.seriesId) {
            // Navigate podcast series to their dedicated profile page
            // Podcast episodes have seriesId, series don't
            navigate(`/podcast-series/${item.id}`);
        } else if (item.mediaType === 'Channel') {
            // Navigate YouTube channels to their dedicated profile page
            navigate(`/youtube-channel/${item.id}`);
        } else {
            navigate(`/media/${item.id}`);
        }
    };

    const handleCheckboxChange = (event) => {
        event.stopPropagation();
        if (onToggleSelect) {
            onToggleSelect(item.id);
        }
    };
    
    // Helper function to get the primary creator/author/maker
    const getPrimaryCredit = () => {
        switch (item.mediaType) {
            case 'Book':
            case 'Article':
                return item.author;
            case 'Movie':
                return item.director ? `Dir: ${item.director}` : null;
            case 'TVShow':
                return item.creator ? `Created by ${item.creator}` : null;
            case 'Video':
                return item.channel || item.platform;
            case 'Podcast':
                return item.publisher;
            case 'Mixlist':
                return null;
            default:
                return item.creator || item.author || null;
        }
    };

    // Helper function to get meaningful metadata line
    const getMetadataLine = () => {
        const parts = [];
        
        switch (item.mediaType) {
            case 'Book':
                if (item.goodreadsRating) parts.push(`${item.goodreadsRating}★`);
                break;
            case 'Movie':
                if (item.releaseYear) parts.push(item.releaseYear);
                if (item.runtimeMinutes) parts.push(`${item.runtimeMinutes} min`);
                if (item.tmdbRating) parts.push(`${item.tmdbRating}★`);
                break;
            case 'TVShow':
                if (item.tmdbRating) parts.push(`${item.tmdbRating}★`);
                break;
            case 'Video':
                if (item.lengthInSeconds) {
                    const hours = Math.floor(item.lengthInSeconds / 3600);
                    const minutes = Math.floor((item.lengthInSeconds % 3600) / 60);
                    parts.push(hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`);
                }
                // Platform is already shown in getPrimaryCredit(), so don't duplicate it here
                break;
            case 'Podcast':
                if (item.podcastType) parts.push(item.podcastType === 'Series' ? 'Series' : 'Episode');
                if (item.durationInSeconds) {
                    const minutes = Math.floor(item.durationInSeconds / 60);
                    parts.push(`${minutes} min`);
                }
                break;
            case 'Article':
                if (item.publication) parts.push(item.publication);
                if (item.estimatedReadingTimeMinutes) parts.push(`${item.estimatedReadingTimeMinutes} min read`);
                if (item.wordCount) parts.push(`${(item.wordCount / 1000).toFixed(1)}k words`);
                break;
            case 'Mixlist':
                return null;
            default:
                break;
        }
        
        return parts.length > 0 ? parts.join(' • ') : null;
    };
    
    return (
    <Paper
        onClick={handleClick}
        sx={{
            p: 2.5,
            mb: 2,
            cursor: 'pointer',
            border: item.isMixlist && isSelected ? '2px solid' : 'none',
            borderColor: 'primary.main',
            '&:hover': {
                boxShadow: 6,
                backgroundColor: 'rgba(255, 255, 255, 0.02)'
            },
            transition: 'all 0.2s'
        }}>
        <Grid container spacing={2} alignItems="center">
            {showCheckbox && (
                <Grid item>
                    <Checkbox
                        checked={isSelected}
                        onChange={handleCheckboxChange}
                        onClick={(e) => e.stopPropagation()}
                        color="primary"
                        size="small"
                    />
                </Grid>
            )}
            <Grid item xs={12} md={6}>
                {/* Title and Rating */}
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                    <Typography variant="h6" sx={{ fontWeight: 'bold', fontSize: '1.1rem' }}>
                        {item.title}
                    </Typography>
                    {item.ratingType && (
                        <Box sx={{ display: 'flex', alignItems: 'center' }}>
                            {getRatingIcon(item.ratingType)}
                        </Box>
                    )}
                </Box>
                
                {/* Primary Credit */}
                {getPrimaryCredit() && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5, fontWeight: 500 }}>
                        {getPrimaryCredit()}
                    </Typography>
                )}
                
                {/* Metadata Line */}
                {getMetadataLine() && (
                    <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5, fontSize: '0.85rem' }}>
                        {getMetadataLine()}
                    </Typography>
                )}
                
                {/* Description/Notes */}
                {item.notes && (
                    <Typography 
                        variant="body2" 
                        sx={{ 
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            display: '-webkit-box',
                            WebkitLineClamp: 1,
                            WebkitBoxOrient: 'vertical',
                            color: 'text.secondary',
                            fontSize: '0.85rem'
                        }}
                    >
                        {item.notes}
                    </Typography>
                )}
            </Grid>
            
            {/* Topics Column */}
            <Grid item xs={12} md={3}>
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
                    {item.topics.slice(0, 4).map((topic) => (
                        <Chip
                            key={`topic-${topic}`}
                            label={topic}
                            size="small"
                            sx={{ 
                                fontSize: '0.75rem', 
                                height: '24px',
                                backgroundColor: 'rgba(54, 39, 89, 0.3)',
                                color: '#ce93d8'
                            }}
                        />
                    ))}
                    {item.topics.length > 4 && (
                        <Chip
                            label={`+${item.topics.length - 4}`}
                            size="small"
                            sx={{ fontSize: '0.75rem', height: '24px' }}
                        />
                    )}
                </Box>
            </Grid>
            
            {/* Chips Column */}
            <Grid item xs={12} md={3}>
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', justifyContent: { xs: 'flex-start', md: 'flex-end' } }}>
                    <Chip 
                        label={formatMediaType(item.mediaType)} 
                        size="small" 
                        sx={{ 
                            backgroundColor: 'rgba(105, 90, 140, 0.2)',
                            color: '#b39ddb',
                            fontWeight: 'bold'
                        }}
                    />
                    {item.status && (
                        <Chip 
                            label={formatStatus(item.status)} 
                            size="small" 
                            color={
                                item.status === 'ActivelyExploring' ? 'success' :
                                item.status === 'Uncharted' ? 'info' :
                                item.status === 'Completed' ? 'default' :
                                'warning'
                            }
                            variant="outlined"
                        />
                    )}
                    
                    {/* Conditional extra chips */}
                    {item.mediaType === 'Podcast' && item.podcastType && (
                        <Chip 
                            label={item.podcastType === 'Series' ? 'Series' : 'Episode'} 
                            size="small"
                            sx={{ fontSize: '0.7rem' }}
                        />
                    )}
                    {item.mediaType === 'Article' && item.isStarred && (
                        <Chip 
                            icon={<Star sx={{ fontSize: 14 }} />} 
                            label="Starred" 
                            size="small"
                            sx={{ fontSize: '0.7rem' }}
                        />
                    )}
                </Box>
            </Grid>
        </Grid>
    </Paper>
    );
});

MediaListItem.displayName = 'MediaListItem';

