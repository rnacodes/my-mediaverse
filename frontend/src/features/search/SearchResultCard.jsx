import React from 'react';
import { Link } from 'react-router-dom';
import { Box, Card, CardContent, CardMedia, Chip, Typography, Checkbox } from '@mui/material';
import { Star, AccessTime } from '@mui/icons-material';
import { formatMediaType, formatStatus, getRatingIcon } from '@/utils/formatters';

// Determine navigation path based on item type
const getItemPath = (item) => {
    if (item.isMixlist) return `/mixlist/${item.id}`;
    if (item.isNote) return `/note/${item.id}`;
    if (item.isHighlight) return `/highlight/${item.id}`;
    if (item.mediaType === 'Podcast' && !item.seriesId) return `/podcast-series/${item.id}`;
    if (item.mediaType === 'Channel') return `/youtube-channel/${item.id}`;
    return `/media/${item.id}`;
};

// Layout constants used to line the overlay checkbox up with its in-flow spacer.
const CARD_PADDING_PX = 20;      // CardContent p: 2.5
const CHECKBOX_TOP_OFFSET_PX = 4; // header row mt: 0.5
const CHECKBOX_SIZE_PX = 20;      // size="small" with p: 0
const MIXLIST_THUMB_HEIGHT_PX = 140;

export const SearchResultCard = React.memo(({ item, isSelected = false, onToggleSelect, showCheckbox = false }) => {
    const handleCheckboxChange = () => {
        if (onToggleSelect) {
            onToggleSelect(item.id);
        }
    };

    const hasThumbnail = Boolean(item.isMixlist && item.thumbnail);
    const checkboxTop = (hasThumbnail ? MIXLIST_THUMB_HEIGHT_PX : 0) + CARD_PADDING_PX + CHECKBOX_TOP_OFFSET_PX;

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
                return null; // Mixlists don't have a primary credit
            case 'Highlight':
                // Show the linked media title if available, otherwise the source title
                return item.linkedMediaTitle || (item.author ? `From: ${item.author}` : null);
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
                return null; // Handled separately with item count
            default:
                break;
        }
        
        return parts.length > 0 ? parts.join(' • ') : null;
    };
    
    return (
        // The checkbox is rendered as a sibling of the Link rather than inside it: a
        // checkbox nested in an anchor needs preventDefault() to stop navigation, and
        // cancelling a checkbox click makes the browser revert the toggle.
        <Box
            sx={{
                position: 'relative',
                height: '100%',
                transition: 'transform 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
                '&:hover': { transform: 'translateY(-4px)' }
            }}
        >
        {showCheckbox && (
            <Checkbox
                checked={isSelected}
                onChange={handleCheckboxChange}
                inputProps={{ 'aria-label': `Select ${item.title}` }}
                sx={{
                    p: 0,
                    position: 'absolute',
                    top: checkboxTop,
                    left: CARD_PADDING_PX,
                    zIndex: 1
                }}
                size="small"
            />
        )}
        <Link to={getItemPath(item)} style={{ textDecoration: 'none', color: 'inherit', display: 'block', height: '100%' }}>
        <Card
            sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                cursor: 'pointer',
                '&:hover': {
                    boxShadow: 8,
                    '& .card-title': {
                        color: 'primary.main'
                    }
                },
                transition: 'box-shadow 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
            }}
        >
        {hasThumbnail && (
            <CardMedia
                component="img"
                image={item.thumbnail}
                alt={item.title}
                sx={{ height: 140, objectFit: 'cover' }}
            />
        )}
        <CardContent sx={{ flexGrow: 1, p: 2.5 }}>
            {/* HEADER ROW: Checkbox + Title + Rating Icon */}
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                <Box sx={{ display: 'flex', alignItems: 'flex-start', gap: 1, flex: 1 }}>
                    {showCheckbox && (
                        // Spacer reserving the overlay checkbox's slot in the header row.
                        <Box sx={{ width: CHECKBOX_SIZE_PX, height: CHECKBOX_SIZE_PX, mt: 0.5, flexShrink: 0 }} aria-hidden="true" />
                    )}
                    <Typography
                        variant="h6"
                        className="card-title"
                        sx={{
                            fontWeight: 'bold',
                            fontSize: '1.1rem',
                            transition: 'color 0.2s'
                        }}
                    >
                        {item.title}
                    </Typography>
                </Box>
                {(item.ratingType || item.rating !== undefined) && (
                    <Box sx={{ display: 'flex', alignItems: 'center', ml: 1, flexShrink: 0 }}>
                        {getRatingIcon(item.ratingType || item.rating)}
                    </Box>
                )}
            </Box>

            {/* CHIPS ROW: Media Type + Status + Optional Extras */}
            <Box sx={{ mb: 1.5, display: 'flex', gap: 1, flexWrap: 'wrap', alignItems: 'center' }}>
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
                        sx={{ fontSize: '0.7rem' }}
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

            {/* PRIMARY CREDIT (Author/Director/Creator/Channel) */}
            {getPrimaryCredit() && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1, fontWeight: 500 }}>
                    {getPrimaryCredit()}
                </Typography>
            )}

            {/* METADATA LINE (Year, duration, rating, etc) */}
            {getMetadataLine() && (
                <Typography variant="body2" color="text.secondary" sx={{ mb: 1.5, fontSize: '0.85rem' }}>
                    {getMetadataLine()}
                </Typography>
            )}

            {/* DESCRIPTION/NOTES - Truncated */}
            {item.notes && (
                <Typography 
                    variant="body2" 
                    sx={{ 
                        mb: 1.5,
                        overflow: 'hidden',
                        textOverflow: 'ellipsis',
                        display: '-webkit-box',
                        WebkitLineClamp: 2,
                        WebkitBoxOrient: 'vertical',
                        color: 'text.secondary'
                    }}
                >
                    {item.notes}
                </Typography>
            )}

            {/* TOPICS - First 3 with overflow indicator */}
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1 }}>
                {item.topics.slice(0, 3).map((topic) => (
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
                {item.topics.length > 3 && (
                    <Chip
                        label={`+${item.topics.length - 3}`}
                        size="small"
                        sx={{ fontSize: '0.75rem', height: '24px' }}
                    />
                )}
            </Box>

            {/* FOOTER: Date Added */}
            <Typography variant="caption" color="text.secondary" sx={{ display: 'flex', alignItems: 'center', gap: 0.5, fontSize: '0.8rem' }}>
                <AccessTime sx={{ fontSize: 14 }} />
                Added {new Date(item.dateAdded).toLocaleDateString()}
            </Typography>
        </CardContent>
    </Card>
    </Link>
    </Box>
    );
});

SearchResultCard.displayName = 'SearchResultCard';

