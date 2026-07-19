import { Box } from '@mui/material';

//NOTE: These comments should be deleted once the logo is dropped in
// Central switch for the official MyMediaVerse logo.
//
// Until the real asset is dropped in, LOGO_SRC / LOGO_ICON_SRC stay null and
// every call site renders its existing "My MediaVerse" text fallback (passed as
// children), so nothing breaks and there are no broken-image icons.
//
// To enable the logo (one-line swap):
//   1. Drop the export into frontend/public/  (e.g. public/logo-horizontal.svg)
//      -- files in public/ are served from the site root, so the path is '/logo-horizontal.svg'.
//      (Alternatively import from src/assets and pass the imported value here.)
//   2. Set the constants below to those paths.
//
// Use the horizontal wordmark for the nav bar + homepage hero, and the compact
// icon-only mark for tight spots (mobile drawer header).
// The "outlined" wordmark has its text converted to paths.
export const LOGO_SRC = '/exports/outlined/logo-horizontal-tight.svg';
export const LOGO_ICON_SRC = '/exports/logo-icon.svg'; // falls back to LOGO_SRC if unset

// Renders the logo <img> when configured, otherwise renders `children` (the
// original text markup) so each call site keeps its exact current styling.
//   logoVariant: 'horizontal' (default) | 'icon'
//   imgSx:       MUI sx applied to the <img> (control height/width per placement)
const BrandLogo = ({ logoVariant = 'horizontal', alt = 'My MediaVerse', imgSx = {}, children }) => {
  const src = logoVariant === 'icon' ? (LOGO_ICON_SRC ?? LOGO_SRC) : LOGO_SRC;

  if (src) {
    return (
      <Box
        component="img"
        src={src}
        alt={alt}
        sx={{ display: 'block', width: 'auto', ...imgSx }}
      />
    );
  }

  return children;
};

export default BrandLogo;
