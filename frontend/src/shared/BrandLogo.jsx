import { Box } from '@mui/material';


//
// logo-horizontal.svg is a processed variant, not a raw design export -- its text
// is converted to paths, and its viewBox is trimmed to the artwork. Both matter:
// an SVG loaded through <img> cannot reach the page's web fonts, so live text
// would fall back off Roboto; and the untrimmed viewBox reserves ~30% of its
// height as blank space, which renders the wordmark far smaller than the text it
// replaces. Re-exporting over this file without both steps will regress the nav.
export const LOGO_SRC = '/logo-horizontal.svg';
export const LOGO_ICON_SRC = '/logo-icon.svg'; // falls back to LOGO_SRC if unset

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
