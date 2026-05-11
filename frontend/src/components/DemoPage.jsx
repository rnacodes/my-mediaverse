import React, { useState } from 'react';
import {
  Container,
  Box,
  Typography,
  Grid,
  Button,
  Chip,
  Paper,
  Card,
  CardContent,
  CardMedia,
  TextField,
  CircularProgress,
  Skeleton,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  ButtonGroup,
  List,
  ListItem,
  ListItemIcon,
  ListItemText
} from '@mui/material';
import WhiteOutlineButton from './shared/WhiteOutlineButton';
import MediaCard from './shared/MediaCard';
import { formatMediaType, formatStatus, getRatingIcon, getRatingText } from '../utils/formatters';
import {
  Book,
  Movie,
  Tv,
  Article,
  LibraryMusic,
  Podcasts,
  SportsEsports,
  YouTube,
  Language,
  MenuBook,
  AutoAwesome,
  Search,
  ViewModule,
  ViewList
} from '@mui/icons-material';
import { COLORS, SPACING, BORDER_RADIUS, SHADOWS, TRANSITIONS } from './shared';
import SearchBar from './shared/SearchBar';
import SimpleMediaCarousel from './shared/SimpleMediaCarousel';

// Mock data for demonstration
const mockMediaItems = [
  {
    id: 1,
    title: 'The Great Gatsby',
    mediaType: 'book',
    status: 'completed',
    rating: 4.5,
    notes: 'A classic American novel about the Jazz Age.',
    dateAdded: '2024-01-15',
    thumbnailUrl: 'https://placehold.co/600x400/474350/fcfafa?text=The+Great+Gatsby'
  },
  {
    id: 2,
    title: 'Inception',
    mediaType: 'movie',
    status: 'actively exploring',
    rating: 4.8,
    notes: 'Mind-bending sci-fi thriller about dreams within dreams.',
    dateAdded: '2024-01-20',
    thumbnailUrl: 'https://placehold.co/600x400/474350/fcfafa?text=Inception'
  },
  {
    id: 3,
    title: 'Breaking Bad',
    mediaType: 'tv',
    status: 'completed',
    rating: 5.0,
    notes: 'One of the greatest TV series ever made.',
    dateAdded: '2024-01-10',
    thumbnailUrl: 'https://placehold.co/600x400/474350/fcfafa?text=Breaking+Bad'
  },
  {
    id: 4,
    title: 'The Joe Rogan Experience',
    mediaType: 'podcast',
    status: 'uncharted',
    rating: 4.2,
    notes: 'Long-form conversations with interesting guests.',
    dateAdded: '2024-01-25',
    thumbnailUrl: 'https://placehold.co/600x400/474350/fcfafa?text=JRE'
  },
  {
    id: 5,
    title: 'Cyberpunk 2077',
    mediaType: 'game',
    status: 'abandoned',
    rating: 3.5,
    notes: 'Open-world RPG set in a dystopian future.',
    dateAdded: '2024-01-30',
    thumbnailUrl: 'https://placehold.co/600x400/474350/fcfafa?text=Cyberpunk+2077'
  },
  {
    id: 6,
    title: 'React Documentation',
    mediaType: 'document',
    status: 'actively exploring',
    rating: 4.7,
    notes: 'Official React documentation and tutorials.',
    dateAdded: '2024-02-01',
    thumbnailUrl: 'https://placehold.co/600x400/474350/fcfafa?text=React+Docs'
  }
];

// Media type icons mapping
const mediaTypeIcons = {
  podcast: <Podcasts />,
  book: <Book />,
  movie: <Movie />,
  tv: <Tv />,
  article: <Article />,
  music: <LibraryMusic />,
  game: <SportsEsports />,
  video: <YouTube />,
  website: <Language />,
  document: <MenuBook />,
  default: <AutoAwesome />
};

