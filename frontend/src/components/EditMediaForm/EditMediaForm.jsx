import { useEffect, useRef, useState, useMemo } from 'react';
import { useParams, useNavigate, Link as RouterLink } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import {
  Container, Typography, TextField, Button, Box, MenuItem, Card, CardContent,
  Snackbar, Alert, CircularProgress, Dialog, DialogTitle, DialogContent,
  DialogContentText, DialogActions, IconButton, Chip, Tooltip,
} from '@mui/material';
import {
  Save, Cancel, ArrowBack, Delete, Add as AddIcon, Close,
  Delete as DeleteIcon, OpenInNew as OpenInNewIcon, Article as NoteIcon, PlaylistAdd,
} from '@mui/icons-material';
import { useMediaItem, useUpdateMedia, useDeleteMedia } from '../../hooks/useMedia';
import { useUploadThumbnail } from '../../hooks/useUpload';
import { useNotesForMedia, useUnlinkNoteFromMedia } from '../../hooks/useNote';
import { useAllMixlists, useRemoveMediaFromMixlist } from '../../hooks/useMixlist';
import { formatStatus } from '../../utils/formatters';
import TopicsGenresSection from '../TopicsGenresSection';
import LinkNotesDialog from './LinkNotesDialog';
import AddToMixlistDialog from './AddToMixlistDialog';
import { editMediaSchema, defaultValues, mapMediaItemToForm, buildUpdatePayload, getVaultColor } from './schema';

const STATUS_OPTIONS = ['Uncharted', 'ActivelyExploring', 'Completed', 'Abandoned'];
const RATING_OPTIONS = ['SuperLike', 'Like', 'Neutral', 'Dislike'];
const OWNERSHIP_OPTIONS = ['Own', 'Rented', 'Streamed'];

