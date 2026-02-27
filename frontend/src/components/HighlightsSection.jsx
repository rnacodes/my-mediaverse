import React, { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
    Box, Card, CardContent, Typography, CircularProgress, Chip, Paper, Link, IconButton, Collapse
} from '@mui/material';
import { Notes, OpenInNew, Star, ExpandMore, ExpandLess } from '@mui/icons-material';

function HighlightsSection({ mediaItem, highlights, highlightsLoading }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <React.Fragment>
      {(mediaItem.mediaType === 'Article' || mediaItem.mediaType === 'Book') && (
        <Card
          sx={{
            width: '100%',
            maxWidth: '1200px',
            mt: 3,
            backgroundColor: 'rgba(255, 255, 255, 0.05)',
            backdropFilter: 'blur(10px)',
            borderRadius: { xs: '8px', sm: '16px' },
            boxShadow: '0 8px 32px rgba(0, 0, 0, 0.3)',
            border: '1px solid rgba(255, 255, 255, 0.1)'
          }}
        >
          <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
            <Box
              sx={{
                display: 'flex',
                alignItems: 'center',
                cursor: 'pointer',
                flexWrap: 'wrap',
                gap: 1
              }}
              onClick={() => setExpanded(!expanded)}
            >
              <Notes sx={{ fontSize: { xs: 24, sm: 32 }, mr: 1, color: '#FFD700' }} />
              <Typography
                variant="h5"
                sx={{
                  fontWeight: 'bold',
                  color: '#ffffff',
                  fontSize: { xs: '1.25rem', sm: '1.5rem' }
                }}
              >
                Highlights
              </Typography>
              {!highlightsLoading && highlights.length > 0 && (
                <Chip
                  label={highlights.length}
                  size="small"
                  sx={{
                    ml: { xs: 0, sm: 2 },
                    backgroundColor: 'rgba(255, 215, 0, 0.2)',
                    color: '#FFD700',
                    fontWeight: 'bold'
                  }}
                />
              )}
              <Box sx={{ flexGrow: 1 }} />
              <IconButton size="small" sx={{ color: '#fcfafa' }}>
                {expanded ? <ExpandLess /> : <ExpandMore />}
              </IconButton>
            </Box>

            <Collapse in={expanded} timeout="auto">
              <Box sx={{ mt: 2 }}>
                {highlightsLoading ? (
                  <Box sx={{ textAlign: 'center', py: 3 }}>
                    <CircularProgress size={32} />
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
                      Loading highlights...
                    </Typography>
                  </Box>
                ) : highlights.length > 0 ? (
                  <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                    {highlights.map((highlight, index) => (
                      <Paper
                        key={highlight.id || index}
                        elevation={2}
                        sx={{
                          p: { xs: 2, sm: 3 },
                          backgroundColor: 'rgba(255, 255, 255, 0.03)',
                          border: '1px solid rgba(255, 215, 0, 0.2)',
                          borderLeft: { xs: '3px solid #FFD700', sm: '4px solid #FFD700' },
                          borderRadius: 2,
                          transition: 'all 0.2s ease',
                          '&:hover': {
                            backgroundColor: 'rgba(255, 255, 255, 0.05)',
                            transform: { xs: 'none', sm: 'translateX(4px)' },
                            boxShadow: '0 4px 12px rgba(255, 215, 0, 0.2)'
                          }
                        }}
                      >
                        <Link
                          component={RouterLink}
                          to={`/highlight/${highlight.id}`}
                          sx={{
                            textDecoration: 'none',
                            color: 'inherit',
                            display: 'block',
                            '&:hover': {
                              textDecoration: 'none'
                            }
                          }}
                        >
                          <Typography
                            variant="body1"
                            sx={{
                              fontSize: { xs: '1rem', sm: '1.1rem' },
                              lineHeight: 1.7,
                              color: '#ffffff',
                              mb: 2,
                              fontStyle: 'italic'
                            }}
                          >
                            {highlight.text || highlight.highlightText}
                          </Typography>
                        </Link>

                        {highlight.note && (
                          <Box
                            sx={{
                              mt: 2,
                              p: { xs: 1.5, sm: 2 },
                              backgroundColor: 'rgba(100, 150, 255, 0.1)',
                              borderLeft: '3px solid #6496FF',
                              borderRadius: 1
                            }}
                          >
                            <Typography
                              variant="body2"
                              sx={{
                                color: '#B0C4DE',
                                fontStyle: 'normal',
                                fontSize: { xs: '0.95rem', sm: '1rem' }
                              }}
                            >
                              <strong style={{ color: '#6496FF' }}>Note:</strong> {highlight.note}
                            </Typography>
                          </Box>
                        )}

                        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mt: 2, alignItems: 'center' }}>
                          {highlight.highlightedAt && (
                            <Chip
                              label={new Date(highlight.highlightedAt).toLocaleDateString()}
                              size="small"
                              variant="outlined"
                              sx={{
                                borderColor: 'rgba(255, 255, 255, 0.3)',
                                color: 'rgba(255, 255, 255, 0.7)',
                                fontSize: '0.75rem'
                              }}
                            />
                          )}
                          {highlight.location && (
                            <Chip
                              label={`Location: ${highlight.location}`}
                              size="small"
                              variant="outlined"
                              sx={{
                                borderColor: 'rgba(255, 255, 255, 0.3)',
                                color: 'rgba(255, 255, 255, 0.7)',
                                fontSize: '0.75rem'
                              }}
                            />
                          )}
                          {highlight.tags && highlight.tags.length > 0 && (
                            <>
                              {highlight.tags.slice(0, 3).map((tag, tagIndex) => (
                                <Chip
                                  key={tagIndex}
                                  label={tag}
                                  size="small"
                                  sx={{
                                    backgroundColor: 'rgba(255, 215, 0, 0.15)',
                                    color: '#FFD700',
                                    fontSize: '0.75rem'
                                  }}
                                />
                              ))}
                              {highlight.tags.length > 3 && (
                                <Chip
                                  label={`+${highlight.tags.length - 3} more`}
                                  size="small"
                                  sx={{
                                    backgroundColor: 'rgba(255, 215, 0, 0.1)',
                                    color: '#FFD700',
                                    fontSize: '0.75rem'
                                  }}
                                />
                              )}
                            </>
                          )}
                        </Box>

                        <Box sx={{ display: 'flex', gap: 2, mt: 2, alignItems: 'center' }}>
                          {highlight.id && (
                            <Link
                              component={RouterLink}
                              to={`/highlight/${highlight.id}`}
                              sx={{
                                color: '#FFD700',
                                fontSize: '0.875rem',
                                textDecoration: 'none',
                                display: 'flex',
                                alignItems: 'center',
                                gap: 0.5,
                                '&:hover': {
                                  textDecoration: 'underline'
                                }
                              }}
                            >
                              View Highlight
                            </Link>
                          )}
                          {highlight.url && (
                            <Link
                              href={highlight.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              sx={{
                                color: '#6496FF',
                                fontSize: '0.875rem',
                                textDecoration: 'none',
                                display: 'flex',
                                alignItems: 'center',
                                gap: 0.5,
                                '&:hover': {
                                  textDecoration: 'underline'
                                }
                              }}
                            >
                              View in Readwise <OpenInNew sx={{ fontSize: 16 }} />
                            </Link>
                          )}
                        </Box>
                      </Paper>
                    ))}
                  </Box>
                ) : (
                  <Box sx={{ textAlign: 'center', py: 3 }}>
                    <Typography variant="body1" color="text.secondary">
                      No highlights found for this {mediaItem.mediaType.toLowerCase()}.
                    </Typography>
                    <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                      Visit the <Link component={RouterLink} to="/readwise-sync" sx={{ color: '#FFD700' }}>Readwise Sync page</Link> to import highlights.
                    </Typography>
                  </Box>
                )}
              </Box>
            </Collapse>
          </CardContent>
        </Card>
      )}
    </React.Fragment>
  );
}

export default React.memo(HighlightsSection);
