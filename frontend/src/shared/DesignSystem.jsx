import { createTheme } from '@mui/material/styles';

// Design System Constants
export const COLORS = {
  // Primary Palette
  primary: {
    main: '#362759', // ultra-violet (updated for better readability)
    light: '#4f3a7a',
    dark: '#1f1a35',
    contrastText: '#fcfafa'
  },
  secondary: {
    main: '#fcfafa', // seasalt
    light: '#ffffff',
    dark: '#e0e0e0',
    contrastText: '#1B1B1B'
  },
  
  // Background Colors
  background: {
    default: '#1B1B1B',
    paper: '#474350', // davys-gray
    elevated: '#2a2a2a',
    overlay: 'rgba(0, 0, 0, 0.7)',
    modal: '#474350' // davys-gray for modals
  },
  
  // Text Colors
  text: {
    primary: '#fcfafa',
    secondary: '#fcfafa', // white for better readability
    disabled: '#666666',
    hint: '#999999'
  },
  
  // Status Colors
  success: '#4caf50',
  warning: '#ff9800',
  error: '#f44336',
  info: '#2196f3',
  
  // New Status Colors
  status: {
    uncharted: '#666666', // dark grey
    activelyExploring: '#4caf50', // green
    completed: '#2196f3', // blue
    abandoned: '#ff9800' // orange
  },
  
  // Media Type Colors
  mediaTypes: {
    podcast: '#e91e63',
    book: '#9c27b0',
    movie: '#3f51b5',
    tv: '#2196f3',
    article: '#4caf50',
    music: '#ff9800',
    game: '#795548',
    video: '#f44336',
    website: '#607d8b',
    document: '#9e9e9e'
  }
};

export const SPACING = {
  xs: '4px',
  sm: '8px',
  md: '16px',
  lg: '24px',
  xl: '32px',
  xxl: '48px'
};

export const BORDER_RADIUS = {
  sm: '4px',
  md: '8px',
  lg: '16px',
  xl: '24px',
  round: '50%'
};

export const SHADOWS = {
  sm: '0 2px 4px rgba(0, 0, 0, 0.1)',
  md: '0 4px 12px rgba(0, 0, 0, 0.15)',
  lg: '0 8px 25px rgba(252, 250, 250, 0.2)',
  xl: '0 16px 40px rgba(0, 0, 0, 0.25)'
};

export const TRANSITIONS = {
  fast: '0.15s ease-in-out',
  normal: '0.3s ease-in-out',
  slow: '0.5s ease-in-out'
};

