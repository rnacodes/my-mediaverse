import React from 'react';
import { Box } from '@mui/material';
import poweredByGoogle from '@/assets/branding/powered-by-google.png';

/**
 * The official "powered by Google" logo from the Google Books branding guidelines.
 * Shown once beside a block of Google Books results and on book profiles. The artwork is drawn for a white
 * background, so it sits on a small light chip to stay legible on the dark theme.
 */
function PoweredByGoogleLogo({ sx }) {
    return (
        <Box
            component="a"
            href="https://books.google.com"
            target="_blank"
            rel="noopener noreferrer"
            onClick={(e) => e.stopPropagation()}
            sx={{
                display: 'inline-flex',
                alignItems: 'center',
                backgroundColor: '#ffffff',
                borderRadius: 1,
                px: 0.75,
                py: 0.25,
                lineHeight: 0,
                ...sx
            }}
        >
            <img src={poweredByGoogle} alt="Powered by Google" width={62} height={30} />
        </Box>
    );
}

export default PoweredByGoogleLogo;
