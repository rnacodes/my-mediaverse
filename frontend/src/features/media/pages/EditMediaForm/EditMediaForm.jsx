import { useEffect, useRef, useState, useMemo } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import { useForm, FormProvider } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Container, Typography, Button, Box, Card, CardContent,
  Snackbar, Alert, CircularProgress, Dialog, DialogTitle, DialogContent,
  DialogContentText, DialogActions, IconButton, Chip, Tooltip,
} from '@mui/material';
import {
  Save, Cancel, ArrowBack, Delete, Add as AddIcon, Close,
  Delete as DeleteIcon, OpenInNew as OpenInNewIcon, Article as NoteIcon, PlaylistAdd,
} from '@mui/icons-material';
import { useUpdateMedia, useDeleteMedia } from '@/hooks/useMedia';
import { useUpdateBook } from '@/hooks/useBook';
import { useUpdateMovie } from '@/hooks/useMovie';
import { useUpdateTvShow } from '@/hooks/useTvShow';
import { useUpdateVideo } from '@/hooks/useVideo';
import { useUpdatePodcastSeries, useUpdatePodcastEpisode } from '@/hooks/usePodcast';
import { useNotesForMedia, useUnlinkNoteFromMedia } from '@/hooks/useNote';
import { useAllMixlists, useRemoveMediaFromMixlist } from '@/hooks/useMixlist';
import { useMergedMediaItem } from '@/hooks/useMergedMediaItem';
import {
  mediaSchema, defaultValues, mapMediaItemToFormValues,
  buildBookPayload, buildEpisodePayload, buildSeriesPayload,
  buildMoviePayload, buildTvShowPayload, buildVideoPayload, buildMediaPayload,
} from '@/features/media/form/schema';
import CommonFields from '@/features/media/form/CommonFields';
import TypeSpecificFields from '@/features/media/form/TypeSpecificFields';
import LinkNotesDialog from './LinkNotesDialog';
import AddToMixlistDialog from './AddToMixlistDialog';
import { getVaultColor } from './schema';