// Enhanced Theme with Design System
export const theme = createTheme({
  palette: {
    mode: 'dark',
    primary: COLORS.primary,
    secondary: COLORS.secondary,
    background: COLORS.background,
    text: COLORS.text,
    success: { main: COLORS.success },
    warning: { main: COLORS.warning },
    error: { main: COLORS.error },
    info: { main: COLORS.info }
  },
  
  typography: {
    fontFamily: 'Roboto, system-ui, -apple-system, sans-serif',
    h1: {
      fontSize: '3rem',
      fontWeight: 700,
      color: COLORS.text.primary,
      textAlign: 'center',
      marginBottom: SPACING.md,
      letterSpacing: '-0.02em'
    },
    h2: {
      fontSize: '2.5rem',
      fontWeight: 600,
      color: COLORS.text.primary,
      marginBottom: SPACING.lg
    },
    h3: {
      fontSize: '2rem',
      fontWeight: 600,
      color: COLORS.text.primary,
      marginBottom: SPACING.md
    },
    h4: {
      fontSize: '1.5rem',
      fontWeight: 600,
      color: COLORS.text.primary,
      borderBottom: `2px solid ${COLORS.primary.main}`,
      paddingBottom: SPACING.sm,
      marginBottom: SPACING.lg
    },
    h5: {
      fontSize: '1.25rem',
      fontWeight: 500,
      color: COLORS.text.primary
    },
    h6: {
      fontSize: '1.125rem',
      fontWeight: 500,
      color: COLORS.text.primary
    },
    body1: {
      fontSize: '1.2rem', // increased from 1.1rem
      lineHeight: 1.6,
      color: COLORS.text.primary
    },
    body2: {
      fontSize: '1.1rem', // increased from 1rem
      lineHeight: 1.5,
      color: COLORS.text.secondary
    },
    caption: {
      fontSize: '1.1rem', // increased from 1rem
      color: COLORS.text.hint
    }
  },
  
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          borderRadius: BORDER_RADIUS.xl,
          textTransform: 'none',
          fontWeight: 'bold',
          padding: '12px 24px',
          transition: `box-shadow ${TRANSITIONS.normal}`,
          '&:hover': {
            boxShadow: SHADOWS.md
          }
        },
        contained: {
          backgroundColor: COLORS.primary.main,
          '&:hover': {
            backgroundColor: COLORS.primary.dark
          }
        },
        outlined: {
          borderColor: COLORS.primary.main,
          color: COLORS.primary.main,
          '&:hover': {
            backgroundColor: COLORS.primary.main,
            color: COLORS.primary.contrastText
          }
        }
      }
    },
    
    MuiCard: {
      styleOverrides: {
        root: {
          backgroundColor: COLORS.background.paper,
          borderRadius: BORDER_RADIUS.lg,
          transition: `box-shadow ${TRANSITIONS.normal}`,
          '&:hover': {
            boxShadow: SHADOWS.lg
          }
        }
      }
    },
    
    MuiTextField: {
      styleOverrides: {
        root: {
          '& .MuiOutlinedInput-root': {
            borderRadius: BORDER_RADIUS.md,
            backgroundColor: COLORS.background.elevated,
            '&:hover .MuiOutlinedInput-notchedOutline': {
              borderColor: COLORS.primary.light
            },
            '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
              borderColor: COLORS.primary.main
            }
          }
        }
      }
    },
    
    MuiChip: {
      styleOverrides: {
        root: {
          borderRadius: BORDER_RADIUS.xl,
          fontWeight: 500,
          transition: TRANSITIONS.fast
        }
      }
    },
    
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: COLORS.background.paper,
          boxShadow: SHADOWS.sm
        }
      }
    },

    MuiInputLabel: {
      styleOverrides: {
        root: {
          color: COLORS.text.primary,
          '&.Mui-focused': {
            color: COLORS.text.primary
          },
          '&.MuiInputLabel-shrink': {
            color: COLORS.text.primary
          }
        }
      }
    },

    MuiFormLabel: {
      styleOverrides: {
        root: {
          color: COLORS.text.primary,
          '&.Mui-focused': {
            color: COLORS.text.primary
          }
        }
      }
    }
  }
});

// Utility functions for consistent styling
export const getMediaTypeColor = (mediaType) => {
  const type = mediaType?.toLowerCase();
  return COLORS.mediaTypes[type] || COLORS.mediaTypes.document;
};

export const getStatusColor = (status) => {
  switch (status?.toLowerCase()) {
    case 'uncharted':
      return COLORS.status.uncharted;
    case 'activelyexploring':
    case 'actively exploring':
      return COLORS.status.activelyExploring;
    case 'completed':
      return COLORS.status.completed;
    case 'abandoned':
      return COLORS.status.abandoned;
    // Legacy status support
    case 'consumed':
      return COLORS.status.completed;
    case 'inprogress':
      return COLORS.status.activelyExploring;
    case 'notconsumed':
      return COLORS.status.uncharted;
    case 'didnotfinish':
      return COLORS.status.abandoned;
    default:
      return COLORS.text.secondary;
  }
};

// Common styles object for reuse
export const commonStyles = {
  container: {
    maxWidth: 'lg',
    mx: 'auto',
    px: { xs: 2, sm: 3, md: 4 },
    py: 4
  },
  
  card: {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    transition: 'box-shadow 0.3s ease-in-out',
    '&:hover': {
      boxShadow: SHADOWS.lg
    }
  },
  
  mediaCard: {
    position: 'relative',
    overflow: 'hidden',
    cursor: 'pointer'
  },
  
  searchBar: {
    backgroundColor: COLORS.background.paper,
    borderRadius: BORDER_RADIUS.xl,
    padding: SPACING.sm,
    boxShadow: SHADOWS.sm
  },
  
  loadingContainer: {
    display: 'flex',
    justifyContent: 'center',
    alignItems: 'center',
    minHeight: '200px'
  },
  
  errorContainer: {
    textAlign: 'center',
    padding: SPACING.xl,
    color: COLORS.error
  }
};
