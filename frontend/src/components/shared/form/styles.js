// Shared MUI `sx` fragments for AddMediaForm sub-forms.
// Centralized here so the per-field styling stays consistent and the field
// components remain short (the old single-file form repeated these inline ~40x).

// Standard text-field styling used by most inputs (14px text, white label/placeholder).
export const fieldSx = {
  mb: 2,
  '& .MuiInputBase-input': { fontSize: '14px' },
  '& .MuiInputBase-input::placeholder': { color: '#ffffff', opacity: 1 },
  '& .MuiInputLabel-root': { color: '#ffffff', fontSize: '14px' },
  '& .MuiInputLabel-root.Mui-focused': { color: '#ffffff' },
};

// Select/FormControl wrapper styling (white label only — the input lives inside).
export const selectFormSx = {
  mb: 2,
  '& .MuiInputLabel-root': { color: '#ffffff', fontSize: '14px' },
  '& .MuiInputLabel-root.Mui-focused': { color: '#ffffff' },
  '& .MuiSelect-select': { fontSize: '14px' },
};

// Section heading used above each media-type-specific block.
export const sectionHeadingSx = {
  mb: 2,
  fontSize: '18px',
  fontWeight: 'bold',
  color: '#ffffff',
};

// Radio/checkbox label sizing.
export const choiceLabelSx = {
  '& .MuiFormControlLabel-label': { fontSize: '14px', color: '#ffffff' },
};