function EditMediaForm() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [linkNoteDialog, setLinkNoteDialog] = useState(false);
  const [addMixlistDialog, setAddMixlistDialog] = useState(false);

  const notify = (message, severity = 'success') => setSnackbar({ open: true, message, severity });

  // Base + type-specific detail, merged for prefill.
  const { basicQuery, mediaItem, isDetailReady, isLoading, error } = useMergedMediaItem(id);

  const linkedNotesQuery = useNotesForMedia(id);
  const linkedNotes = linkedNotesQuery.data ?? [];

  const mixlistsQuery = useAllMixlists();
  const { currentMixlists, availableMixlists } = useMemo(() => {
    const all = mixlistsQuery.data ?? [];
    const mixlistIds = mediaItem?.mixlistIds || [];
    if (mixlistIds.length === 0) return { currentMixlists: [], availableMixlists: all };
    const set = new Set(mixlistIds);
    return {
      currentMixlists: all.filter((m) => set.has(m.id)),
      availableMixlists: all.filter((m) => !set.has(m.id)),
    };
  }, [mixlistsQuery.data, mediaItem]);

  // Mutations — per-type update where available, generic fallback otherwise.
  const updateBook = useUpdateBook();
  const updateMovie = useUpdateMovie();
  const updateTvShow = useUpdateTvShow();
  const updateVideo = useUpdateVideo();
  const updateSeries = useUpdatePodcastSeries();
  const updateEpisode = useUpdatePodcastEpisode();
  const updateMedia = useUpdateMedia();
  const deleteMediaMutation = useDeleteMedia();
  const unlinkNoteMutation = useUnlinkNoteFromMedia();
  const removeFromMixlistMutation = useRemoveMediaFromMixlist();

  const savingNote = unlinkNoteMutation.isPending;
  const savingMixlist = removeFromMixlistMutation.isPending;

  // Form
  const methods = useForm({ resolver: zodResolver(mediaSchema), defaultValues });
  const { handleSubmit, reset, watch, formState: { isSubmitting } } = methods;
  const titleValue = watch('title');
  const saving = isSubmitting;

  // Prefill once per media id, so a mid-edit mixlist/notes refetch doesn't clobber
  // unsaved field edits. Wait for the type-specific detail (isDetailReady) so the
  // first snapshot isn't the base-only item (which would drop author/director/etc.).
  const initializedRef = useRef(false);
  useEffect(() => {
    initializedRef.current = false;
  }, [id]);
  useEffect(() => {
    if (mediaItem && isDetailReady && !initializedRef.current) {
      initializedRef.current = true;
      reset(mapMediaItemToFormValues(mediaItem));
    }
  }, [mediaItem, isDetailReady, reset]);

  useEffect(() => {
    if (error) {
      console.error('Failed to fetch media:', error);
      notify('Failed to load media item', 'error');
    }
  }, [error]);

  // Route the validated form to the right update endpoint by media type.
  const updateByType = (data) => {
    switch (data.mediaType) {
      case 'Book':
        return updateBook.mutateAsync({ id, bookData: buildBookPayload(data) });
      case 'Movie':
        return updateMovie.mutateAsync({ id, movieData: buildMoviePayload(data) });
      case 'TVShow':
        return updateTvShow.mutateAsync({ id, tvShowData: buildTvShowPayload(data) });
      case 'Video':
        return updateVideo.mutateAsync({ id, videoData: buildVideoPayload(data) });
      case 'Podcast':
        return data.podcastType === 'Episode'
          ? updateEpisode.mutateAsync({ id, episodeData: buildEpisodePayload(data), seriesId: data.podcastSeriesId })
          : updateSeries.mutateAsync({ id, seriesData: buildSeriesPayload(data) });
      default:
        return updateMedia.mutateAsync({ id, mediaData: buildMediaPayload(data) });
    }
  };

  const onSubmit = async (data) => {
    try {
      await updateByType(data);
      notify('Media item updated successfully!', 'success');
      setTimeout(() => navigate(`/media/${id}`), 1500);
    } catch (err) {
      console.error('Failed to update media:', err);
      notify(err.response?.data?.message || err.response?.data?.error || 'Failed to update media item', 'error');
    }
  };

  const handleDelete = () => {
    deleteMediaMutation.mutate(id, {
      onSuccess: () => {
        notify('Media item deleted successfully!', 'success');
        setTimeout(() => navigate('/'), 1500);
      },
      onError: (err) => {
        console.error('Failed to delete media:', err);
        notify(err.response?.data?.error || 'Failed to delete media item', 'error');
      },
      onSettled: () => setDeleteDialogOpen(false),
    });
  };

  const handleUnlinkNote = (noteId, noteTitle) => {
    unlinkNoteMutation.mutate(
      { noteId, mediaItemId: id },
      {
        onSuccess: () => notify(`Unlinked note "${noteTitle}"`, 'success'),
        onError: (err) => {
          console.error('Error unlinking note:', err);
          notify('Failed to unlink note', 'error');
        },
      }
    );
  };

  const handleRemoveFromMixlist = (mixlistId, mixlistName) => {
    removeFromMixlistMutation.mutate(
      { mixlistId, mediaItemId: id },
      {
        onSuccess: () => {
          basicQuery.refetch();
          notify(`Removed from "${mixlistName}"`, 'success');
        },
        onError: (err) => {
          console.error('Error removing from mixlist:', err);
          notify('Failed to remove from mixlist', 'error');
        },
      }
    );
  };

  const whiteOutlinedBtn = {
    color: 'white',
    borderColor: 'white',
    '&:hover': { borderColor: 'white', backgroundColor: 'rgba(255, 255, 255, 0.08)' },
  };

  if (isLoading) {
    return (
      <Container maxWidth="md" sx={{ px: { xs: 2, sm: 3 } }}>
        <Box sx={{ display: 'flex', flexDirection: 'column', justifyContent: 'center', alignItems: 'center', minHeight: '50vh', gap: 2 }}>
          <CircularProgress size={60} />
          <Typography variant="h6" color="text.secondary" sx={{ fontSize: { xs: '1rem', sm: '1.25rem' } }}>
            Loading media item...
          </Typography>
        </Box>
      </Container>
    );
  }

  return (
    <Container maxWidth="md" sx={{ px: { xs: 2, sm: 3 }, py: { xs: 2, sm: 3, md: 4 } }}>
      <Box
        sx={{
          mt: { xs: 2, sm: 3, md: 4 },
          '& .MuiInputBase-input': { fontSize: '16px !important' },
          '& .MuiInputLabel-root': { fontSize: '16px !important' },
          '& .MuiSelect-select': { fontSize: '16px !important' },
          '& .MuiFormControlLabel-label': { fontSize: '16px !important' },
        }}
      >
        {/* Header */}
        <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, alignItems: { xs: 'flex-start', sm: 'center' }, mb: { xs: 3, sm: 4 }, gap: { xs: 2, sm: 0 } }}>
          <Button
            onClick={() => navigate(`/media/${id}`)}
            startIcon={<ArrowBack />}
            variant="outlined"
            sx={{ mr: { xs: 0, sm: 2 }, minHeight: '44px', fontSize: { xs: '0.875rem', sm: '1rem' }, ...whiteOutlinedBtn }}
          >
            Back
          </Button>
          <Typography variant="h4" component="h1" sx={{ fontWeight: 'bold', fontSize: { xs: '1.5rem', sm: '2rem', md: '2.125rem' } }}>
            Edit Media Item
          </Typography>
        </Box>

        <Card>
          <CardContent sx={{ p: { xs: 2, sm: 3, md: 4 } }}>
            <FormProvider {...methods}>
              <Box component="form" onSubmit={handleSubmit(onSubmit)}>
                {/* Shared common + type-specific fields (media type locked) */}
                <CommonFields lockMediaType />
                <TypeSpecificFields editing />

                {/* Mixlists */}
                <Box sx={{ border: '1px solid rgba(255, 255, 255, 0.23)', borderRadius: 1, p: 2, mt: 3 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <PlaylistAdd sx={{ fontSize: 20, color: 'rgba(255, 255, 255, 0.7)' }} />
                      <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                        Mixlists ({currentMixlists.length})
                      </Typography>
                    </Box>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<AddIcon />}
                      onClick={() => setAddMixlistDialog(true)}
                      disabled={savingMixlist}
                      sx={{ borderColor: 'rgba(255, 255, 255, 0.5)', color: 'white', '&:hover': { borderColor: 'white', backgroundColor: 'rgba(255, 255, 255, 0.08)' } }}
                    >
                      Add to Mixlist
                    </Button>
                  </Box>

                  {currentMixlists.length > 0 ? (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                      {currentMixlists.map((mixlist) => (
                        <Chip
                          key={mixlist.id}
                          label={mixlist.name}
                          onDelete={() => handleRemoveFromMixlist(mixlist.id, mixlist.name)}
                          deleteIcon={<Close sx={{ fontSize: 16, color: 'white !important' }} />}
                          disabled={savingMixlist}
                          onClick={() => navigate(`/mixlist/${mixlist.id}`)}
                          sx={{ cursor: 'pointer', backgroundColor: '#362759', color: 'white', fontWeight: 'bold', '&:hover': { backgroundColor: '#2a1e47' } }}
                        />
                      ))}
                    </Box>
                  ) : (
                    <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic', textAlign: 'center', py: 1 }}>
                      Not part of any mixlists. Click &quot;Add to Mixlist&quot; to assign.
                    </Typography>
                  )}
                </Box>

                {/* Linked Notes */}
                <Box sx={{ border: '1px solid rgba(255, 255, 255, 0.23)', borderRadius: 1, p: 2, mt: 3 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <NoteIcon sx={{ fontSize: 20, color: 'rgba(255, 255, 255, 0.7)' }} />
                      <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                        Linked Notes ({linkedNotes.length})
                      </Typography>
                    </Box>
                    <Button
                      variant="outlined"
                      size="small"
                      startIcon={<AddIcon />}
                      onClick={() => setLinkNoteDialog(true)}
                      disabled={savingNote}
                      sx={{ borderColor: 'rgba(255, 255, 255, 0.5)', color: 'white', '&:hover': { borderColor: 'white', backgroundColor: 'rgba(255, 255, 255, 0.08)' } }}
                    >
                      Link Note
                    </Button>
                  </Box>

                  {linkedNotesQuery.isLoading ? (
                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
                      <CircularProgress size={24} />
                    </Box>
                  ) : linkedNotes.length > 0 ? (
                    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
                      {linkedNotes.map((note) => (
                        <Box
                          key={note.id}
                          sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 1.5, borderRadius: 1, backgroundColor: 'rgba(255, 255, 255, 0.05)', border: '1px solid rgba(255, 255, 255, 0.1)', '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.08)' } }}
                        >
                          <Box sx={{ flex: 1, minWidth: 0 }}>
                            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                              <Typography
                                component={RouterLink}
                                to={`/note/${note.id}`}
                                sx={{ fontWeight: 'bold', color: 'white', textDecoration: 'none', '&:hover': { textDecoration: 'underline', color: '#90caf9' } }}
                              >
                                {note.title}
                              </Typography>
                              <Chip
                                label={note.vaultName}
                                size="small"
                                sx={{ backgroundColor: getVaultColor(note.vaultName), color: 'white', fontWeight: 'bold', fontSize: '0.65rem', height: '18px' }}
                              />
                            </Box>
                            {note.linkDescription && (
                              <Typography variant="caption" sx={{ color: 'rgba(255, 255, 255, 0.5)', fontStyle: 'italic' }}>
                                &quot;{note.linkDescription}&quot;
                              </Typography>
                            )}
                          </Box>
                          <Box sx={{ display: 'flex', gap: 0.5 }}>
                            {note.sourceUrl && (
                              <Tooltip title="View in Quartz">
                                <IconButton
                                  href={note.sourceUrl}
                                  target="_blank"
                                  rel="noopener noreferrer"
                                  size="small"
                                  sx={{ color: 'rgba(255, 255, 255, 0.5)', '&:hover': { color: 'white', backgroundColor: 'rgba(255, 255, 255, 0.1)' } }}
                                >
                                  <OpenInNewIcon fontSize="small" />
                                </IconButton>
                              </Tooltip>
                            )}
                            <Tooltip title="Unlink note">
                              <IconButton
                                onClick={() => handleUnlinkNote(note.id, note.title)}
                                size="small"
                                disabled={savingNote}
                                sx={{ color: 'rgba(255, 255, 255, 0.5)', '&:hover': { color: '#f44336', backgroundColor: 'rgba(244, 67, 54, 0.1)' } }}
                              >
                                <DeleteIcon fontSize="small" />
                              </IconButton>
                            </Tooltip>
                          </Box>
                        </Box>
                      ))}
                    </Box>
                  ) : (
                    <Typography variant="body2" color="text.secondary" sx={{ fontStyle: 'italic', textAlign: 'center', py: 1 }}>
                      No linked notes. Click &quot;Link Note&quot; to connect Obsidian notes.
                    </Typography>
                  )}
                </Box>

                {/* Action Buttons */}
                <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: 2, justifyContent: 'space-between', mt: { xs: 3, sm: 4 } }}>
                  <Button
                    variant="outlined"
                    startIcon={<Delete />}
                    onClick={() => setDeleteDialogOpen(true)}
                    disabled={saving}
                    size="large"
                    sx={{ width: { xs: '100%', sm: 'auto' }, minHeight: '48px', fontSize: { xs: '0.875rem', sm: '1rem' }, ...whiteOutlinedBtn }}
                  >
                    Delete Media
                  </Button>
                  <Box sx={{ display: 'flex', flexDirection: { xs: 'column', sm: 'row' }, gap: 2, width: { xs: '100%', sm: 'auto' } }}>
                    <Button
                      variant="outlined"
                      startIcon={<Cancel />}
                      onClick={() => navigate(`/media/${id}`)}
                      disabled={saving}
                      size="large"
                      sx={{ width: { xs: '100%', sm: 'auto' }, minHeight: '48px', fontSize: { xs: '0.875rem', sm: '1rem' }, ...whiteOutlinedBtn }}
                    >
                      Cancel
                    </Button>
                    <Button
                      type="submit"
                      variant="contained"
                      startIcon={<Save />}
                      disabled={saving}
                      size="large"
                      sx={{ width: { xs: '100%', sm: 'auto' }, minHeight: '48px', fontSize: { xs: '0.875rem', sm: '1rem' } }}
                    >
                      {saving ? 'Saving...' : 'Save Changes'}
                    </Button>
                  </Box>
                </Box>
              </Box>
            </FormProvider>
          </CardContent>
        </Card>
      </Box>

      {/* Snackbar */}
      <Snackbar open={snackbar.open} autoHideDuration={6000} onClose={() => setSnackbar((s) => ({ ...s, open: false }))}>
        <Alert onClose={() => setSnackbar((s) => ({ ...s, open: false }))} severity={snackbar.severity}>
          {snackbar.message}
        </Alert>
      </Snackbar>

      {/* Delete Confirmation */}
      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>Confirm Delete</DialogTitle>
        <DialogContent>
          <DialogContentText>
            Are you sure you want to delete &quot;{titleValue}&quot;? This action cannot be undone.
          </DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleDelete} color="error" variant="contained">
            Delete
          </Button>
        </DialogActions>
      </Dialog>

      <LinkNotesDialog
        open={linkNoteDialog}
        onClose={() => setLinkNoteDialog(false)}
        mediaId={id}
        mediaTitle={titleValue}
        linkedNotes={linkedNotes}
        onResult={notify}
      />

      <AddToMixlistDialog
        open={addMixlistDialog}
        onClose={() => setAddMixlistDialog(false)}
        mediaId={id}
        mediaTitle={titleValue}
        availableMixlists={availableMixlists}
        onResult={notify}
        onChanged={() => basicQuery.refetch()}
      />
    </Container>
  );
}

export default EditMediaForm;
