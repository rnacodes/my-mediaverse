import React from 'react';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, TextField, InputAdornment, Grid, Button, Divider, Accordion, AccordionSummary, AccordionDetails, FormGroup, FormControlLabel, Checkbox, Paper } from '@mui/material';
import { Search as SearchIcon, Clear, TuneRounded, ExpandMore } from '@mui/icons-material';
import { getRatingIcon } from '@/utils/formatters';


// No "all" entry — like ratings, an empty selection means no status filter.
const statusOptions = [
    { value: 'Uncharted', label: 'Uncharted' },
    { value: 'ActivelyExploring', label: 'Actively Exploring' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Abandoned', label: 'Abandoned' }
];

const ratingOptions = [
    { value: 'SuperLike', label: 'Super Like', icon: 'superlike' },
    { value: 'Like', label: 'Like', icon: 'like' },
    { value: 'Neutral', label: 'Neutral', icon: 'neutral' },
    { value: 'Dislike', label: 'Dislike', icon: 'dislike' }
];

export const SearchFilterSidebar = React.memo(({
    searchMode,
    selectedMediaTypes,
    setSelectedMediaTypes,
    selectedTopics,
    setSelectedTopics,
    selectedGenres,
    setSelectedGenres,
    selectedStatuses,
    setSelectedStatuses,
    selectedRatings,
    setSelectedRatings,
    handleClearFilters,
    topicSearchQuery,
    setTopicSearchQuery,
    genreSearchQuery,
    setGenreSearchQuery,
    showAllTopics,
    setShowAllTopics,
    showAllGenres,
    setShowAllGenres,
    allTopics,
    allGenres,
    mediaTypeOptions
}) => {
    const navigate = useNavigate();

    const handleMediaTypeToggle = (value) => {
        if (value === 'all') {
            setSelectedMediaTypes(['all']);
        } else {
            const newSelection = selectedMediaTypes.includes('all') 
                ? [value]
                : selectedMediaTypes.includes(value)
                    ? selectedMediaTypes.filter(t => t !== value)
                    : [...selectedMediaTypes.filter(t => t !== 'all'), value];
            
            setSelectedMediaTypes(newSelection.length === 0 ? ['all'] : newSelection);
        }
    };

    const handleTopicToggle = (topic) => {
        setSelectedTopics(prev =>
            prev.includes(topic) ? prev.filter(t => t !== topic) : [...prev, topic]
        );
    };

    const handleGenreToggle = (genre) => {
        setSelectedGenres(prev =>
            prev.includes(genre) ? prev.filter(g => g !== genre) : [...prev, genre]
        );
    };

    const handleStatusToggle = (status) => {
        setSelectedStatuses(prev =>
            prev.includes(status) ? prev.filter(s => s !== status) : [...prev, status]
        );
    };

    const handleRatingToggle = (rating) => {
        setSelectedRatings(prev =>
            prev.includes(rating) ? prev.filter(r => r !== rating) : [...prev, rating]
        );
    };

    return (
        <Grid item xs={12} md={3}>
            <Paper 
                elevation={2}
                sx={{ 
                    p: 2.5, 
                    position: 'sticky',
                    top: 16,
                    backgroundColor: 'background.paper',
                    borderRadius: 2
                }}
            >
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Typography variant="h6" sx={{ fontWeight: 'bold', display: 'flex', alignItems: 'center', gap: 1 }}>
                        <TuneRounded /> Filters
                    </Typography>
                    <Button 
                        size="small" 
                        onClick={handleClearFilters}
                        startIcon={<Clear />}
                        sx={{ color: 'white' }}
                    >
                        Clear All
                    </Button>
                </Box>

                <Divider sx={{ mb: 2 }} />

                {/* Media Type Filter - Only show for media search */}
                {searchMode === 'media' && (
                    <>
                        <Accordion defaultExpanded disableGutters elevation={0}>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                                    Media Type
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails sx={{ pt: 0 }}>
                                <FormGroup>
                                    {mediaTypeOptions.map((option) => (
                                        <FormControlLabel
                                            key={option.value}
                                            control={
                                                <Checkbox
                                                    checked={selectedMediaTypes.includes(option.value)}
                                                    onChange={() => handleMediaTypeToggle(option.value)}
                                                    size="small"
                                                    sx={{ color: 'white', '&.Mui-checked': { color: 'white' } }}
                                                />
                                            }
                                            label={<Typography variant="body2">{option.label}</Typography>}
                                            sx={{ mb: 0.5 }}
                                        />
                                    ))}
                                </FormGroup>
                            </AccordionDetails>
                        </Accordion>

                        <Divider sx={{ my: 1 }} />
                    </>
                )}

                {/* Topics Filter */}
                <Accordion disableGutters elevation={0}>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                            Topics {selectedTopics.length > 0 && `(${selectedTopics.length})`}
                        </Typography>
                    </AccordionSummary>
                    <AccordionDetails sx={{ pt: 0 }}>
                        <TextField
                            fullWidth
                            size="small"
                            placeholder="Search topics..."
                            value={topicSearchQuery}
                            onChange={(e) => setTopicSearchQuery(e.target.value)}
                            sx={{ mb: 1.5 }}
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <SearchIcon sx={{ fontSize: 18 }} />
                                    </InputAdornment>
                                )
                            }}
                        />
                        <FormGroup>
                            {allTopics
                                .filter(topic => topic.toLowerCase().includes(topicSearchQuery.toLowerCase()))
                                .slice(0, showAllTopics ? undefined : 10)
                                .map((topic) => (
                                    <FormControlLabel
                                        key={topic}
                                        control={
                                            <Checkbox
                                                checked={selectedTopics.includes(topic)}
                                                onChange={() => handleTopicToggle(topic)}
                                                size="small"
                                            />
                                        }
                                        label={<Typography variant="body2">{topic}</Typography>}
                                        sx={{ mb: 0.5 }}
                                    />
                                ))}
                        </FormGroup>
                        {allTopics.filter(topic => topic.toLowerCase().includes(topicSearchQuery.toLowerCase())).length > 10 && !showAllTopics && (
                            <Button
                                size="small"
                                onClick={() => setShowAllTopics(true)}
                                sx={{ mt: 1, textTransform: 'none', color: '#fcfafa' }}
                            >
                                Show More
                            </Button>
                        )}
                        {showAllTopics && (
                            <Button
                                size="small"
                                onClick={() => setShowAllTopics(false)}
                                sx={{ mt: 1, textTransform: 'none', color: '#fcfafa' }}
                            >
                                Show Less
                            </Button>
                        )}
                        <Divider sx={{ my: 1.5 }} />
                        <Button
                            size="small"
                            fullWidth
                            variant="text"
                            onClick={() => navigate('/search-by-topic-genre')}
                            sx={{ textTransform: 'none', justifyContent: 'flex-start', color: 'white' }}
                        >
                            Browse all topics →
                        </Button>
                    </AccordionDetails>
                </Accordion>

                {/* Genres Filter - notes and highlights have no genres, so only media and mixlists show it */}
                {(searchMode === 'media' || searchMode === 'mixlists') && (
                <>
                <Divider sx={{ my: 1 }} />

                <Accordion disableGutters elevation={0}>
                    <AccordionSummary expandIcon={<ExpandMore />}>
                        <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                            Genres {selectedGenres.length > 0 && `(${selectedGenres.length})`}
                        </Typography>
                    </AccordionSummary>
                    <AccordionDetails sx={{ pt: 0 }}>
                        <TextField
                            fullWidth
                            size="small"
                            placeholder="Search genres..."
                            value={genreSearchQuery}
                            onChange={(e) => setGenreSearchQuery(e.target.value)}
                            sx={{ mb: 1.5 }}
                            InputProps={{
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <SearchIcon sx={{ fontSize: 18 }} />
                                    </InputAdornment>
                                )
                            }}
                        />
                        <FormGroup>
                            {allGenres
                                .filter(genre => genre.toLowerCase().includes(genreSearchQuery.toLowerCase()))
                                .slice(0, showAllGenres ? undefined : 10)
                                .map((genre) => (
                                    <FormControlLabel
                                        key={genre}
                                        control={
                                            <Checkbox
                                                checked={selectedGenres.includes(genre)}
                                                onChange={() => handleGenreToggle(genre)}
                                                size="small"
                                            />
                                        }
                                        label={<Typography variant="body2">{genre}</Typography>}
                                        sx={{ mb: 0.5 }}
                                    />
                                ))}
                        </FormGroup>
                        {allGenres.filter(genre => genre.toLowerCase().includes(genreSearchQuery.toLowerCase())).length > 10 && !showAllGenres && (
                            <Button
                                size="small"
                                onClick={() => setShowAllGenres(true)}
                                sx={{ mt: 1, textTransform: 'none', color: '#fcfafa' }}
                            >
                                Show More
                            </Button>
                        )}
                        {showAllGenres && (
                            <Button
                                size="small"
                                onClick={() => setShowAllGenres(false)}
                                sx={{ mt: 1, textTransform: 'none', color: '#fcfafa' }}
                            >
                                Show Less
                            </Button>
                        )}
                        <Divider sx={{ my: 1.5 }} />
                        <Button
                            size="small"
                            fullWidth
                            variant="text"
                            onClick={() => navigate('/search-by-topic-genre')}
                            sx={{ textTransform: 'none', justifyContent: 'flex-start', color: 'white' }}
                        >
                            Browse all genres →
                        </Button>
                    </AccordionDetails>
                </Accordion>
                </>
                )}

                {/* Status and Rating Filters - Only show for media search */}
                {searchMode === 'media' && (
                    <>
                        <Divider sx={{ my: 1 }} />

                        {/* Status Filter */}
                        <Accordion disableGutters elevation={0}>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                                    Status {selectedStatuses.length > 0 && `(${selectedStatuses.length})`}
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails sx={{ pt: 0 }}>
                                <FormGroup>
                                    {statusOptions.map((option) => (
                                        <FormControlLabel
                                            key={option.value}
                                            control={
                                                <Checkbox
                                                    checked={selectedStatuses.includes(option.value)}
                                                    onChange={() => handleStatusToggle(option.value)}
                                                    size="small"
                                                />
                                            }
                                            label={<Typography variant="body2">{option.label}</Typography>}
                                            sx={{ mb: 0.5 }}
                                        />
                                    ))}
                                </FormGroup>
                            </AccordionDetails>
                        </Accordion>

                        <Divider sx={{ my: 1 }} />

                        {/* Rating Filter */}
                        <Accordion disableGutters elevation={0}>
                            <AccordionSummary expandIcon={<ExpandMore />}>
                                <Typography variant="subtitle2" sx={{ fontWeight: 'bold' }}>
                                    Rating {selectedRatings.length > 0 && `(${selectedRatings.length})`}
                                </Typography>
                            </AccordionSummary>
                            <AccordionDetails sx={{ pt: 0 }}>
                                <FormGroup>
                                    {ratingOptions.map((rating) => (
                                        <FormControlLabel
                                            key={rating.value}
                                            control={
                                                <Checkbox
                                                    checked={selectedRatings.includes(rating.value)}
                                                    onChange={() => handleRatingToggle(rating.value)}
                                                    size="small"
                                                />
                                            }
                                            label={
                                                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                                                    {getRatingIcon(rating.icon)}
                                                    <Typography variant="body2">{rating.label}</Typography>
                                                </Box>
                                            }
                                            sx={{ mb: 0.5 }}
                                        />
                                    ))}
                                </FormGroup>
                            </AccordionDetails>
                        </Accordion>
                    </>
                )}
            </Paper>
        </Grid>
    );
});

SearchFilterSidebar.displayName = 'SearchFilterSidebar';