function EditMediaForm() {
  const { id } = useParams();
  const navigate = useNavigate();

  const [snackbar, setSnackbar] = useState({ open: false, message: '', severity: 'success' });
  const [thumbnailFile, setThumbnailFile] = useState(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [linkNoteDialog, setLinkNoteDialog] = useState(false);
  const [addMixlistDialog, setAddMixlistDialog] = useState(false);

  const notify = (message, severity = 'success') => setSnackbar({ open: true, message, severity });

  // Queries
  const mediaQuery = useMediaItem(id);
  const mediaItem = mediaQuery.data ?? null;

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

  // Mutations
  const updateMediaMutation = useUpdateMedia();
  const deleteMediaMutation = useDeleteMedia();
  const uploadThumbnailMutation = useUploadThumbnail();
  const unlinkNoteMutation = useUnlinkNoteFromMedia();
  const removeFromMixlistMutation = useRemoveMediaFromMixlist();

  const saving = updateMediaMutation.isPending;
  const savingNote = unlinkNoteMutation.isPending;
  const savingMixlist = removeFromMixlistMutation.isPending;

  // Form
  const { control, handleSubmit, reset, watch, setValue } = useForm({
    resolver: zodResolver(editMediaSchema),
    defaultValues,
  });
  const thumbnail = watch('thumbnail');
  const titleValue = watch('title');

  // Prefill once per media id (so a mid-edit mixlist/notes refetch doesn't
  // clobber unsaved field edits).
  const initializedRef = useRef(false);
  useEffect(() => {
    initializedRef.current = false;
  }, [id]);
  useEffect(() => {
    if (mediaItem && !initializedRef.current) {
      initializedRef.current = true;
      reset(mapMediaItemToForm(mediaItem));
    }
  }, [mediaItem, reset]);

  useEffect(() => {
    if (mediaQuery.error) {
      console.error('Failed to fetch media:', mediaQuery.error);
      notify('Failed to load media item', 'error');
    }
  }, [mediaQuery.error]);

  const handleThumbnailUpload = (event) => {
    const file = event.target.files[0];
    if (!file) return;
    setThumbnailFile(file);
    uploadThumbnailMutation.mutate(file, {
      onSuccess: (data) => {
        setValue('thumbnail', data.url);
        notify('Thumbnail uploaded successfully!', 'success');
      },
      onError: (error) => {
        console.error('Error uploading thumbnail:', error);
        notify('Failed to upload thumbnail. Please try again.', 'error');
        setThumbnailFile(null);
      },
    });
  };

  const onSubmit = (formData) => {
    updateMediaMutation.mutate(
      { id, mediaData: buildUpdatePayload(formData, mediaItem) },
      {
        onSuccess: () => {
          notify('Media item updated successfully!', 'success');
          setTimeout(() => navigate(`/media/${id}`), 1500);
        },
        onError: (error) => {
          console.error('Failed to update media:', error);
          notify(error.response?.data?.message || 'Failed to update media item', 'error');
        },
      }
    );
  };

  const handleDelete = () => {
    deleteMediaMutation.mutate(id, {
      onSuccess: () => {
        notify('Media item deleted successfully!', 'success');
        setTimeout(() => navigate('/'), 1500);
      },
      onError: (error) => {
        console.error('Failed to delete media:', error);
        notify(error.response?.data?.error || 'Failed to delete media item', 'error');
      },
      onSettled: () => setDeleteDialogOpen(false),
    });
  };

  const handleUnlinkNote = (noteId, noteTitle) => {
    unlinkNoteMutation.mutate(
      { noteId, mediaItemId: id },
      {
        onSuccess: () => notify(`Unlinked note "${noteTitle}"`, 'success'),
        onError: (error) => {
          console.error('Error unlinking note:', error);
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
          mediaQuery.refetch();
          notify(`Removed from "${mixlistName}"`, 'success');
        },
        onError: (error) => {
          console.error('Error removing from mixlist:', error);
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

  if (mediaQuery.isLoading) {
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
      <Box sx={{ mt: { xs: 2, sm: 3, md: 4 } }}>
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
            <form onSubmit={handleSubmit(onSubmit)}>
              <Box sx={{ display: 'flex', flexDirection: 'column', gap: { xs: 2, sm: 3 } }}>
                {/* Title */}
                <Controller
                  name="title"
                  control={control}
                  render={({ field, fieldState }) => (
                    <TextField
                      {...field}
                      fullWidth
                      label="Title *"
                      required
                      error={!!fieldState.error}
                      helperText={fieldState.error?.message}
                    />
                  )}
                />

                {/* Media Type (display only) */}
                <Controller
                  name="mediaType"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} fullWidth label="Media Type" disabled helperText="Media type cannot be changed after creation" />
                  )}
                />

                {/* Status */}
                <Controller
                  name="status"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} fullWidth select label="Status">
                      {STATUS_OPTIONS.map((option) => (
                        <MenuItem key={option} value={option}>
                          {formatStatus(option)}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />

                {/* Rating */}
                <Controller
                  name="rating"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} fullWidth select label="Rating">
                      <MenuItem value="">None</MenuItem>
                      {RATING_OPTIONS.map((option) => (
                        <MenuItem key={option} value={option}>
                          {option}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />

                {/* Ownership Status */}
                <Controller
                  name="ownershipStatus"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} fullWidth select label="Ownership Status">
                      <MenuItem value="">None</MenuItem>
                      {OWNERSHIP_OPTIONS.map((option) => (
                        <MenuItem key={option} value={option}>
                          {option}
                        </MenuItem>
                      ))}
                    </TextField>
                  )}
                />

                {/* Link */}
                <Controller
                  name="link"
                  control={control}
                  render={({ field }) => <TextField {...field} fullWidth label="Link/URL" placeholder="https://example.com" />}
                />

                {/* Topics & Genres */}
                {mediaItem && (
                  <TopicsGenresSection mediaItem={mediaItem} setSnackbar={setSnackbar} onUpdate={() => mediaQuery.refetch()} />
                )}

                {/* Thumbnail URL */}
                <Controller
                  name="thumbnail"
                  control={control}
                  render={({ field }) => <TextField {...field} fullWidth label="Thumbnail URL" placeholder="https://example.com/image.jpg" />}
                />

                {/* Thumbnail Upload */}
                <Box sx={{ mt: 2 }}>
                  <Typography variant="body1" sx={{ mb: 2, fontSize: { xs: '0.875rem', sm: '1rem' }, fontWeight: 'bold' }}>
                    Upload New Thumbnail
                  </Typography>
                  <Button
                    variant="contained"
                    color="primary"
                    component="label"
                    sx={{ fontSize: { xs: '0.875rem', sm: '1rem' }, fontWeight: 'bold', textTransform: 'none', py: 1.5, px: 3, minHeight: '48px', width: { xs: '100%', sm: 'auto' }, borderRadius: '8px' }}
                  >
                    Choose File
                    <input type="file" accept="image/*" hidden onChange={handleThumbnailUpload} />
                  </Button>
                  {thumbnailFile && (
                    <Typography variant="body2" sx={{ mt: 1, fontSize: { xs: '0.75rem', sm: '0.875rem' }, color: 'text.secondary' }}>
                      Selected: {thumbnailFile.name}
                    </Typography>
                  )}
                  {!thumbnailFile && thumbnail && (
                    <Typography variant="body2" sx={{ mt: 1, fontSize: { xs: '0.75rem', sm: '0.875rem' }, color: 'text.secondary' }}>
                      Current: {thumbnail}
                    </Typography>
                  )}
                </Box>

                {/* Date Completed */}
                <Controller
                  name="dateCompleted"
                  control={control}
                  render={({ field }) => (
                    <TextField {...field} fullWidth label="Date Completed" type="date" InputLabelProps={{ shrink: true }} />
                  )}
                />

                {/* Description */}
                <Controller
                  name="description"
                  control={control}
                  render={({ field }) => <TextField {...field} fullWidth label="Description" multiline rows={4} />}
                />

                {/* Notes */}
                <Controller
                  name="notes"
                  control={control}
                  render={({ field }) => <TextField {...field} fullWidth label="Notes" multiline rows={4} />}
                />

                {/* Mixlists */}
                <Box sx={{ border: '1px solid rgba(255, 255, 255, 0.23)', borderRadius: 1, p: 2 }}>
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
                <Box sx={{ border: '1px solid rgba(255, 255, 255, 0.23)', borderRadius: 1, p: 2 }}>
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
            </form>
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
        onChanged={() => mediaQuery.refetch()}
      />
    </Container>
  );
}

export default EditMediaForm;
