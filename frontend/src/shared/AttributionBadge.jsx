import React from 'react';
import { Box, Typography, Link } from '@mui/material';

/**
 * Every external data provider's credit, in one place. A provider is one of
 * three layouts:
 *   inline  — small logo beside a line of text (TMDB's required notice)
 *   stacked — linked logo above "Powered by <name>" (Trakt)
 *   text    — a caption, optionally ending in a link (no logo)
 * Adding a provider is one entry here.
 */
const PROVIDERS = {
    tmdb: {
        layout: 'inline',
        name: 'TMDB',
        logo: '/tmdb-primary-short-logo.svg',
        logoAlt: 'TMDB',
        logoHeight: 20,
        text: 'This product uses the TMDB API but is not endorsed or certified by TMDB',
    },
    trakt: {
        layout: 'stacked',
        name: 'Trakt',
        url: 'https://trakt.tv',
        logo: '/trakt-logo-dark.svg',
        logoAlt: 'Trakt logo',
        sizes: {
            medium: { logoHeight: 40, fontSize: '13px' },
            large: { logoHeight: 50, fontSize: '20px' },
        },
    },
    apple: {
        layout: 'text',
        name: 'Apple Podcasts',
        text: 'Search results from Apple Podcasts',
    },
    podcastindex: {
        layout: 'text',
        name: 'Podcast Index',
        url: 'https://podcastindex.org',
        text: 'Found via',
    },
};

function AttributionBadge({ provider, size = 'medium', className, sx }) {
    const config = PROVIDERS[provider];
    if (!config) return null;

    const link = (children) => (
        <Link href={config.url} target="_blank" rel="noopener noreferrer" color="inherit">
            {children}
        </Link>
    );

    if (config.layout === 'inline') {
        return (
            <Box className={className} sx={{ display: 'flex', alignItems: 'center', gap: 1, justifyContent: 'center', ...sx }}>
                <img src={config.logo} alt={config.logoAlt} style={{ height: `${config.logoHeight}px`, width: 'auto' }} />
                <Typography variant="caption" sx={{ color: 'text.secondary', fontSize: '0.75rem' }}>
                    {config.text}
                </Typography>
            </Box>
        );
    }

    if (config.layout === 'stacked') {
        const { logoHeight, fontSize } = config.sizes[size] || config.sizes.medium;
        return (
            <Box className={className} sx={sx}>
                {link(<img src={config.logo} alt={config.logoAlt} style={{ height: `${logoHeight}px`, width: 'auto' }} />)}
                <Typography component="p" sx={{ fontSize, mt: 0 }}>
                    Powered by {link(config.name)}
                </Typography>
            </Box>
        );
    }

    return (
        <Typography className={className} variant="caption" color="text.secondary" sx={{ display: 'block', ...sx }}>
            {config.text}
            {config.url && <> {link(config.name)}</>}
        </Typography>
    );
}

export default AttributionBadge;
