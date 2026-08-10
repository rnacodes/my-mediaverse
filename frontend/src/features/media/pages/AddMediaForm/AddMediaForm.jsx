import { useForm, FormProvider } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, Button } from '@mui/material';
import { useAddMediaToMixlist } from '@/hooks/useMixlist';
import { useCreatePodcastEpisode, useCreatePodcastSeries } from '@/hooks/usePodcast';
import { useCreateBook } from '@/hooks/useBook';
import { useCreateMovie } from '@/hooks/useMovie';
import { useCreateTvShow } from '@/hooks/useTvShow';
import { useCreateVideo } from '@/hooks/useVideo';
import {
  mediaSchema, defaultValues, SUPPORTED_TYPES,
  buildBookPayload, buildEpisodePayload, buildSeriesPayload,
  buildMoviePayload, buildTvShowPayload, buildVideoPayload,
} from '@/features/media/form/schema';
import CommonFields from '@/features/media/form/CommonFields';
import TypeSpecificFields from '@/features/media/form/TypeSpecificFields';
import MixlistSelector from './MixlistSelector';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';

function AddMediaForm() {
  const navigate = useNavigate();
  const methods = useForm({ resolver: zodResolver(mediaSchema), defaultValues });

  const createBook = useCreateBook();
  const createMovie = useCreateMovie();
  const createTvShow = useCreateTvShow();
  const createVideo = useCreateVideo();
  const createEpisode = useCreatePodcastEpisode();
  const createSeries = useCreatePodcastSeries();
  const addToMixlist = useAddMediaToMixlist();

  // Route the validated form to the right create endpoint, then attach it to
  // any selected mixlists and navigate to the new item.
  const createMediaItem = (data) => {
    switch (data.mediaType) {
      case 'Book':
        return createBook.mutateAsync(buildBookPayload(data));
      case 'Movie':
        return createMovie.mutateAsync(buildMoviePayload(data));
      case 'TVShow':
        return createTvShow.mutateAsync(buildTvShowPayload(data));
      case 'Video':
        return createVideo.mutateAsync(buildVideoPayload(data));
      case 'Podcast':
        return data.podcastType === 'Episode'
          ? createEpisode.mutateAsync(buildEpisodePayload(data))
          : createSeries.mutateAsync(buildSeriesPayload(data));
      default:
        throw new Error(`Unsupported media type: ${data.mediaType}`);
    }
  };

  const onSubmit = async (data) => {
    if (!SUPPORTED_TYPES.includes(data.mediaType)) {
      alert('Currently only Podcast, Book, Movie, TVShow, and Video media types are supported by the backend. Other media types are not yet implemented.');
      return;
    }

    try {
      const created = await createMediaItem(data);
      const mediaId = created.id || created.Id;

      for (const mixlist of data.selectedMixlists) {
        if (!mediaId) break;
        try {
          await addToMixlist.mutateAsync({ mixlistId: mixlist.Id || mixlist.id, mediaItemId: mediaId });
        } catch (mixlistError) {
          console.error(`Failed to add media to mixlist ${mixlist.Name || mixlist.name}:`, mixlistError);
        }
      }

      navigate(`/media/${mediaId}`);
    } catch (error) {
      console.error('Failed to add media:', error);
      let errorMessage = 'Unknown error';
      const errData = error.response?.data;
      if (errData) {
        if (typeof errData === 'string') errorMessage = errData;
        else if (errData.message) errorMessage = errData.message;
        else if (errData.errors) {
          errorMessage = `Validation errors:\n${Object.entries(errData.errors)
            .map(([field, messages]) => `${field}: ${messages.join(', ')}`)
            .join('\n')}`;
        } else errorMessage = JSON.stringify(errData);
      } else if (error.message) {
        errorMessage = error.message;
      }
      alert(`Failed to add media (Status ${error.response?.status}):\n${errorMessage}`);
    }
  };

  return (
    <Box
      sx={{
        minHeight: '100vh',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'flex-start',
        py: 4,
        px: 2,
        '& .MuiInputBase-input': { fontSize: '16px !important' },
        '& .MuiInputLabel-root': { fontSize: '16px !important' },
        '& .MuiSelect-select': { fontSize: '16px !important' },
        '& .MuiFormControlLabel-label': { fontSize: '16px !important' },
      }}
    >
      <FormProvider {...methods}>
        <Box
          component="form"
          onSubmit={methods.handleSubmit(onSubmit)}
          sx={{
            width: '100%',
            maxWidth: '600px',
            backgroundColor: 'background.paper',
            borderRadius: '16px',
            p: 4,
            boxShadow: '0 4px 12px rgba(0,0,0,0.3)',
          }}
        >
          <Typography variant="h4" component="h1" gutterBottom sx={{ textAlign: 'center', fontSize: '28px', fontWeight: 'bold', mb: 3 }}>
            Add New Media
          </Typography>

          <CommonFields />
          <MixlistSelector />
          <TypeSpecificFields />

          <DemoWriteGuard title="Adding media is not available in the demo" style={{ width: '100%' }}>
            <Button
              type="submit"
              variant="contained"
              color="primary"
              disabled={methods.formState.isSubmitting}
              sx={{ mt: 3, width: '100%', fontSize: '16px', fontWeight: 'bold', py: 1.5 }}
            >
              Save Media
            </Button>
          </DemoWriteGuard>
        </Box>
      </FormProvider>
    </Box>
  );
}

export default AddMediaForm;
