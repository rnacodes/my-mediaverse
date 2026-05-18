import React, { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Box,
  Typography,
  Card,
  CardContent,
  CircularProgress,
  Chip,
  Alert,
  IconButton,
  Tooltip,
  List,
  ListItem,
  ListItemText,
  Collapse,
} from '@mui/material';
import {
  AutoAwesome as AutoAwesomeIcon,
  Refresh as RefreshIcon,
  Note as NoteIcon,
  Folder as FolderIcon,
  ExpandMore as ExpandMoreIcon,
  ExpandLess as ExpandLessIcon,
} from '@mui/icons-material';
import { useSimilarNotes } from '../hooks/useRecommendation';

// Vault color mapping
const vaultColors = {
  general: '#4caf50',
  programming: '#2196f3',
};

function SimilarNotesSection({ note }) {
  const [expanded, setExpanded] = useState(false);

  const getVaultColor = (vaultName) => {
    return vaultColors[vaultName?.toLowerCase()] || '#9e9e9e';
  };

  const query = useSimilarNotes(note?.id, 6, null, {
    enabled: !!note?.id && expanded,
  });
  const similarNotes = query.data ?? [];
  const loading = query.isFetching;
  const hasFetched = query.isFetched;

  const fetchError = query.error;
  const hasEmbedding = !fetchError
    || !(fetchError.response?.status === 400 || fetchError.response?.data?.message?.includes('embedding'));
  const error = fetchError && hasEmbedding
    ? (fetchError.response?.data?.message || fetchError.message || 'Failed to load similar notes')
    : null;

  const handleExpandClick = () => {
    setExpanded((prev) => !prev);
  };

  // Don't render if no embedding (only show after we've fetched)
  if (!hasEmbedding && hasFetched) {
    return (
      <Card sx={{ mb: 3 }}>
        <CardContent>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
            <AutoAwesomeIcon color="action" />
            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
              Similar Notes
            </Typography>
          </Box>
          <Alert severity="info">
            Generate embeddings in the AI Admin page to enable similar note recommendations.
          </Alert>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card sx={{ mb: 3 }}>
      <CardContent sx={{ pb: expanded ? 2 : '16px !important' }}>
        {/* Clickable header for expand/collapse */}
        <Box
          onClick={handleExpandClick}
          sx={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            cursor: 'pointer',
            '&:hover': { opacity: 0.8 },
          }}
        >
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <AutoAwesomeIcon color={expanded ? 'primary' : 'action'} />
            <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
              Similar Notes
            </Typography>
            <Chip label="AI" size="small" color="secondary" sx={{ ml: 1 }} />
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
            {expanded && hasFetched && (
              <Tooltip title="Refresh recommendations">
                <IconButton
                  onClick={(e) => {
                    e.stopPropagation();
                    query.refetch();
                  }}
                  disabled={loading}
                  size="small"
                >
                  <RefreshIcon />
                </IconButton>
              </Tooltip>
            )}
            <IconButton size="small">
              {expanded ? <ExpandLessIcon /> : <ExpandMoreIcon />}
            </IconButton>
          </Box>
        </Box>

        {/* Collapsible content */}
        <Collapse in={expanded}>
          <Box sx={{ mt: 2 }}>
            {loading && (
              <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                <CircularProgress size={32} />
              </Box>
            )}

            {error && (
              <Alert severity="error" sx={{ mb: 2 }}>
                {error}
              </Alert>
            )}

            {!loading && !error && hasFetched && similarNotes.length === 0 && (
              <Typography variant="body2" color="text.secondary" sx={{ textAlign: 'center', py: 2 }}>
                No similar notes found
              </Typography>
            )}

            {!loading && !error && similarNotes.length > 0 && (
              <List disablePadding>
                {similarNotes.map((similarNote) => (
                  <ListItem
                    key={similarNote.id}
                    component={RouterLink}
                    to={`/note/${similarNote.id}`}
                    sx={{
                      border: '1px solid',
                      borderColor: 'divider',
                      borderRadius: 1,
                      mb: 1,
                      textDecoration: 'none',
                      '&:hover': {
                        bgcolor: 'action.hover',
                      },
                    }}
                  >
                    <NoteIcon sx={{ mr: 2, color: 'text.secondary' }} />
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                          <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                            {similarNote.title}
                          </Typography>
                          <Chip
                            icon={<FolderIcon sx={{ fontSize: '0.8rem !important' }} />}
                            label={similarNote.vaultName}
                            size="small"
                            sx={{
                              bgcolor: getVaultColor(similarNote.vaultName),
                              color: 'white',
                              fontSize: '0.7rem',
                              height: 22,
                            }}
                          />
                        </Box>
                      }
                      secondary={
                        similarNote.description || similarNote.aiDescription ? (
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
                            {similarNote.description || similarNote.aiDescription}
                          </Typography>
                        ) : null
                      }
                    />
                  </ListItem>
                ))}
              </List>
            )}
          </Box>
        </Collapse>
      </CardContent>
    </Card>
  );
}

export default React.memo(SimilarNotesSection);