const DemoPage = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [selectedMedia, setSelectedMedia] = useState(null);
  const [viewMode, setViewMode] = useState('card');

  const handleMediaClick = (media) => {
    setSelectedMedia(media);
    console.log('Selected media:', media);
  };

  const toggleLoading = () => {
    setLoading(!loading);
  };

  const getMediaIcon = (mediaType) => {
    const type = mediaType?.toLowerCase();
    return mediaTypeIcons[type] || mediaTypeIcons.default;
  };

  const getStatusColor = (status) => {
    switch (status?.toLowerCase()) {
      case 'uncharted':
        return COLORS.status.uncharted;
      case 'actively exploring':
        return COLORS.status.activelyExploring;
      case 'completed':
        return COLORS.status.completed;
      case 'abandoned':
        return COLORS.status.abandoned;
      default:
        return COLORS.text.secondary;
    }
  };

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h1" gutterBottom>
        Design System Demo
      </Typography>
      <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
        Showcasing the shared components and consistent design system
      </Typography>

      {/* Search Bar Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Functional Search Bar Component
        </Typography>
        <Typography variant="body1" sx={{ mb: 3, color: 'text.secondary' }}>
          Search through your media library and mixlists. Try searching for titles, descriptions, or media types.
        </Typography>
        <Box sx={{ maxWidth: '700px' }}>
          <SearchBar
            placeholder="Search media and mixlists..."
            onSearch={(query, results) => {
              console.log('Search results:', results);
              setSearchQuery(query);
            }}
            showSuggestions={true}
          />
        </Box>
        {searchQuery && (
          <Typography variant="body2" sx={{ mt: 2 }}>
            Search query: "{searchQuery}"
          </Typography>
        )}
      </Paper>

      {/* Loading Spinner Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Loading Spinner Component
        </Typography>
        <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap', mb: 2 }}>
          <Button variant="contained" onClick={toggleLoading}>
            Toggle Loading
          </Button>
        </Box>
        <Grid container spacing={2}>
          <Grid item xs={12} md={3}>
            <Box sx={{ textAlign: 'center' }}>
              <CircularProgress size={24} />
              <Typography variant="body2" sx={{ mt: 1 }}>Small spinner</Typography>
            </Box>
          </Grid>
          <Grid item xs={12} md={3}>
            <Box sx={{ textAlign: 'center' }}>
              <CircularProgress size={40} />
              <Typography variant="body2" sx={{ mt: 1 }}>Medium spinner</Typography>
            </Box>
          </Grid>
          <Grid item xs={12} md={3}>
            <Box sx={{ textAlign: 'center' }}>
              <CircularProgress size={60} />
              <Typography variant="body2" sx={{ mt: 1 }}>Large spinner</Typography>
            </Box>
          </Grid>
          <Grid item xs={12} md={3}>
            <Box sx={{ textAlign: 'center' }}>
              <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', gap: 1 }}>
                {[...Array(3)].map((_, index) => (
                  <Box
                    key={index}
                    sx={{
                      width: 8,
                      height: 8,
                      borderRadius: '50%',
                      backgroundColor: COLORS.primary.main,
                      animation: 'pulse 1.4s ease-in-out infinite both',
                      animationDelay: `${index * 0.16}s`
                    }}
                  />
                ))}
              </Box>
              <Typography variant="body2" sx={{ mt: 1 }}>Dots loader</Typography>
            </Box>
          </Grid>
        </Grid>
        {loading && (
          <Box sx={{ mt: 2 }}>
            <Grid container spacing={2}>
              {[...Array(6)].map((_, index) => (
                <Grid item xs={12} sm={6} md={4} key={index}>
                  <Skeleton
                    variant="rectangular"
                    height={200}
                    sx={{
                      borderRadius: '16px',
                      backgroundColor: COLORS.background.elevated
                    }}
                  />
                  <Box sx={{ mt: 1 }}>
                    <Skeleton
                      variant="text"
                      width="80%"
                      sx={{ backgroundColor: COLORS.background.elevated }}
                    />
                    <Skeleton
                      variant="text"
                      width="60%"
                      sx={{ backgroundColor: COLORS.background.elevated }}
                    />
                  </Box>
                </Grid>
              ))}
            </Grid>
          </Box>
        )}
      </Paper>

      {/* Media Card Variants Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          MediaCard Component Variants
        </Typography>
        <Typography variant="body1" sx={{ mb: 3, color: 'text.secondary' }}>
          The shared MediaCard component supports three variants for different display contexts.
        </Typography>

        {/* Default Variant */}
        <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
          Default Variant
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Standard card with 180px image, title, chips, rating, description, and date.
        </Typography>
        <Grid container spacing={3} sx={{ mb: 4 }}>
          {mockMediaItems.slice(0, 3).map((media) => (
            <Grid item xs={12} sm={6} md={4} key={media.id}>
              <MediaCard
                media={media}
                variant="default"
                onClick={handleMediaClick}
              />
            </Grid>
          ))}
        </Grid>

        {/* Compact Variant */}
        <Typography variant="h6" gutterBottom>
          Compact Variant
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Smaller card with 120px image, no description. Ideal for dense layouts.
        </Typography>
        <Grid container spacing={3} sx={{ mb: 4 }}>
          {mockMediaItems.slice(3, 6).map((media) => (
            <Grid item xs={12} sm={6} md={4} key={media.id}>
              <MediaCard
                media={media}
                variant="compact"
                onClick={handleMediaClick}
              />
            </Grid>
          ))}
        </Grid>

        {/* Featured Variant */}
        <Typography variant="h6" gutterBottom>
          Featured Variant
        </Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Large card (400px height) with 200px image and gradient background. Used for hero sections.
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} sm={6}>
            <MediaCard
              media={mockMediaItems[0]}
              variant="featured"
              onClick={handleMediaClick}
            />
          </Grid>
          <Grid item xs={12} sm={6}>
            <MediaCard
              media={mockMediaItems[1]}
              variant="featured"
              onClick={handleMediaClick}
            />
          </Grid>
        </Grid>

        {selectedMedia && (
          <Box sx={{ mt: 2, p: 2, backgroundColor: COLORS.background.elevated, borderRadius: 1 }}>
            <Typography variant="body2">
              Selected: {selectedMedia.title} ({selectedMedia.mediaType})
            </Typography>
          </Box>
        )}
      </Paper>

      {/* Rating System Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Rating System
        </Typography>
        <Typography variant="body1" sx={{ mb: 3, color: 'text.secondary' }}>
          Four-tier rating system using icons and colors. Accepts both numeric values (0-3) and string names.
        </Typography>
        <Grid container spacing={3}>
          {[
            { value: 0, name: 'superlike', color: '#e91e63' },
            { value: 1, name: 'like', color: '#4caf50' },
            { value: 2, name: 'neutral', color: '#9e9e9e' },
            { value: 3, name: 'dislike', color: '#f44336' }
          ].map((rating) => (
            <Grid item xs={6} sm={3} key={rating.value}>
              <Box sx={{
                textAlign: 'center',
                p: 3,
                border: '1px solid',
                borderColor: 'divider',
                borderRadius: 2,
                backgroundColor: COLORS.background.elevated
              }}>
                <Box sx={{ mb: 2, '& svg': { fontSize: 48 } }}>
                  {getRatingIcon(rating.value)}
                </Box>
                <Typography variant="h6" sx={{ mb: 0.5 }}>
                  {getRatingText(rating.value)}
                </Typography>
                <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
                  Value: {rating.value}
                </Typography>
                <Typography variant="caption" sx={{ color: rating.color }}>
                  {rating.color}
                </Typography>
              </Box>
            </Grid>
          ))}
        </Grid>
      </Paper>

      {/* Carousel Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Simple Media Carousel Component
        </Typography>
        <SimpleMediaCarousel
          mediaItems={mockMediaItems}
          title="Featured Media"
          subtitle="Simple carousel with navigation and pagination"
          onMediaClick={handleMediaClick}
        />
      </Paper>

      {/* Design System Colors Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Design System Colors
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Media Type Colors
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
              {Object.entries(COLORS.mediaTypes).map(([type, color]) => (
                <Chip
                  key={type}
                  label={type}
                  sx={{
                    backgroundColor: color,
                    color: 'white',
                    fontWeight: 'bold',
                    minWidth: '80px',
                    textAlign: 'center'
                  }}
                />
              ))}
            </Box>
            <Typography variant="body2" color="text.secondary">
              Each media type has a distinct color for easy identification
            </Typography>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Status Colors
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 2 }}>
              {['uncharted', 'actively exploring', 'completed', 'abandoned'].map((status) => (
                <Chip
                  key={status}
                  label={status}
                  sx={{
                    backgroundColor: getStatusColor(status),
                    color: 'white',
                    fontWeight: 'bold',
                    minWidth: '120px',
                    textAlign: 'center'
                  }}
                />
              ))}
            </Box>
            <Typography variant="body2" color="text.secondary">
              Status colors help track progress through your media
            </Typography>
          </Grid>
          <Grid item xs={12}>
            <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
              Color Palette
            </Typography>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.primary.main, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Primary</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.primary.main}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.secondary.main, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Secondary</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.secondary.main}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.background.default, borderRadius: 1, mb: 1, border: '1px solid', borderColor: 'divider' }} />
                  <Typography variant="body2" fontWeight="bold">Background</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.background.default}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.background.paper, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Paper</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.background.paper}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.background.elevated, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Elevated</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.background.elevated}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.text.primary, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Text Primary</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.text.primary}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.text.disabled, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Disabled</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.text.disabled}</Typography>
                </Box>
              </Grid>
              <Grid item xs={12} sm={6} md={3}>
                <Box sx={{ textAlign: 'center', p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                  <Box sx={{ width: '100%', height: 60, backgroundColor: COLORS.text.hint, borderRadius: 1, mb: 1 }} />
                  <Typography variant="body2" fontWeight="bold">Hint</Typography>
                  <Typography variant="caption" color="text.secondary">{COLORS.text.hint}</Typography>
                </Box>
              </Grid>
            </Grid>
          </Grid>
        </Grid>
      </Paper>

      {/* View Mode Toggle Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          View Mode Toggle Pattern
        </Typography>
        <Typography variant="body1" sx={{ mb: 3, color: 'text.secondary' }}>
          Common pattern used across AllMedia and MixlistsPage for switching between card and list layouts.
        </Typography>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="body2" color="text.secondary">
            {mockMediaItems.length} items
          </Typography>
          <ButtonGroup variant="outlined" size="small">
            <Button
              onClick={() => setViewMode('card')}
              variant={viewMode === 'card' ? 'contained' : 'outlined'}
              startIcon={<ViewModule />}
            >
              Card
            </Button>
            <Button
              onClick={() => setViewMode('list')}
              variant={viewMode === 'list' ? 'contained' : 'outlined'}
              startIcon={<ViewList />}
            >
              List
            </Button>
          </ButtonGroup>
        </Box>

        {viewMode === 'card' ? (
          <Grid container spacing={3}>
            {mockMediaItems.slice(0, 4).map((media) => (
              <Grid item xs={12} sm={6} md={3} key={media.id}>
                <MediaCard
                  media={media}
                  variant="default"
                  onClick={handleMediaClick}
                />
              </Grid>
            ))}
          </Grid>
        ) : (
          <List>
            {mockMediaItems.slice(0, 4).map((media) => (
              <ListItem
                key={media.id}
                sx={{
                  mb: 1,
                  borderRadius: 1,
                  backgroundColor: COLORS.background.elevated,
                  cursor: 'pointer',
                  '&:hover': { backgroundColor: COLORS.background.paper }
                }}
                onClick={() => handleMediaClick(media)}
              >
                <ListItemIcon>
                  {getMediaIcon(media.mediaType)}
                </ListItemIcon>
                <ListItemText
                  primary={media.title}
                  secondary={media.notes}
                />
                <Box sx={{ display: 'flex', gap: 1 }}>
                  <Chip
                    label={formatMediaType(media.mediaType)}
                    size="small"
                    sx={{
                      backgroundColor: COLORS.mediaTypes[media.mediaType] || COLORS.mediaTypes.document,
                      color: 'white',
                      fontWeight: 'bold'
                    }}
                  />
                  <Chip
                    label={formatStatus(media.status)}
                    size="small"
                    sx={{
                      backgroundColor: getStatusColor(media.status),
                      color: 'white',
                      fontWeight: 'bold'
                    }}
                  />
                </Box>
              </ListItem>
            ))}
          </List>
        )}
      </Paper>

      {/* Interactive Components Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Interactive Components
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Button Variants
            </Typography>
            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 2 }}>
              <Button variant="contained" color="primary">Primary</Button>
              <Button variant="contained" color="secondary" sx={{ color: '#1B1B1B' }}>Secondary</Button>
              <Button variant="outlined">Outlined</Button>
              <Button variant="text">Text</Button>
            </Box>
            <Typography variant="body2" color="text.secondary">
              Different button styles for various use cases
            </Typography>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Form Controls
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mb: 2 }}>
              <TextField label="Sample Input" variant="outlined" size="small" />
              <FormControl size="small">
                <InputLabel>Sample Select</InputLabel>
                <Select label="Sample Select" value="">
                  <MenuItem value="">Option 1</MenuItem>
                  <MenuItem value="">Option 2</MenuItem>
                </Select>
              </FormControl>
            </Box>
            <Typography variant="body2" color="text.secondary">
              Form components with consistent styling
            </Typography>
          </Grid>
        </Grid>
      </Paper>

      {/* Button Components Section */}
      <Paper elevation={2} sx={{ p: 4, mb: 4, backgroundColor: COLORS.background.default }}>
        <Typography variant="h4" gutterBottom sx={{ color: COLORS.text.primary, mb: 3 }}>
          Button Components
        </Typography>

        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Box sx={{ p: 3, backgroundColor: COLORS.background.paper, borderRadius: BORDER_RADIUS.medium }}>
              <Typography variant="h6" gutterBottom sx={{ color: COLORS.text.primary, mb: 2 }}>
                White Outline Button
              </Typography>
              <Typography variant="body2" sx={{ color: COLORS.text.secondary, mb: 3 }}>
                A reusable button component with white outline styling, perfect for dark backgrounds.
              </Typography>

              <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                <WhiteOutlineButton size="small">
                  Small Button
                </WhiteOutlineButton>
                <WhiteOutlineButton>
                  Medium Button
                </WhiteOutlineButton>
                <WhiteOutlineButton size="large">
                  Large Button
                </WhiteOutlineButton>
              </Box>

              <Box sx={{ mt: 2 }}>
                <WhiteOutlineButton disabled>
                  Disabled Button
                </WhiteOutlineButton>
              </Box>
            </Box>
          </Grid>

          <Grid item xs={12} md={6}>
            <Box sx={{ p: 3, backgroundColor: COLORS.background.paper, borderRadius: BORDER_RADIUS.medium }}>
              <Typography variant="h6" gutterBottom sx={{ color: COLORS.text.primary, mb: 2 }}>
                Usage Example
              </Typography>
              <Box sx={{
                backgroundColor: COLORS.background.default,
                p: 2,
                borderRadius: BORDER_RADIUS.small,
                fontFamily: 'monospace',
                fontSize: '0.875rem',
                color: COLORS.text.secondary,
                overflow: 'auto'
              }}>
                <pre>{`import WhiteOutlineButton from './shared/WhiteOutlineButton';

<WhiteOutlineButton
  onClick={handleClick}
  size="small"
  disabled={isLoading}
>
  Click Me
</WhiteOutlineButton>`}</pre>
              </Box>
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* Spacing and Typography Demo */}
      <Paper sx={{ p: 3, mb: 4 }}>
        <Typography variant="h4" gutterBottom>
          Typography & Spacing
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Heading Hierarchy
            </Typography>
            <Box sx={{ mb: 2 }}>
              <Typography variant="h1">Heading 1</Typography>
              <Typography variant="h2">Heading 2</Typography>
              <Typography variant="h3">Heading 3</Typography>
              <Typography variant="h4">Heading 4</Typography>
              <Typography variant="h5">Heading 5</Typography>
              <Typography variant="h6">Heading 6</Typography>
            </Box>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Text Variants
            </Typography>
            <Box>
              <Typography variant="body1" sx={{ mb: 1 }}>
                Body 1 text (1.2rem) with normal line height and spacing.
              </Typography>
              <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                Body 2 text (1.1rem) with secondary color for less emphasis.
              </Typography>
              <Typography variant="caption" color="text.hint" sx={{ mb: 1 }}>
                Caption text (1.1rem) for small details and metadata.
              </Typography>
              <Typography variant="overline" color="text.secondary">
                Overline text for labels and categories
              </Typography>
            </Box>
          </Grid>
        </Grid>
      </Paper>

      {/* Spacing and Shadows Demo */}
      <Paper sx={{ p: 3 }}>
        <Typography variant="h4" gutterBottom>
          Spacing & Shadows
        </Typography>
        <Grid container spacing={3}>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Spacing Scale
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ p: 1, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                <Typography variant="body2">Padding: 8px (1)</Typography>
              </Box>
              <Box sx={{ p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                <Typography variant="body2">Padding: 16px (2)</Typography>
              </Box>
              <Box sx={{ p: 3, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                <Typography variant="body2">Padding: 24px (3)</Typography>
              </Box>
              <Box sx={{ p: 4, border: '1px solid', borderColor: 'divider', borderRadius: 1 }}>
                <Typography variant="body2">Padding: 32px (4)</Typography>
              </Box>
            </Box>
          </Grid>
          <Grid item xs={12} md={6}>
            <Typography variant="h6" gutterBottom>
              Shadow Variants
            </Typography>
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ p: 2, boxShadow: SHADOWS.sm, borderRadius: 1 }}>
                <Typography variant="body2">Small Shadow</Typography>
              </Box>
              <Box sx={{ p: 2, boxShadow: SHADOWS.md, borderRadius: 1 }}>
                <Typography variant="body2">Medium Shadow</Typography>
              </Box>
              <Box sx={{ p: 2, boxShadow: SHADOWS.lg, borderRadius: 1 }}>
                <Typography variant="body2">Large Shadow</Typography>
              </Box>
              <Box sx={{ p: 2, boxShadow: SHADOWS.xl, borderRadius: 1 }}>
                <Typography variant="body2">Extra Large Shadow</Typography>
              </Box>
            </Box>
          </Grid>
        </Grid>
      </Paper>
    </Container>
  );
};

export default DemoPage;
