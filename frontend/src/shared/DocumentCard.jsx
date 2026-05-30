import React from 'react';
import { useNavigate } from 'react-router-dom';
import {
    Card, CardContent, Typography, Box,
    Chip, IconButton, Tooltip
} from '@mui/material';
import {
    Archive, Description, OpenInNew, Person,
    FolderOpen, CalendarToday, InsertDriveFile,
    PictureAsPdf, Image as ImageIcon
} from '@mui/icons-material';

function DocumentCard({ document }) {
    const navigate = useNavigate();

    const handleCardClick = () => {
        navigate(`/media/${document.id}`);
    };

    const handleOpenUrl = (e) => {
        e.stopPropagation();
        if (document.paperlessUrl) {
            window.open(document.paperlessUrl, '_blank', 'noopener,noreferrer');
        }
    };

    // Get appropriate icon based on file type
    const getFileIcon = () => {
        const fileType = (document.fileType || '').toLowerCase();
        if (fileType === 'pdf') {
            return <PictureAsPdf sx={{ fontSize: 80, opacity: 0.7 }} />;
        } else if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'].includes(fileType)) {
            return <ImageIcon sx={{ fontSize: 80, opacity: 0.7 }} />;
        }
        return <InsertDriveFile sx={{ fontSize: 80, opacity: 0.7 }} />;
    };

    // Get color based on document type
    const getDocumentTypeColor = () => {
        const docType = (document.documentType || '').toLowerCase();
        if (docType.includes('invoice')) return 'error';
        if (docType.includes('receipt')) return 'warning';
        if (docType.includes('contract')) return 'info';
        if (docType.includes('tax') || docType.includes('legal')) return 'secondary';
        return 'primary';
    };

    // Format file size
    const formatFileSize = (bytes) => {
        if (!bytes) return null;
        const sizes = ['B', 'KB', 'MB', 'GB'];
        let len = bytes;
        let order = 0;
        while (len >= 1024 && order < sizes.length - 1) {
            order++;
            len /= 1024;
        }
        return `${len.toFixed(order > 0 ? 1 : 0)} ${sizes[order]}`;
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
            {/* File Type Header */}
            <Box
                sx={{
                    height: 120,
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    bgcolor: document.isArchived ? 'success.light' : 'grey.200',
                    color: document.isArchived ? 'success.contrastText' : 'text.secondary',
                    position: 'relative'
                }}
            >
                {getFileIcon()}
                {document.fileType && (
                    <Box
                        sx={{
                            position: 'absolute',
                            top: 8,
                            right: 8,
                            bgcolor: 'background.paper',
                            borderRadius: 1,
                            px: 1,
                            py: 0.25
                        }}
                    >
                        <Typography variant="caption" fontWeight="bold" color="text.secondary">
                            {document.fileType.toUpperCase()}
                        </Typography>
                    </Box>
                )}
                {document.isArchived && (
                    <Box
                        sx={{
                            position: 'absolute',
                            top: 8,
                            left: 8
                        }}
                    >
                        <Tooltip title="Archived">
                            <Archive sx={{ color: 'white' }} />
                        </Tooltip>
                    </Box>
                )}
            </Box>

            <CardContent sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column' }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'start', mb: 1 }}>
                    <Typography
                        variant="h6"
                        component="div"
                        sx={{
                            flex: 1,
                            fontWeight: 600,
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            display: '-webkit-box',
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: 'vertical'
                        }}
                    >
                        {document.title}
                    </Typography>
                </Box>

                {document.correspondent && (
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 1 }}>
                        <Person sx={{ fontSize: 16, color: 'text.secondary' }} />
                        <Typography variant="body2" color="text.secondary">
                            {document.correspondent}
                        </Typography>
                    </Box>
                )}

                {document.description && (
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
                        {document.description}
                    </Typography>
                )}

                <Box sx={{ mt: 'auto' }}>
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
                        {document.documentType && (
                            <Chip
                                icon={<FolderOpen sx={{ fontSize: 16 }} />}
                                label={document.documentType}
                                color={getDocumentTypeColor()}
                                size="small"
                            />
                        )}

                        {document.documentDate && (
                            <Chip
                                icon={<CalendarToday sx={{ fontSize: 16 }} />}
                                label={new Date(document.documentDate).toLocaleDateString()}
                                size="small"
                                variant="outlined"
                            />
                        )}

                        {document.pageCount > 0 && (
                            <Chip
                                icon={<Description sx={{ fontSize: 16 }} />}
                                label={`${document.pageCount} page${document.pageCount > 1 ? 's' : ''}`}
                                size="small"
                                variant="outlined"
                            />
                        )}

                        {document.fileSizeBytes && (
                            <Chip
                                label={document.formattedFileSize || formatFileSize(document.fileSizeBytes)}
                                size="small"
                                variant="outlined"
                            />
                        )}
                    </Box>

                    {document.paperlessUrl && (
                        <Box sx={{ display: 'flex', justifyContent: 'flex-end' }}>
                            <Tooltip title="Open in Paperless-ngx">
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

                {document.topics && document.topics.length > 0 && (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mt: 1 }}>
                        {document.topics.slice(0, 3).map((topic) => (
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
                        {document.topics.length > 3 && (
                            <Chip
                                label={`+${document.topics.length - 3}`}
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

export default DocumentCard;
