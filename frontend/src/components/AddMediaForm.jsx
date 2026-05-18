import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography, Container,
    Select, MenuItem, InputLabel, FormControl,
    Checkbox, FormControlLabel, Radio, RadioGroup,
    FormLabel, Chip, OutlinedInput, Paper, Grid,
    Autocomplete
} from '@mui/material';
import { useAddMedia } from '../hooks/useMedia';
import { useAllMixlists, useAddMediaToMixlist } from '../hooks/useMixlist';
import { useCreatePodcastEpisode, usePodcastSeriesSearch } from '../hooks/usePodcast';
import { useTopicSearch, useGenreSearch } from '../hooks/useTopicGenre';
import { useCreateBook } from '../hooks/useBook';
import { useCreateMovie } from '../hooks/useMovie';
import { useCreateTvShow } from '../hooks/useTvShow';
import { useCreateVideo } from '../hooks/useVideo';
import { useUploadThumbnail } from '../hooks/useUpload';

function AddMediaForm() {
    const [title, setTitle] = useState('');
    const [mediaType, setMediaType] = useState('');
    const [link, setLink] = useState('');
    const [notes, setNotes] = useState('');
    const [status, setStatus] = useState('Uncharted'); // Changed from consumed to status
    const [dateCompleted, setDateCompleted] = useState(''); // Changed from dateConsumed
    const [rating, setRating] = useState('');
    const [ownershipStatus, setOwnershipStatus] = useState('');
    const [description, setDescription] = useState('');
    const [relatedNotes, setRelatedNotes] = useState('');
    const [thumbnail, setThumbnail] = useState('');
    const [thumbnailFile, setThumbnailFile] = useState(null);
    const [genres, setGenres] = useState([]);
    const [genreInput, setGenreInput] = useState('');
    const [topics, setTopics] = useState([]);
    const [topicInput, setTopicInput] = useState('');
    
    // Podcast specific fields
    const [podcastType, setPodcastType] = useState(''); // 'Series' or 'Episode'
    const [podcastSeriesId, setPodcastSeriesId] = useState('');
    const [audioLink, setAudioLink] = useState('');
    const [releaseDate, setReleaseDate] = useState('');
    const [durationInSeconds, setDurationInSeconds] = useState('');
    
    // Book specific fields
    const [author, setAuthor] = useState('');
    const [isbn, setIsbn] = useState('');
    const [asin, setAsin] = useState('');
    const [format, setFormat] = useState('Digital'); // 'Digital' or 'Physical'
    const [partOfSeries, setPartOfSeries] = useState(false);
    const [goodreadsRating, setGoodreadsRating] = useState('');
    
    // Movie specific fields
    const [director, setDirector] = useState('');
    const [cast, setCast] = useState('');
    const [releaseYear, setReleaseYear] = useState('');
    const [runtimeMinutes, setRuntimeMinutes] = useState('');
    const [mpaaRating, setMpaaRating] = useState('');
    const [imdbId, setImdbId] = useState('');
    const [tmdbId, setTmdbId] = useState('');
    const [tmdbRating, setTmdbRating] = useState('');
    const [tagline, setTagline] = useState('');
    const [homepage, setHomepage] = useState('');
    const [originalLanguage, setOriginalLanguage] = useState('');
    const [originalTitle, setOriginalTitle] = useState('');
    
    // TV Show specific fields
    const [creator, setCreator] = useState('');
    const [firstAirYear, setFirstAirYear] = useState('');
    const [lastAirYear, setLastAirYear] = useState('');
    const [numberOfSeasons, setNumberOfSeasons] = useState('');
    const [numberOfEpisodes, setNumberOfEpisodes] = useState('');
    const [contentRating, setContentRating] = useState('');
    const [originalName, setOriginalName] = useState('');
    
    // Video specific fields
    const [videoType, setVideoType] = useState('Series'); // 'Series' or 'Episode'
    const [parentVideoId, setParentVideoId] = useState('');
    const [platform, setPlatform] = useState('YouTube');
    const [channelName, setChannelName] = useState('');
    const [lengthInSeconds, setLengthInSeconds] = useState('');
    const [externalId, setExternalId] = useState('');
    
    // Mixlist selection
    const [selectedMixlists, setSelectedMixlists] = useState([]);
    const [mixlistInput, setMixlistInput] = useState('');

    // Autocomplete inputs (drive react-query hooks) — topicInput/genreInput are declared above
    const [podcastSeriesInput, setPodcastSeriesInput] = useState('');
    const [selectedPodcastSeries, setSelectedPodcastSeries] = useState(null);

    // Validation errors
    const [validationErrors, setValidationErrors] = useState({});

    const navigate = useNavigate();

    // Queries + mutations
    const mixlistsQuery = useAllMixlists();
    const availableMixlists = mixlistsQuery.data ?? [];

    const topicSearchQuery = useTopicSearch(topicInput);
    const genreSearchQuery = useGenreSearch(genreInput);
    const podcastSeriesSearchQuery = usePodcastSeriesSearch(podcastSeriesInput);
    const topicSuggestions = topicSearchQuery.data ?? [];
    const genreSuggestions = genreSearchQuery.data ?? [];
    const podcastSeriesSuggestions = podcastSeriesSearchQuery.data ?? [];

    const addMediaMutation = useAddMedia();
    const createBookMutation = useCreateBook();
    const createMovieMutation = useCreateMovie();
    const createTvShowMutation = useCreateTvShow();
    const createVideoMutation = useCreateVideo();
    const createPodcastEpisodeMutation = useCreatePodcastEpisode();
    const addMediaToMixlistMutation = useAddMediaToMixlist();
    const uploadThumbnailMutation = useUploadThumbnail();

    // Handle mixlist selection
    const handleMixlistKeyPress = (event) => {
        if (event.key === 'Enter' && mixlistInput.trim()) {
            event.preventDefault();
            const mixlist = availableMixlists.find(p => {
                const name = p.Name || p.name || '';
                return name.toLowerCase().includes(mixlistInput.toLowerCase());
            });
            if (mixlist && !selectedMixlists.some(p => (p.Id || p.id) === (mixlist.Id || mixlist.id))) {
                // Normalize the mixlist object
                const normalizedMixlist = {
                    ...mixlist,
                    Id: mixlist.Id || mixlist.id,
                    Name: mixlist.Name || mixlist.name || `Mixlist ${mixlist.Id || mixlist.id}`
                };
                setSelectedMixlists([...selectedMixlists, normalizedMixlist]);
                setMixlistInput('');
            }
        }
    };

    const removeMixlist = (mixlistToRemove) => {
        setSelectedMixlists(selectedMixlists.filter(mixlist => mixlist.Id !== mixlistToRemove.Id));
    };

    // Handle genre input
    const handleGenreKeyPress = (event) => {
        if (event.key === 'Enter' && genreInput.trim()) {
            event.preventDefault();
            const normalizedGenre = genreInput.trim().toLowerCase();
            if (!genres.includes(normalizedGenre)) {
                setGenres([...genres, normalizedGenre]);
            }
            setGenreInput('');
        }
    };

    const removeGenre = (genreToRemove) => {
        setGenres(genres.filter(genre => genre !== genreToRemove));
    };

    // Handle topic input
    const handleTopicKeyPress = (event) => {
        if (event.key === 'Enter' && topicInput.trim()) {
            event.preventDefault();
            const normalizedTopic = topicInput.trim().toLowerCase();
            if (!topics.includes(normalizedTopic)) {
                setTopics([...topics, normalizedTopic]);
            }
            setTopicInput('');
        }
    };

    const removeTopic = (topicToRemove) => {
        setTopics(topics.filter(topic => topic !== topicToRemove));
    };

    // Handle thumbnail file upload
    const handleThumbnailUpload = (event) => {
        const file = event.target.files[0];
        if (!file) return;
        setThumbnailFile(file);
        uploadThumbnailMutation.mutate(file, {
            onSuccess: (data) => setThumbnail(data.url),
            onError: (error) => {
                console.error('Error uploading thumbnail:', error);
                alert('Failed to upload thumbnail. Please try again.');
                setThumbnailFile(null);
            },
        });
    };

    const handleSubmit = async (event) => {
        event.preventDefault();
        
        // Base media data - Using camelCase to match backend DTO
        let mediaData = { 
            title: title, 
            mediaType: mediaType, 
            status: status, // Required field
            topics: topics.length > 0 ? topics : [], // Required array
            genres: genres.length > 0 ? genres : [] // Required array
        };
        
        // Add optional fields only if they have values
        if (link && link.trim()) mediaData.link = link;
        if (notes && notes.trim()) mediaData.notes = notes;
        if (status === 'Completed' && dateCompleted) mediaData.dateCompleted = dateCompleted;
        if (status === 'Completed' && rating) mediaData.rating = rating;
        if (ownershipStatus) mediaData.ownershipStatus = ownershipStatus;
        if (description && description.trim()) mediaData.description = description;
        if (relatedNotes && relatedNotes.trim()) mediaData.relatedNotes = relatedNotes;
        if (thumbnail && thumbnail.trim()) mediaData.thumbnail = thumbnail;
        else mediaData.thumbnail = null;

        try {
            // Clear previous validation errors
            setValidationErrors({});
            
            // Basic validation
            const errors = {};
            if (!title.trim()) {
                errors.title = 'Title is required';
            }
            if (!mediaType) {
                errors.mediaType = 'Media Type is required';
            }
            
            if (Object.keys(errors).length > 0) {
                setValidationErrors(errors);
                return;
            }
            
            console.log('Submitting media data:', mediaData);
            console.log('Raw form values:', { title, mediaType, status, ownershipStatus, rating });
            
            // Check if media type is supported by backend
            if (mediaType !== 'Podcast' && mediaType !== 'Book' && mediaType !== 'Movie' && mediaType !== 'TVShow' && mediaType !== 'Video') {
                alert('Currently only Podcast, Book, Movie, TVShow, and Video media types are supported by the backend. Other media types are not yet implemented.');
                return;
            }
            
            let data;

            // Handle book-specific creation
            if (mediaType === 'Book') {
                const bookData = {
                    title: title,
                    mediaType: "Book",
                    link: link,
                    notes: notes,
                    description: description,
                    status: status,
                    dateCompleted: status === 'Completed' && dateCompleted ? dateCompleted : null,
                    rating: status === 'Completed' && rating ? rating : null,
                    ownershipStatus: ownershipStatus || null,
                    topics: topics.length > 0 ? topics : [],
                    genres: genres.length > 0 ? genres : [],
                    relatedNotes: relatedNotes,
                    thumbnail: thumbnail || null,
                    author: author,
                    isbn: isbn || null,
                    asin: asin || null,
                    format: format,
                    partOfSeries: partOfSeries,
                    goodreadsRating: goodreadsRating ? parseFloat(goodreadsRating) : null
                };
                
                data = await createBookMutation.mutateAsync(bookData);
            }
            // Handle podcast-specific creation
            else if (mediaType === 'Podcast') {
                if (podcastType === 'Series') {
                    // For now, create as regular media until PodcastSeriesController exists
                    data = await addMediaMutation.mutateAsync(mediaData);
                } else if (podcastType === 'Episode') {
                    // Create podcast episode with additional fields - camelCase for backend
                    const episodeData = {
                        title: title,
                        mediaType: "Podcast",
                        link: link,
                        notes: notes,
                        description: description,
                        status: status,
                        dateCompleted: status === 'Completed' && dateCompleted ? dateCompleted : null,
                        rating: status === 'Completed' && rating ? rating : null,
                        ownershipStatus: ownershipStatus || null,
                        topics: topics.length > 0 ? topics : [], // Ensure proper array format
                        genres: genres.length > 0 ? genres : [], // Ensure proper array format
                        relatedNotes: relatedNotes,
                        thumbnail: thumbnail || null,
                        parentPodcastId: selectedPodcastSeries?.id || selectedPodcastSeries?.Id || podcastSeriesId,
                        audioLink: audioLink || null,
                        releaseDate: releaseDate || null,
                        durationInSeconds: durationInSeconds ? parseInt(durationInSeconds) : 0
                    };
                    
                    data = await createPodcastEpisodeMutation.mutateAsync(episodeData);
                } else {
                    // No podcast type selected, create as regular media
                    data = await addMediaMutation.mutateAsync(mediaData);
                }
            }
            // Handle movie-specific creation
            else if (mediaType === 'Movie') {
                const movieData = {
                    title: title,
                    mediaType: "Movie",
                    link: link,
                    notes: notes,
                    description: description,
                    status: status,
                    dateCompleted: status === 'Completed' && dateCompleted ? dateCompleted : null,
                    rating: status === 'Completed' && rating ? rating : null,
                    ownershipStatus: ownershipStatus || null,
                    topics: topics.length > 0 ? topics : [],
                    genres: genres.length > 0 ? genres : [],
                    relatedNotes: relatedNotes,
                    thumbnail: thumbnail || null,
                    director: director || null,
                    cast: cast || null,
                    releaseYear: releaseYear ? parseInt(releaseYear) : null,
                    runtimeMinutes: runtimeMinutes ? parseInt(runtimeMinutes) : null,
                    mpaaRating: mpaaRating || null,
                    imdbId: imdbId || null,
                    tmdbId: tmdbId || null,
                    tmdbRating: tmdbRating ? parseFloat(tmdbRating) : null,
                    tagline: tagline || null,
                    homepage: homepage || null,
                    originalLanguage: originalLanguage || null,
                    originalTitle: originalTitle || null
                };
                
                data = await createMovieMutation.mutateAsync(movieData);
            }
            // Handle TV show-specific creation
            else if (mediaType === 'TVShow') {
                const tvShowData = {
                    title: title,
                    mediaType: "TVShow",
                    link: link,
                    notes: notes,
                    description: description,
                    status: status,
                    dateCompleted: status === 'Completed' && dateCompleted ? dateCompleted : null,
                    rating: status === 'Completed' && rating ? rating : null,
                    ownershipStatus: ownershipStatus || null,
                    topics: topics.length > 0 ? topics : [],
                    genres: genres.length > 0 ? genres : [],
                    relatedNotes: relatedNotes,
                    thumbnail: thumbnail || null,
                    creator: creator || null,
                    cast: cast || null,
                    firstAirYear: firstAirYear ? parseInt(firstAirYear) : null,
                    lastAirYear: lastAirYear ? parseInt(lastAirYear) : null,
                    numberOfSeasons: numberOfSeasons ? parseInt(numberOfSeasons) : null,
                    numberOfEpisodes: numberOfEpisodes ? parseInt(numberOfEpisodes) : null,
                    contentRating: contentRating || null,
                    tmdbId: tmdbId || null,
                    tmdbRating: tmdbRating ? parseFloat(tmdbRating) : null,
                    tagline: tagline || null,
                    homepage: homepage || null,
                    originalLanguage: originalLanguage || null,
                    originalName: originalName || null
                };
                
                data = await createTvShowMutation.mutateAsync(tvShowData);
            }
            // Handle video-specific creation
            else if (mediaType === 'Video') {
                const videoData = {
                    title: title,
                    mediaType: "Video",
                    link: link,
                    notes: notes,
                    description: description,
                    status: status,
                    dateCompleted: status === 'Completed' && dateCompleted ? dateCompleted : null,
                    rating: status === 'Completed' && rating ? rating : null,
                    ownershipStatus: ownershipStatus || null,
                    topics: topics.length > 0 ? topics : [],
                    genres: genres.length > 0 ? genres : [],
                    relatedNotes: relatedNotes,
                    thumbnail: thumbnail || null,
                    videoType: videoType,
                    parentVideoId: parentVideoId || null,
                    platform: platform || 'YouTube',
                    channelName: channelName || null,
                    lengthInSeconds: lengthInSeconds ? parseInt(lengthInSeconds) : 0,
                    externalId: externalId || null
                };
                
                data = await createVideoMutation.mutateAsync(videoData);
            } else {
                // Create regular media item
                data = await addMediaMutation.mutateAsync(mediaData);
            }

            // Add media to selected mixlists
            const mediaId = data.id || data.Id; // Handle both lowercase and uppercase Id
            if (selectedMixlists.length > 0 && mediaId) {
                for (const mixlist of selectedMixlists) {
                    try {
                        await addMediaToMixlistMutation.mutateAsync({
                            mixlistId: mixlist.Id || mixlist.id,
                            mediaItemId: mediaId,
                        });
                    } catch (mixlistError) {
                        console.error(`Failed to add media to mixlist ${mixlist.Name || mixlist.name}:`, mixlistError);
                    }
                }
            }
            
            console.log('Media added!', data);
            navigate(`/media/${mediaId}`);
        } catch (error) {
            console.error('Failed to add media:', error);
            console.error('Error details:', error.response?.data);
            console.error('Error status:', error.response?.status);
            console.error('Full error response:', error.response);
            
            // More detailed error message
            let errorMessage = 'Unknown error';
            if (error.response?.data) {
                if (typeof error.response.data === 'string') {
                    errorMessage = error.response.data;
                } else if (error.response.data.message) {
                    errorMessage = error.response.data.message;
                } else if (error.response.data.errors) {
                    // Handle validation errors
                    const validationErrors = Object.entries(error.response.data.errors)
                        .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
                        .join('\n');
                    errorMessage = `Validation errors:\n${validationErrors}`;
                } else {
                    errorMessage = JSON.stringify(error.response.data);
                }
            } else if (error.message) {
                errorMessage = error.message;
            }
            
            // Show error to user
            alert(`Failed to add media (Status ${error.response?.status}):\n${errorMessage}`);
        }
    };

    // Convert duration from minutes to seconds
    const handleDurationChange = (e) => {
        const minutes = e.target.value;
        if (minutes) {
            setDurationInSeconds((parseFloat(minutes) * 60).toString());
        } else {
            setDurationInSeconds('');
        }
    };

    const renderMediaTypeSpecificFields = () => {
        if (mediaType === 'Book') {
            return (
                <Box sx={{ mt: 3, mb: 2 }}>
                    <Typography variant="h6" sx={{ mb: 2, fontSize: '18px', fontWeight: 'bold', color: '#ffffff' }}>
                        Book Details
                    </Typography>
                    
                    {/* Author - Required */}
                    <TextField
                        label="Author"
                        placeholder="Enter author name..."
                        variant="outlined"
                        fullWidth
                        required
                        margin="normal"
                        value={author}
                        onChange={(e) => setAuthor(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />

                    {/* ISBN */}
                    <TextField
                        label="ISBN"
                        placeholder="978-0123456789"
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={isbn}
                        onChange={(e) => setIsbn(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />

                    {/* ASIN */}
                    <TextField
                        label="ASIN"
                        placeholder="B0010SKUYM"
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={asin}
                        onChange={(e) => setAsin(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />

                    {/* Goodreads Rating */}
                    <TextField
                        label="Goodreads Rating (1-5)"
                        placeholder="e.g., 4.5"
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        type="number"
                        inputProps={{ min: 1, max: 5, step: 0.1 }}
                        value={goodreadsRating}
                        onChange={(e) => setGoodreadsRating(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                        helperText="Will auto-convert to MMV rating if not set manually"
                    />

                    {/* Format */}
                    <FormControl fullWidth margin="normal" sx={{
                        mb: 2,
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}>
                        <InputLabel id="format-label">Format</InputLabel>
                        <Select
                            labelId="format-label"
                            value={format}
                            label="Format"
                            onChange={(e) => setFormat(e.target.value)}
                        >
                            <MenuItem value="Digital">Digital</MenuItem>
                            <MenuItem value="Physical">Physical</MenuItem>
                        </Select>
                    </FormControl>

                    {/* Part of Series */}
                    <FormControlLabel
                        control={
                            <Checkbox
                                checked={partOfSeries}
                                onChange={(e) => setPartOfSeries(e.target.checked)}
                            />
                        }
                        label="Part of Series"
                        sx={{ 
                            mt: 1,
                            '& .MuiFormControlLabel-label': { 
                                fontSize: '14px',
                                color: '#ffffff'
                            }
                        }}
                    />
                </Box>
            );
        }
        else if (mediaType === 'Podcast') {
            return (
                <Box sx={{ mt: 3, mb: 2 }}>
                    <Typography variant="h6" sx={{ mb: 2, fontSize: '18px', fontWeight: 'bold' }}>
                        Podcast Type
                    </Typography>
                    
                    <FormControl component="fieldset" fullWidth margin="normal">
                        <FormLabel component="legend" sx={{ 
                            color: '#ffffff',
                            fontSize: '14px',
                            '&.Mui-focused': { color: '#ffffff' }
                        }}>
                            Choose podcast type:
                        </FormLabel>
                        <RadioGroup
                            value={podcastType}
                            onChange={(e) => setPodcastType(e.target.value)}
                            row
                            sx={{ mt: 1 }}
                        >
                            <FormControlLabel 
                                value="Series" 
                                control={<Radio />} 
                                label="Series"
                                sx={{ 
                                    '& .MuiFormControlLabel-label': { fontSize: '14px' }
                                }}
                            />
                            <FormControlLabel 
                                value="Episode" 
                                control={<Radio />} 
                                label="Episode"
                                sx={{ 
                                    '& .MuiFormControlLabel-label': { fontSize: '14px' }
                                }}
                            />
                        </RadioGroup>
                    </FormControl>

                    {podcastType === 'Episode' && (
                        <>
                            <Autocomplete
                                options={podcastSeriesSuggestions}
                                getOptionLabel={(option) => option.title || option.Title || ''}
                                value={selectedPodcastSeries}
                                onChange={(event, newValue) => {
                                    setSelectedPodcastSeries(newValue);
                                    setPodcastSeriesId(newValue?.id || newValue?.Id || '');
                                }}
                                onInputChange={(event, newInputValue) => setPodcastSeriesInput(newInputValue)}
                                renderInput={(params) => (
                                    <TextField
                                        {...params}
                                        label="Podcast Series"
                                        placeholder="Search for podcast series..."
                                        variant="outlined"
                                        fullWidth
                                        margin="normal"
                                        sx={{
                                            '& .MuiInputBase-input': {
                                                fontSize: '14px'
                                            },
                                            '& .MuiInputBase-input::placeholder': {
                                                color: '#ffffff',
                                                opacity: 1
                                            },
                                            '& .MuiInputLabel-root': {
                                                color: '#ffffff',
                                                fontSize: '14px'
                                            },
                                            '& .MuiInputLabel-root.Mui-focused': {
                                                color: '#ffffff'
                                            }
                                        }}
                                    />
                                )}
                            />
                            <TextField
                                label="Duration (Minutes)"
                                placeholder="60"
                                type="number"
                                variant="outlined"
                                fullWidth
                                margin="normal"
                                value={durationInSeconds ? (parseInt(durationInSeconds) / 60).toString() : ''}
                                onChange={handleDurationChange}
                                sx={{
                                    '& .MuiInputBase-input': {
                                        fontSize: '14px'
                                    },
                                    '& .MuiInputBase-input::placeholder': {
                                        color: '#ffffff',
                                        opacity: 1
                                    },
                                    '& .MuiInputLabel-root': {
                                        color: '#ffffff',
                                        fontSize: '14px'
                                    },
                                    '& .MuiInputLabel-root.Mui-focused': {
                                        color: '#ffffff'
                                    }
                                }}
                            />
                        </>
                    )}
                </Box>
            );
        }
        else if (mediaType === 'Movie') {
            return (
                <Box sx={{ mt: 3, mb: 2 }}>
                    <Typography variant="h6" sx={{ mb: 2, fontSize: '18px', fontWeight: 'bold', color: '#ffffff' }}>
                        Movie Details
                    </Typography>
                    
                    {/* Director */}
                    <TextField
                        label="Director"
                        placeholder="Enter director name..."
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={director}
                        onChange={(e) => setDirector(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />
                </Box>
            );
        }
        else if (mediaType === 'TVShow') {
            return (
                <Box sx={{ mt: 3, mb: 2 }}>
                    <Typography variant="h6" sx={{ mb: 2, fontSize: '18px', fontWeight: 'bold', color: '#ffffff' }}>
                        TV Show Details
                    </Typography>
                    
                    {/* Creator */}
                    <TextField
                        label="Creator"
                        placeholder="Enter creator name..."
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={creator}
                        onChange={(e) => setCreator(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />
                </Box>
            );
        }
        else if (mediaType === 'Video') {
            return (
                <Box sx={{ mt: 3, mb: 2 }}>
                    <Typography variant="h6" sx={{ mb: 2, fontSize: '18px', fontWeight: 'bold', color: '#ffffff' }}>
                        Video Details
                    </Typography>
                    
                    {/* Video Type */}
                    <FormControl component="fieldset" fullWidth margin="normal">
                        <FormLabel component="legend" sx={{ 
                            color: '#ffffff',
                            fontSize: '14px',
                            '&.Mui-focused': { color: '#ffffff' }
                        }}>
                            Video Type:
                        </FormLabel>
                        <RadioGroup
                            value={videoType}
                            onChange={(e) => setVideoType(e.target.value)}
                            sx={{ 
                                '& .MuiFormControlLabel-label': { 
                                    fontSize: '14px',
                                    color: '#ffffff'
                                }
                            }}
                        >
                            <FormControlLabel value="Series" control={<Radio />} label="Series" />
                            <FormControlLabel value="Episode" control={<Radio />} label="Episode" />
                            <FormControlLabel value="Channel" control={<Radio />} label="Channel" />
                        </RadioGroup>
                    </FormControl>

                    {/* Platform - Required */}
                    <FormControl fullWidth margin="normal" sx={{
                        mb: 2,
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}>
                        <InputLabel id="platform-label">Platform *</InputLabel>
                        <Select
                            labelId="platform-label"
                            value={platform}
                            label="Platform *"
                            onChange={(e) => setPlatform(e.target.value)}
                            required
                        >
                            <MenuItem value="YouTube">YouTube</MenuItem>
                            <MenuItem value="Vimeo">Vimeo</MenuItem>
                            <MenuItem value="Twitch">Twitch</MenuItem>
                            <MenuItem value="Instagram">Instagram</MenuItem>
                            <MenuItem value="Facebook">Facebook</MenuItem>
                            <MenuItem value="Other">Other</MenuItem>
                        </Select>
                    </FormControl>

                    {/* Channel Name - Optional */}
                    <TextField
                        label="Channel Name (Optional)"
                        placeholder="Enter channel/creator name..."
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={channelName}
                        onChange={(e) => setChannelName(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />

                    {/* Length in Seconds */}
                    <TextField
                        label="Length (seconds)"
                        placeholder="Enter video length in seconds..."
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        type="number"
                        value={lengthInSeconds}
                        onChange={(e) => setLengthInSeconds(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />

                    {/* External ID */}
                    <TextField
                        label="External ID"
                        placeholder="Enter external platform ID (optional)..."
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={externalId}
                        onChange={(e) => setExternalId(e.target.value)}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />
                </Box>
            );
        }
        return null;
    };

    return (
        <Box sx={{ 
            minHeight: '100vh', 
            display: 'flex', 
            justifyContent: 'center', 
            alignItems: 'flex-start',
            py: 4,
            px: 2,
            // Global font size override for this form
            '& .MuiInputBase-input': {
                fontSize: '16px !important'
            },
            '& .MuiInputLabel-root': {
                fontSize: '16px !important'
            },
            '& .MuiSelect-select': {
                fontSize: '16px !important'
            },
            '& .MuiFormControlLabel-label': {
                fontSize: '16px !important'
            }
        }}>
            <Box 
                component="form" 
                onSubmit={handleSubmit} 
                sx={{ 
                    width: '100%',
                    maxWidth: '600px',
                    backgroundColor: 'background.paper',
                    borderRadius: '16px',
                    p: 4,
                    boxShadow: '0 4px 12px rgba(0,0,0,0.3)'
                }}
            >
                <Typography variant="h4" component="h1" gutterBottom sx={{ 
                    textAlign: 'center', 
                    fontSize: '28px',
                    fontWeight: 'bold',
                    mb: 3
                }}>
                    Add New Media
                </Typography>
                
                {/* Title - Prominent heading */}
                <Typography variant="h5" sx={{ 
                    fontSize: '20px', 
                    fontWeight: 'bold', 
                    mb: 1,
                    color: '#ffffff'
                }}>
                    Title
                </Typography>
                <TextField
                    placeholder="Enter media title..."
                    variant="outlined"
                    fullWidth
                    required
                    margin="normal"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    sx={{
                        mb: 3,
                        '& .MuiInputBase-input': {
                            fontSize: '16px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        }
                    }}
                />
                {validationErrors.title && (
                    <Typography color="error" variant="body2" sx={{ mt: 1, mb: 2 }} data-testid="title-error">
                        {validationErrors.title}
                    </Typography>
                )}

                {/* Media Type */}
                <FormControl fullWidth margin="normal" required sx={{
                    mb: 3,
                    '& .MuiInputLabel-root': {
                        color: '#ffffff',
                        fontSize: '16px'
                    },
                    '& .MuiInputLabel-root.Mui-focused': {
                        color: '#ffffff'
                    }
                }}>
                    <InputLabel id="media-type-label" data-testid="media-type-label">Media Type</InputLabel>
                    <Select
                        labelId="media-type-label"
                        value={mediaType}
                        label="Media Type"
                        data-testid="media-type-select"
                        onChange={(e) => setMediaType(e.target.value)}
                        sx={{
                            '& .MuiSelect-select': {
                                fontSize: '16px'
                            }
                        }}
                    >
                        <MenuItem value="Article">Article (Coming Soon)</MenuItem>
                        <MenuItem value="Book">Book</MenuItem>
                        <MenuItem value="Document">Document (Coming Soon)</MenuItem>
                        <MenuItem value="Movie">Movie</MenuItem>
                        <MenuItem value="Music">Music (Coming Soon)</MenuItem>
                        <MenuItem value="Other">Other (Coming Soon)</MenuItem>
                        <MenuItem value="Podcast">Podcast</MenuItem>
                        <MenuItem value="TVShow">TV Show</MenuItem>
                        <MenuItem value="Video">Video</MenuItem>
                        <MenuItem value="VideoGame">Video Game (Coming Soon)</MenuItem>
                        <MenuItem value="Website">Website (Coming Soon)</MenuItem>
                    </Select>
                </FormControl>
                {validationErrors.mediaType && (
                    <Typography color="error" variant="body2" sx={{ mt: 1, mb: 2 }} data-testid="media-type-error">
                        {validationErrors.mediaType}
                    </Typography>
                )}

                {/* Link */}
                <TextField
                    label="Link"
                    placeholder="https://example.com"
                    variant="outlined"
                    fullWidth
                    margin="normal"
                    value={link}
                    onChange={(e) => setLink(e.target.value)}
                    sx={{
                        mb: 3,
                        '& .MuiInputBase-input': {
                            fontSize: '14px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        },
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}
                />

                {/* Description */}
                <TextField
                    label="Description"
                    placeholder="Brief description of the media..."
                    variant="outlined"
                    fullWidth
                    multiline
                    rows={3}
                    margin="normal"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    sx={{
                        mb: 3,
                        '& .MuiInputBase-input': {
                            fontSize: '14px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        },
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}
                />

                {/* Status Selection */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="h6" sx={{ 
                        fontSize: '18px', 
                        fontWeight: 'bold', 
                        mb: 2,
                        color: '#ffffff'
                    }}>
                        Status
                    </Typography>
                    <FormControl component="fieldset" fullWidth>
                        <RadioGroup
                            value={status}
                            onChange={(e) => setStatus(e.target.value)}
                            row
                            sx={{ gap: 2 }}
                        >
                            <FormControlLabel 
                                value="Uncharted" 
                                control={<Radio size="small" />} 
                                label="Uncharted"
                                sx={{ '& .MuiFormControlLabel-label': { fontSize: '14px' } }}
                            />
                            <FormControlLabel 
                                value="ActivelyExploring" 
                                control={<Radio size="small" />} 
                                label="Actively Exploring"
                                sx={{ '& .MuiFormControlLabel-label': { fontSize: '14px' } }}
                            />
                            <FormControlLabel 
                                value="Completed" 
                                control={<Radio size="small" />} 
                                label="Completed"
                                sx={{ '& .MuiFormControlLabel-label': { fontSize: '14px' } }}
                            />
                            <FormControlLabel 
                                value="Abandoned" 
                                control={<Radio size="small" />} 
                                label="Abandoned"
                                sx={{ '& .MuiFormControlLabel-label': { fontSize: '14px' } }}
                            />
                        </RadioGroup>
                    </FormControl>
                </Box>

                {/* Conditional Date Completed */}
                {status === 'Completed' && (
                    <TextField
                        label="Date Completed"
                        type="date"
                        variant="outlined"
                        fullWidth
                        margin="normal"
                        value={dateCompleted}
                        onChange={(e) => setDateCompleted(e.target.value)}
                        InputLabelProps={{
                            shrink: true,
                        }}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root': {
                                color: '#ffffff',
                                fontSize: '14px'
                            },
                            '& .MuiInputLabel-root.Mui-focused': {
                                color: '#ffffff'
                            }
                        }}
                    />
                )}

                {/* Conditional Rating */}
                {status === 'Completed' && (
                    <FormControl fullWidth margin="normal" sx={{
                        mb: 3,
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}>
                        <InputLabel id="rating-label">Rating</InputLabel>
                        <Select
                            labelId="rating-label"
                            value={rating}
                            label="Rating"
                            onChange={(e) => setRating(e.target.value)}
                            sx={{
                                '& .MuiSelect-select': {
                                    fontSize: '14px'
                                }
                            }}
                        >
                            <MenuItem value="">None</MenuItem>
                            <MenuItem value="SuperLike">Super Like</MenuItem>
                            <MenuItem value="Like">Like</MenuItem>
                            <MenuItem value="Neutral">Neutral</MenuItem>
                            <MenuItem value="Dislike">Dislike</MenuItem>
                        </Select>
                    </FormControl>
                )}

                {/* Ownership Status */}
                <FormControl fullWidth margin="normal" sx={{
                    mb: 3,
                    '& .MuiInputLabel-root': {
                        color: '#ffffff',
                        fontSize: '14px'
                    },
                    '& .MuiInputLabel-root.Mui-focused': {
                        color: '#ffffff'
                    }
                }}>
                    <InputLabel id="ownership-label">Ownership Status</InputLabel>
                    <Select
                        labelId="ownership-label"
                        value={ownershipStatus}
                        label="Ownership Status"
                        onChange={(e) => setOwnershipStatus(e.target.value)}
                        sx={{
                            '& .MuiSelect-select': {
                                fontSize: '14px'
                            }
                        }}
                    >
                        <MenuItem value="">None</MenuItem>
                        <MenuItem value="Own">Own</MenuItem>
                        <MenuItem value="Rented">Rented</MenuItem>
                        <MenuItem value="Streamed">Streamed</MenuItem>
                    </Select>
                </FormControl>

                {/* Thumbnail URL */}
                <TextField
                    label="Thumbnail URL"
                    placeholder="https://example.com/thumbnail.jpg"
                    variant="outlined"
                    fullWidth
                    margin="normal"
                    value={thumbnail}
                    onChange={(e) => setThumbnail(e.target.value)}
                    sx={{
                        mb: 2,
                        '& .MuiInputBase-input': {
                            fontSize: '14px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        },
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}
                />

                {/* Thumbnail Upload */}
                <Box sx={{ mb: 3 }}>
                    <Typography variant="body1" sx={{ 
                        mb: 2, 
                        fontSize: '16px',
                        fontWeight: 'bold',
                        color: '#ffffff'
                    }}>
                        Upload Thumbnail
                    </Typography>
                    <Button
                        variant="contained"
                        color="secondary"
                        component="label"
                        sx={{ 
                            fontSize: '16px',
                            fontWeight: 'bold',
                            textTransform: 'none',
                            py: 1.5,
                            px: 3,
                            borderRadius: '8px',
                            color: '#ffffff'
                        }}
                    >
                        Choose File
                        <input
                            type="file"
                            accept="image/*"
                            hidden
                            onChange={handleThumbnailUpload}
                        />
                    </Button>
                    {thumbnailFile && (
                        <Typography variant="body2" sx={{ 
                            mt: 1, 
                            fontSize: '14px',
                            color: '#ffffff'
                        }}>
                            Selected: {thumbnailFile.name}
                        </Typography>
                    )}
                </Box>

                {/* Genres */}
                <Box sx={{ mb: 3 }}>
                    <Autocomplete
                        multiple
                        freeSolo
                        options={genreSuggestions.map((option) => option.name || option.Name)}
                        value={genres}
                        onChange={(event, newValue) => {
                            // Convert all values to lowercase
                            const normalizedGenres = newValue.map(genre => genre.toLowerCase());
                            setGenres(normalizedGenres);
                        }}
                        onInputChange={(event, newInputValue) => setGenreInput(newInputValue)}
                        renderTags={(value, getTagProps) =>
                            value.map((option, index) => (
                                <Chip
                                    key={index}
                                    variant="outlined"
                                    label={option}
                                    size="small"
                                    sx={{ fontSize: '12px' }}
                                    {...getTagProps({ index })}
                                />
                            ))
                        }
                        renderInput={(params) => (
                            <TextField
                                {...params}
                                label="Genres"
                                placeholder="Type to search genres or add new..."
                                variant="outlined"
                                sx={{
                                    '& .MuiInputBase-input': {
                                        fontSize: '14px'
                                    },
                                    '& .MuiInputBase-input::placeholder': {
                                        color: '#ffffff',
                                        opacity: 1
                                    },
                                    '& .MuiInputLabel-root': {
                                        color: '#ffffff',
                                        fontSize: '14px'
                                    },
                                    '& .MuiInputLabel-root.Mui-focused': {
                                        color: '#ffffff'
                                    }
                                }}
                            />
                        )}
                    />
                </Box>

                {/* Topics */}
                <Box sx={{ mb: 3 }}>
                    <Autocomplete
                        multiple
                        freeSolo
                        options={topicSuggestions.map((option) => option.name || option.Name)}
                        value={topics}
                        onChange={(event, newValue) => {
                            // Convert all values to lowercase
                            const normalizedTopics = newValue.map(topic => topic.toLowerCase());
                            setTopics(normalizedTopics);
                        }}
                        onInputChange={(event, newInputValue) => setTopicInput(newInputValue)}
                        renderTags={(value, getTagProps) =>
                            value.map((option, index) => (
                                <Chip
                                    key={index}
                                    variant="outlined"
                                    label={option}
                                    size="small"
                                    sx={{ fontSize: '12px' }}
                                    {...getTagProps({ index })}
                                />
                            ))
                        }
                        renderInput={(params) => (
                            <TextField
                                {...params}
                                label="Topics"
                                placeholder="Type to search topics or add new..."
                                variant="outlined"
                                sx={{
                                    '& .MuiInputBase-input': {
                                        fontSize: '14px'
                                    },
                                    '& .MuiInputBase-input::placeholder': {
                                        color: '#ffffff',
                                        opacity: 1
                                    },
                                    '& .MuiInputLabel-root': {
                                        color: '#ffffff',
                                        fontSize: '14px'
                                    },
                                    '& .MuiInputLabel-root.Mui-focused': {
                                        color: '#ffffff'
                                    }
                                }}
                            />
                        )}
                    />
                </Box>

                {/* Notes */}
                <TextField
                    label="Notes"
                    placeholder="Add any notes or thoughts about this media..."
                    variant="outlined"
                    fullWidth
                    multiline
                    rows={4}
                    margin="normal"
                    value={notes}
                    onChange={(e) => setNotes(e.target.value)}
                    sx={{
                        mb: 3,
                        '& .MuiInputBase-input': {
                            fontSize: '14px'
                        },
                        '& .MuiInputBase-input::placeholder': {
                            color: '#ffffff',
                            opacity: 1
                        },
                        '& .MuiInputLabel-root': {
                            color: '#ffffff',
                            fontSize: '14px'
                        },
                        '& .MuiInputLabel-root.Mui-focused': {
                            color: '#ffffff'
                        }
                    }}
                />

                {/* Mixlist Selection */}
                <Box sx={{ mb: 3 }}>
                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                        <Typography variant="h6" sx={{ 
                            fontSize: '18px',
                            fontWeight: 'bold',
                            color: '#ffffff'
                        }}>
                            Add to Mixlists
                        </Typography>
                        <Button
                            variant="contained"
                            color="secondary"
                            onClick={() => navigate('/create-mixlist', { state: { returnTo: '/add-media' } })}
                            sx={{ 
                                fontSize: '16px',
                                fontWeight: 'bold',
                                textTransform: 'none',
                                py: 1.5,
                                px: 3,
                                borderRadius: '8px',
                                color: '#ffffff'
                            }}
                        >
                            + New Mixlist
                        </Button>
                    </Box>
                    <TextField
                        placeholder="Type to search mixlists..."
                        variant="outlined"
                        fullWidth
                        value={mixlistInput}
                        onChange={(e) => setMixlistInput(e.target.value)}
                        onKeyPress={handleMixlistKeyPress}
                        sx={{
                            mb: 2,
                            '& .MuiInputBase-input': {
                                fontSize: '16px'
                            },
                            '& .MuiInputBase-input::placeholder': {
                                color: '#ffffff',
                                opacity: 1
                            }
                        }}
                    />
                    
                    {/* Selected Mixlists */}
                    {selectedMixlists.length > 0 && (
                        <Box sx={{ mb: 2 }}>
                            <Typography variant="body2" sx={{ 
                                fontSize: '14px', 
                                color: '#ffffff', 
                                mb: 1,
                                fontWeight: 'bold'
                            }}>
                                Selected mixlists:
                            </Typography>
                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                {selectedMixlists.map((mixlist) => (
                                    <Chip
                                        key={mixlist.Id}
                                        label={mixlist.Name}
                                        onDelete={() => removeMixlist(mixlist)}
                                        size="small"
                                        sx={{ fontSize: '14px' }}
                                    />
                                ))}
                            </Box>
                        </Box>
                    )}
                    
                    {/* Available Mixlists */}
                    {availableMixlists.length > 0 && mixlistInput && (
                        <Box sx={{ mt: 1 }}>
                            <Typography variant="body2" sx={{ 
                                fontSize: '14px', 
                                color: '#ffffff', 
                                mb: 1,
                                fontWeight: 'bold'
                            }}>
                                Available mixlists:
                            </Typography>
                            <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                                {availableMixlists
                                    .filter(mixlist => {
                                        // Handle both Name and name properties, and Id vs id
                                        const name = mixlist.Name || mixlist.name || '';
                                        const id = mixlist.Id || mixlist.id;
                                        return name.toLowerCase().includes(mixlistInput.toLowerCase()) &&
                                            !selectedMixlists.some(p => (p.Id || p.id) === id);
                                    })
                                    .slice(0, 5)
                                    .map(mixlist => {
                                        // Ensure we have consistent property names
                                        const normalizedMixlist = {
                                            ...mixlist,
                                            Id: mixlist.Id || mixlist.id,
                                            Name: mixlist.Name || mixlist.name || `Mixlist ${mixlist.Id || mixlist.id}`
                                        };
                                        
                                        return (
                                            <Chip
                                                key={normalizedMixlist.Id}
                                                label={normalizedMixlist.Name}
                                                variant="outlined"
                                                size="small"
                                                onClick={() => {
                                                    setSelectedMixlists([...selectedMixlists, normalizedMixlist]);
                                                    setMixlistInput('');
                                                }}
                                                sx={{ 
                                                    fontSize: '12px', 
                                                    cursor: 'pointer',
                                                    '&:hover': {
                                                        backgroundColor: 'rgba(255, 255, 255, 0.1)'
                                                    }
                                                }}
                                            />
                                        );
                                    })
                                }
                            </Box>
                        </Box>
                    )}
                    

                </Box>

                {/* Media type specific fields */}
                {renderMediaTypeSpecificFields()}

                <Button 
                    type="submit" 
                    variant="contained" 
                    color="primary" 
                    sx={{ 
                        mt: 3, 
                        width: '100%',
                        fontSize: '16px',
                        fontWeight: 'bold',
                        py: 1.5
                    }}
                >
                    Save Media
                </Button>
            </Box>
        </Box>
    );
}

export default AddMediaForm;