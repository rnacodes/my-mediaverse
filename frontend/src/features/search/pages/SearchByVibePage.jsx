import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Container,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  CardMedia,
  Chip,
  Button,
  ButtonGroup,
  List,
  ListItem,
  ListItemText,
  CircularProgress,
  TextField,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Slider,
  Paper,
  Alert,
} from '@mui/material';
import {
  ViewModule,
  ViewList,
  Search as SearchIcon,
  AutoAwesome as AutoAwesomeIcon,
} from '@mui/icons-material';
import { useVibeSearch } from '@/hooks/useRecommendation';
import { formatMediaType } from '@/utils/formatters';

function SearchByVibePage() {
  const [query, setQuery] = useState('');
  const [mediaType, setMediaType] = useState('');
  const [limit, setLimit] = useState(20);
  const [viewMode, setViewMode] = useState('card');
  const [validationError, setValidationError] = useState(null);

  // Submitted-* mirror the active query — only changes on explicit submit.
  const [submittedQuery, setSubmittedQuery] = useState('');
  const [submittedMediaType, setSubmittedMediaType] = useState('');
  const [submittedLimit, setSubmittedLimit] = useState(20);

  const vibeQuery = useVibeSearch(submittedQuery, submittedMediaType || null, submittedLimit);
  const results = vibeQuery.data ?? [];
  const loading = vibeQuery.isFetching;
  const hasSearched = !!submittedQuery;

  const fetchError = vibeQuery.error;
  const fetchErrorMessage = fetchError
    ? (fetchError.response?.status === 503 || fetchError.response?.data?.message?.includes('unavailable')
        ? 'Vibe search requires embeddings. Please generate embeddings in the AI Admin page first.'
        : (fetchError.response?.data?.message || fetchError.message || 'Search failed'))
    : null;
  const error = validationError || fetchErrorMessage;

  const mediaTypes = [
    { value: '', label: 'All Types' },
    { value: 'Book', label: 'Books' },
    { value: 'Article', label: 'Articles' },
    { value: 'Movie', label: 'Movies' },
    { value: 'TVShow', label: 'TV Shows' },
    { value: 'Video', label: 'Videos' },
    { value: 'Podcast', label: 'Podcasts' },
    { value: 'Website', label: 'Websites' },
  ];

  const handleSearch = () => {
    if (!query.trim()) {
      setValidationError('Please enter a description of what you\'re looking for');
      return;
    }

    setValidationError(null);
    setSubmittedQuery(query);
    setSubmittedMediaType(mediaType);
    setSubmittedLimit(limit);
  };

  const handleKeyPress = (e) => {
    if (e.key === 'Enter') {
      handleSearch();
    }
  };

  const renderCardView = () => (
    <Grid container spacing={3} sx={{ mt: 2 }}>
      {results.map((item) => (
        <Grid item xs={12} sm={6} md={4} lg={3} key={item.id}>
          <Card
            component={Link}
            to={`/media/${item.id}`}
            sx={{
              textDecoration: 'none',
              height: '100%',
              display: 'flex',
              flexDirection: 'column',
              '&:hover': {
                transform: 'translateY(-2px)',
                boxShadow: 4
              },
              transition: 'all 0.2s ease-in-out'
            }}
          >
            {item.thumbnail && (
              <CardMedia
                component="img"
                height="140"
                image={item.thumbnail}
                alt={item.title}
                sx={{ objectFit: 'cover' }}
                onError={(e) => {
                  e.target.style.display = 'none';
                }}
              />
            )}
            <CardContent sx={{ flexGrow: 1 }}>
              <Typography
                variant="subtitle1"
                sx={{
                  fontWeight: 'bold',
                  overflow: 'hidden',
                  textOverflow: 'ellipsis',
                  display: '-webkit-box',
                  WebkitLineClamp: 2,
                  WebkitBoxOrient: 'vertical',
                  mb: 1,
                }}
              >
                {item.title}
              </Typography>

              <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mb: 1 }}>
                <Chip
                  label={formatMediaType(item.mediaType)}
                  size="small"
                  color="primary"
                />
              </Box>

              {item.description && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    display: '-webkit-box',
                    WebkitLineClamp: 3,
                    WebkitBoxOrient: 'vertical',
                  }}
                >
                  {item.description}
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>
      ))}
    </Grid>
  );

  const renderListView = () => (
    <List sx={{ mt: 2 }}>
      {results.map((item) => (
        <ListItem
          key={item.id}
          component={Link}
          to={`/media/${item.id}`}
          sx={{
            border: '1px solid',
            borderColor: 'divider',
            borderRadius: 1,
            mb: 1,
            textDecoration: 'none',
            '&:hover': {
              bgcolor: 'action.hover'
            }
          }}
        >
          {item.thumbnail && (
            <CardMedia
              component="img"
              sx={{ width: 80, height: 60, objectFit: 'cover', borderRadius: 1, mr: 2 }}
              image={item.thumbnail}
              alt={item.title}
              onError={(e) => {
                e.target.style.display = 'none';
              }}
            />
          )}
          <ListItemText
            primary={
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flexWrap: 'wrap' }}>
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                  {item.title}
                </Typography>
                <Chip
                  label={formatMediaType(item.mediaType)}
                  size="small"
                  color="primary"
                />
              </Box>
            }
            secondary={
              item.description && (
                <Typography
                  variant="body2"
                  color="text.secondary"
                  sx={{
                    overflow: 'hidden',
                    textOverflow: 'ellipsis',
                    display: '-webkit-box',
                    WebkitLineClamp: 2,
                    WebkitBoxOrient: 'vertical',
                    mt: 0.5,
                  }}
                >
                  {item.description}
                </Typography>
              )
            }
          />
        </ListItem>
      ))}
    </List>
  );

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 4 }}>
        <AutoAwesomeIcon sx={{ fontSize: 40, color: 'primary.main' }} />
        <Box>
          <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
            Search by Vibe
          </Typography>
          <Typography variant="body1" color="text.secondary">
            Describe what you&apos;re looking for in natural language
          </Typography>
        </Box>
      </Box>

      {/* Search Form */}
      <Paper elevation={3} sx={{ p: 3, mb: 4 }}>
        <TextField
          fullWidth
          multiline
          rows={3}
          label="Describe what you're looking for..."
          placeholder="e.g., 'inspiring documentaries about nature and conservation' or 'thought-provoking books about artificial intelligence and its impact on society'"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onKeyPress={handleKeyPress}
          sx={{ mb: 3 }}
        />

        <Grid container spacing={3} alignItems="center">
          <Grid item xs={12} sm={4}>
            <FormControl fullWidth>
              <InputLabel>Media Type</InputLabel>
              <Select
                value={mediaType}
                label="Media Type"
                onChange={(e) => setMediaType(e.target.value)}
              >
                {mediaTypes.map((type) => (
                  <MenuItem key={type.value} value={type.value}>
                    {type.label}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Grid>

          <Grid item xs={12} sm={4}>
            <Typography gutterBottom>
              Results Limit: {limit}
            </Typography>
            <Slider
              value={limit}
              onChange={(e, value) => setLimit(value)}
              min={5}
              max={50}
              step={5}
              marks={[
                { value: 5, label: '5' },
                { value: 25, label: '25' },
                { value: 50, label: '50' },
              ]}
            />
          </Grid>

          <Grid item xs={12} sm={4}>
            <Button
              variant="contained"
              size="large"
              fullWidth
              onClick={handleSearch}
              disabled={loading || !query.trim()}
              startIcon={loading ? <CircularProgress size={20} color="inherit" /> : <SearchIcon />}
              sx={{ height: 56 }}
            >
              {loading ? 'Searching...' : 'Search'}
            </Button>
          </Grid>
        </Grid>
      </Paper>

      {/* Error Display */}
      {error && (
        <Alert severity="error" sx={{ mb: 3 }}>
          {error}
        </Alert>
      )}

      {/* Results Section */}
      {hasSearched && !loading && !error && (
        <>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">
              {results.length > 0
                ? `Found ${results.length} result${results.length !== 1 ? 's' : ''}`
                : 'No results found'}
            </Typography>

            {results.length > 0 && (
              <ButtonGroup>
                <Button
                  variant={viewMode === 'card' ? 'contained' : 'outlined'}
                  onClick={() => setViewMode('card')}
                >
                  <ViewModule />
                </Button>
                <Button
                  variant={viewMode === 'list' ? 'contained' : 'outlined'}
                  onClick={() => setViewMode('list')}
                >
                  <ViewList />
                </Button>
              </ButtonGroup>
            )}
          </Box>

          {results.length === 0 && (
            <Box sx={{ textAlign: 'center', py: 8 }}>
              <AutoAwesomeIcon sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
              <Typography variant="h6" color="text.secondary">
                No matching items found
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Try a different description or broaden your search
              </Typography>
            </Box>
          )}

          {results.length > 0 && (viewMode === 'card' ? renderCardView() : renderListView())}
        </>
      )}

      {/* Initial State */}
      {!hasSearched && !loading && (
        <Box sx={{ textAlign: 'center', py: 8 }}>
          <AutoAwesomeIcon sx={{ fontSize: 80, color: 'action.disabled', mb: 2 }} />
          <Typography variant="h5" color="text.secondary" gutterBottom>
            Describe your perfect media
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ maxWidth: 600, mx: 'auto' }}>
            Use natural language to find content that matches your mood or interests.
            Our AI will search through your library to find semantically similar items.
          </Typography>
        </Box>
      )}
    </Container>
  );
}

export default SearchByVibePage;
