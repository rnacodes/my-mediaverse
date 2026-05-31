import React from 'react';
import { Box, Grid, Typography } from '@mui/material';
import { Book, Movie, Tv, Article, LibraryMusic, Podcasts, SportsEsports, YouTube, Language, MenuBook, AutoAwesome, NoteAlt, LocalLibrary, FormatQuote } from '@mui/icons-material';

// Alphabetized primary media types. `supported: false` entries render dimmed with a "Coming Soon" caption.
const mainMediaIcons = [
    { name: 'Articles', icon: <Article sx={{ fontSize: 40 }} />, mediaType: 'Article', supported: true },
    { name: 'Books', icon: <Book sx={{ fontSize: 40 }} />, mediaType: 'Book', supported: true },
    { name: 'Courses', icon: <LocalLibrary sx={{ fontSize: 40 }} />, mediaType: 'Course', supported: false },
    { name: 'Documents', icon: <NoteAlt sx={{ fontSize: 40 }} />, mediaType: 'Document', supported: false },
    { name: 'Highlights', icon: <FormatQuote sx={{ fontSize: 40 }} />, mediaType: 'Highlight', supported: true },
    { name: 'Movies', icon: <Movie sx={{ fontSize: 40 }} />, mediaType: 'Movie', supported: true },
    { name: 'Music', icon: <LibraryMusic sx={{ fontSize: 40 }} />, mediaType: 'Music', supported: false },
    { name: 'Online Videos', icon: <YouTube sx={{ fontSize: 40 }} />, mediaType: 'Video,Channel,Playlist', supported: true },
    { name: 'Podcasts', icon: <Podcasts sx={{ fontSize: 40 }} />, mediaType: 'Podcast', supported: true },
    { name: 'TV Shows', icon: <Tv sx={{ fontSize: 40 }} />, mediaType: 'TVShow', supported: true },
    { name: 'Video Games', icon: <SportsEsports sx={{ fontSize: 40 }} />, mediaType: 'VideoGame', supported: false },
    { name: 'Websites', icon: <Language sx={{ fontSize: 40 }} />, mediaType: 'Website', supported: true },
];

const specialMediaIcons = [
    { name: 'Online Notebook', icon: <MenuBook sx={{ fontSize: 40 }} />, key: 'zk', mediaType: 'Note', supported: true },
    { name: 'Panorama', icon: <AutoAwesome sx={{ fontSize: 40 }} />, key: 'panorama', supported: false, caption: 'For everything else - coming soon!' },
];

// The grid of media-type entry points: an alphabetized icon row plus two larger
// "special" tiles below it. Each supported tile navigates to a filtered search.
const MediaTypeNav = ({ navigate }) => (
  <>
    {/* Alphabetized Icons */}
    <Box sx={{ display: 'flex', justifyContent: 'center', width: '100%' }}>
      <Grid container spacing={{ xs: 1, sm: 2 }} justifyContent="center" sx={{ mt: { xs: 2, sm: 3, md: 4 }, mb: 2, maxWidth: '900px' }}>
        {mainMediaIcons.map((item) => (
            <Grid item xs={4} sm={3} md={2} key={`media-${item.name}`} sx={{ display: 'flex', justifyContent: 'center' }}>
                <Box
                    onClick={() => {
                        if (item.supported) {
                            navigate(`/search?mediaType=${item.mediaType}`);
                        }
                    }}
                    sx={{
                        display: 'flex',
                        flexDirection: 'column',
                        alignItems: 'center',
                        justifyContent: 'center',
                        cursor: item.supported ? 'pointer' : 'default',
                        color: item.supported ? 'text.secondary' : 'text.disabled',
                        minHeight: { xs: '60px', sm: '70px' },
                        minWidth: { xs: '60px', sm: '70px' },
                        p: { xs: 1, sm: 1.5 },
                        borderRadius: '12px',
                        transition: 'all 0.2s ease',
                        opacity: item.supported ? 1 : 0.5,
                        '&:hover': item.supported ? {
                            color: 'text.primary',
                            transform: 'scale(1.05)',
                            backgroundColor: 'rgba(255, 255, 255, 0.05)'
                        } : {},
                        '&:active': item.supported ? {
                            transform: 'scale(0.98)'
                        } : {}
                    }}
                >
                    {React.cloneElement(item.icon, {
                        sx: { fontSize: { xs: 32, sm: 40 } }
                    })}
                    <Typography
                        variant="caption"
                        sx={{
                            mt: 0.5,
                            fontSize: { xs: '0.65rem', sm: '0.75rem' },
                            lineHeight: 1.2,
                            textAlign: 'center'
                        }}
                    >
                        {item.name}
                    </Typography>
                    {!item.supported && (
                        <Typography
                            variant="caption"
                            sx={{
                                fontSize: { xs: '0.55rem', sm: '0.65rem' },
                                color: 'text.disabled',
                                fontStyle: 'italic',
                                textAlign: 'center'
                            }}
                        >
                            Coming Soon
                        </Typography>
                    )}
                </Box>
            </Grid>
        ))}
      </Grid>
    </Box>
    {/* Special, larger icons */}
    <Box sx={{ display: 'flex', justifyContent: 'center', width: '100%' }}>
      <Grid container spacing={{ xs: 1, sm: 2 }} justifyContent="center" sx={{ mb: { xs: 2, sm: 3, md: 4 }, maxWidth: '900px' }}>
          {specialMediaIcons.map((item) => (
              <Grid item xs={6} sm={4} md={3} key={item.key} sx={{ display: 'flex', justifyContent: 'center' }}>
                  <Box
                      onClick={() => {
                          if (item.supported && item.mediaType) {
                              navigate(`/search?mediaType=${item.mediaType}`);
                          }
                      }}
                      sx={{
                          display: 'flex',
                          flexDirection: 'column',
                          alignItems: 'center',
                          justifyContent: 'center',
                          cursor: item.supported ? 'pointer' : 'default',
                          color: item.supported ? 'text.secondary' : 'text.disabled',
                          minHeight: { xs: '70px', sm: '80px' },
                          p: { xs: 1.5, sm: 2 },
                          borderRadius: '12px',
                          transition: 'all 0.2s ease',
                          opacity: item.supported ? 1 : 0.5,
                          '&:hover': item.supported ? {
                              color: 'text.primary',
                              backgroundColor: 'rgba(255, 255, 255, 0.05)'
                          } : {},
                          '&:active': item.supported ? {
                              transform: 'scale(0.98)'
                          } : {}
                      }}>
                      {React.cloneElement(item.icon, { sx: { fontSize: { xs: 40, sm: 50 } } })}
                      <Typography
                          variant="body2"
                          sx={{
                              mt: 0.5,
                              fontSize: { xs: '0.8rem', sm: '0.875rem' },
                              textAlign: 'center'
                          }}
                      >
                          {item.name}
                      </Typography>
                      {item.caption && (
                          <Typography
                              variant="caption"
                              sx={{
                                  fontSize: { xs: '0.55rem', sm: '0.65rem' },
                                  color: 'text.disabled',
                                  fontStyle: 'italic',
                                  textAlign: 'center'
                              }}
                          >
                              {item.caption}
                          </Typography>
                      )}
                  </Box>
              </Grid>
          ))}
      </Grid>
    </Box>
  </>
);

export default MediaTypeNav;
