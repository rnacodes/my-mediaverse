import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Card, CardContent, CardMedia, Typography, Box,
    Chip, LinearProgress, IconButton, Tooltip
} from '@mui/material';
import {
    Star, Archive, CheckCircle,
    OpenInNew, AccessTime, MenuBook
} from '@mui/icons-material';
import { formatStatus } from '@/utils/formatters';
import { resolveMediaImage, getPlaceholderImage } from '@/utils/mediaImageUtils';

function ArticleCard({ article }) {
    const navigate = useNavigate();

    const handleCardClick = () => {
        navigate(`/media/${article.id}`);
    };

    const handleOpenUrl = (e) => {
        e.stopPropagation();
        const url = article.effectiveUrl || article.originalUrl || article.link;
        if (url) {
            window.open(url, '_blank', 'noopener,noreferrer');
        }
    };

    const getStatusColor = (status) => {
        switch (status) {
            case 'Completed':
                return 'success';
            case 'ActivelyExploring':
                return 'primary';
            case 'Abandoned':
                return 'error';
            default:
                return 'default';
        }
    };

    return (
        <Card
            sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                cursor: 'pointer',
                transition: 'all 0.3s ease',
                '&:hover': {
                    transform: 'translateY(-4px)',
                    boxShadow: 6
                }
            }}
            onClick={handleCardClick}
        >
            <CardMedia
                component="img"
                height="180"
                image={resolveMediaImage(article, 'Article')}
                alt={article.title}
                onError={(e) => { e.target.onerror = null; e.target.src = getPlaceholderImage('Article'); }}
                sx={{
                  width: '100%',
                  maxHeight: 180,
                  objectFit: 'cover'
                }}
            />

            <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 1 }}>
                    <Typography variant="h6" component="div" sx={{ flex: 1, fontWeight: 600 }}>
                        {article.title}
                    </Typography>
                    <Box sx={{ display: 'flex', gap: 0.5, ml: 1 }}>
                        {article.isStarred && (
                            <Tooltip title="Starred">
                                <Star sx={{ color: 'warning.main', fontSize: 20 }} />
                            </Tooltip>
                        )}
                        {article.isArchived && (
                            <Tooltip title="Archived">
                                <Archive sx={{ color: 'text.secondary', fontSize: 20 }} />
                            </Tooltip>
                        )}
                        {article.isReadingCompleted && (
                            <Tooltip title="Completed">
                                <CheckCircle sx={{ color: 'success.main', fontSize: 20 }} />
                            </Tooltip>
                        )}
                    </Box>
                </Box>

                {article.author && (
                    <Typography variant="body2" color="text.secondary" gutterBottom>
                        By {article.author}
                    </Typography>
                )}

                {article.description && (
                    <Typography
                        variant="body2"
                        color="text.secondary"
                        sx={{
                            mb: 2,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            display: '-webkit-box',
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: 'vertical'
                        }}
                    >
                        {article.description}
                    </Typography>
                )}

                <Box sx={{ mt: 'auto' }}>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                        {article.status && (
                            <Chip
                                label={formatStatus(article.status)}
                                color={getStatusColor(article.status)}
                                size="small"
                            />
                        )}
                        
                        {article.publication && (
                            <Chip
                                label={article.publication}
                                size="small"
                                variant="outlined"
                            />
                        )}
                        
                        {article.estimatedReadingTimeMinutes > 0 && (
                            <Chip
                                icon={<AccessTime sx={{ fontSize: 16 }} />}
                                label={`${article.estimatedReadingTimeMinutes} min`}
                                size="small"
                                variant="outlined"
                            />
                        )}

                        {article.wordCount > 0 && (
                            <Chip
                                icon={<MenuBook sx={{ fontSize: 16 }} />}
                                label={`${article.wordCount.toLocaleString()} words`}
                                size="small"
                                variant="outlined"
                            />
                        )}
                    </Box>

                    {article.readingProgress > 0 && (
                        <Box sx={{ mb: 1 }}>
                            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                                <Typography variant="caption" color="text.secondary">
                                    Reading Progress
                                </Typography>
                                <Typography variant="caption" color="text.secondary" fontWeight="bold">
                                    {Math.round(article.readingProgress * 100)}%
                                </Typography>
                            </Box>
                            <LinearProgress
                                variant="determinate"
                                value={article.readingProgress * 100}
                                sx={{
                                    height: 6,
                                    borderRadius: 3,
                                    bgcolor: 'grey.200',
                                    '& .MuiLinearProgress-bar': {
                                        borderRadius: 3
                                    }
                                }}
                            />
                        </Box>
                    )}

                    {(article.effectiveUrl || article.originalUrl || article.link) && (
                        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                            <Tooltip title="Open article in new tab">
                                <IconButton
                                    size="small"
                                    onClick={handleOpenUrl}
                                    sx={{
                                        color: 'primary.main',
                                        '&:hover': {
                                            bgcolor: 'primary.light'
                                        }
                                    }}
                                >
                                    <OpenInNew fontSize="small" />
                                </IconButton>
                            </Tooltip>
                        </Box>
                    )}
                </Box>

                {article.topics && article.topics.length > 0 && (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mt: 1 }}>
                        {article.topics.slice(0, 3).map((topic) => (
                            <Chip
                                key={`topic-${topic}`}
                                label={topic}
                                size="small"
                                sx={{
                                    bgcolor: 'secondary.light',
                                    color: 'secondary.contrastText',
                                    fontSize: '0.7rem',
                                    height: 20
                                }}
                            />
                        ))}
                        {article.topics.length > 3 && (
                            <Chip
                                label={`+${article.topics.length - 3}`}
                                size="small"
                                sx={{
                                    bgcolor: 'grey.300',
                                    fontSize: '0.7rem',
                                    height: 20
                                }}
                            />
                        )}
                    </Box>
                )}
            </CardContent>
        </Card>
    );
}

export default ArticleCard;

