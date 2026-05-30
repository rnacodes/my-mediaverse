import { z } from 'zod';

// Edit form covers only the BASE media fields. Media type is locked (display
// only), and type-specific fields are intentionally out of scope for this form
// (see the deferred edit-parity feature). This schema deliberately mirrors the
// common slice of the AddMediaForm Phase 5 schema; it is kept local to avoid
// coupling the two forms' schemas before the Phase 7 reorg.
export const editMediaSchema = z.object({
  title: z.string().trim().min(1, 'Title is required'),
  mediaType: z.string(),
  status: z.string(),
  rating: z.string().optional(),
  ownershipStatus: z.string().optional(),
  link: z.string().optional(),
  description: z.string().optional(),
  notes: z.string().optional(),
  thumbnail: z.string().optional(),
  genre: z.string().optional(),
  dateCompleted: z.string().optional(),
});

export const defaultValues = {
  title: '',
  mediaType: 'Other',
  status: 'Uncharted',
  rating: '',
  ownershipStatus: '',
  link: '',
  description: '',
  notes: '',
  thumbnail: '',
  genre: '',
  dateCompleted: '',
};

// Map a fetched media item (which may come back in camelCase or PascalCase)
// into the form's default-value shape. Used with reset() once data loads.
export function mapMediaItemToForm(mediaItem) {
  const completed = mediaItem.dateCompleted || mediaItem.DateCompleted;
  return {
    title: mediaItem.title || mediaItem.Title || '',
    mediaType: mediaItem.mediaType || mediaItem.MediaType || 'Other',
    status: mediaItem.status || mediaItem.Status || 'Uncharted',
    rating: mediaItem.rating || mediaItem.Rating || '',
    ownershipStatus: mediaItem.ownershipStatus || mediaItem.OwnershipStatus || '',
    link: mediaItem.link || mediaItem.Link || '',
    description: mediaItem.description || mediaItem.Description || '',
    notes: mediaItem.notes || mediaItem.Notes || '',
    thumbnail: mediaItem.thumbnail || mediaItem.Thumbnail || '',
    genre: mediaItem.genre || mediaItem.Genre || '',
    dateCompleted: completed ? new Date(completed).toISOString().split('T')[0] : '',
  };
}

// Build the PUT /api/media/{id} payload. Topics/genres are owned by
// TopicsGenresSection, so they're carried through from the current media item.
export function buildUpdatePayload(formData, mediaItem) {
  return {
    title: formData.title,
    mediaType: formData.mediaType,
    status: formData.status,
    rating: formData.rating || null,
    ownershipStatus: formData.ownershipStatus || null,
    link: formData.link || null,
    description: formData.description || null,
    notes: formData.notes || null,
    thumbnail: formData.thumbnail || null,
    genre: formData.genre || null,
    dateCompleted: formData.dateCompleted ? new Date(formData.dateCompleted).toISOString() : null,
    topics: mediaItem?.topics || mediaItem?.topicNames || [],
    genres: mediaItem?.genres || mediaItem?.genreNames || [],
  };
}

// Vault chip color, shared by the linked-notes display and the link dialog.
export function getVaultColor(vaultName) {
  switch (vaultName?.toLowerCase()) {
    case 'general':
      return '#4caf50';
    case 'programming':
      return '#2196f3';
    default:
      return '#9e9e9e';
  }
}
