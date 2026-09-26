import React from 'react';
import { Box, Container, Typography, Link } from '@mui/material';
import { COLORS, SPACING } from './DesignSystem';
import AttributionBadge from './AttributionBadge';

const Footer = () => {
  const currentYear = new Date().getFullYear();

  return (
    <Box
      component="footer"
      sx={{
        backgroundColor: COLORS.background.paper,
        borderTop: `2px solid ${COLORS.primary.main}`,
        marginTop: 'auto',
        py: 3,
        mt: 8
      }}
    >
      <Container maxWidth="lg">
        <Box
          sx={{
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: SPACING.sm
          }}
        >
          {/* Copyright */}
          <Typography variant="body2" sx={{ color: COLORS.text.primary }}>
            Copyright © Rashida Asante-Eccleston {currentYear}
          </Typography>

          {/* Disclaimer */}
          <Typography 
            variant="body2" 
            sx={{ 
              color: COLORS.text.secondary,
              textAlign: 'center',
              fontSize: '0.95rem'
            }}
          >
           My MediaVerse is not affiliated with any of the brands or websites used in the application.
          </Typography>

          {/* Data source credit */}
          <Typography
            variant="body2"
            sx={{
              color: COLORS.text.secondary,
              textAlign: 'center',
              fontSize: '0.95rem'
            }}
          >
            Book information and covers from{' '}
            <Link
              href="https://books.google.com"
              target="_blank"
              rel="noopener noreferrer"
              sx={{ color: COLORS.text.secondary, textDecoration: 'underline' }}
            >
              Google Books
            </Link>
            {' '}and{' '}
            <Link
              href="https://openlibrary.org"
              target="_blank"
              rel="noopener noreferrer"
              sx={{ color: COLORS.text.secondary, textDecoration: 'underline' }}
            >
              Open Library
            </Link>
            .
          </Typography>

          {/* Movie and TV data providers: TMDB's notice is a term of its API; Trakt asks for its mark. */}
          <Box
            sx={{
              display: 'flex',
              gap: SPACING.lg,
              flexWrap: 'wrap',
              justifyContent: 'center',
              alignItems: 'center',
              textAlign: 'center'
            }}
          >
            <AttributionBadge provider="tmdb" />
            <AttributionBadge provider="trakt" sx={{ display: 'flex', alignItems: 'center', gap: 1, '& p': { fontSize: '0.75rem', color: COLORS.text.secondary } }} />
          </Box>

          {/* Links */}
          <Box
            sx={{
              display: 'flex',
              gap: SPACING.md,
              flexWrap: 'wrap',
              justifyContent: 'center',
              alignItems: 'center'
            }}
          >
            <Link
              href="https://github.com/rnacodes/MyMediaVerse"
              target="_blank"
              rel="noopener noreferrer"
              sx={{
                color: COLORS.text.primary,
                textDecoration: 'underline',
                fontSize: '1rem',
                fontWeight: 500,
                transition: 'color 0.3s ease',
                '&:hover': {
                  color: COLORS.primary.light
                }
              }}
            >
              View project on GitHub
            </Link>
            <Typography variant="body2" sx={{ color: COLORS.text.hint }}>
              •
            </Typography>
            <Link
              href="https://raeccleston.com"
              target="_blank"
              rel="noopener noreferrer"
              sx={{
                color: COLORS.text.primary,
                textDecoration: 'underline',
                fontSize: '1rem',
                fontWeight: 500,
                transition: 'color 0.3s ease',
                '&:hover': {
                  color: COLORS.primary.light
                }
              }}
            >
              raeccleston.com
            </Link>
          </Box>
        </Box>
      </Container>
    </Box>
  );
};

export default Footer;


