import React from 'react';
import { Box, Typography, Button, IconButton, CircularProgress } from '@mui/material';
import { ArrowBack, Edit, Sync } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTheme } from '@mui/material/styles';
import useMediaQuery from '@mui/material/useMediaQuery';
import DemoWriteGuard from '@/features/demo/DemoWriteGuard';
import { DEMO_EDIT_BLOCKED } from '@/features/demo/demoMessages';

// `actions` renders extra page-specific controls beside Reindex and Edit Media.
function MediaHeader({ title, mediaId, onReindex, reindexing, actions = null }) {
  const navigate = useNavigate();
  const theme = useTheme();
  const isTablet = useMediaQuery(theme.breakpoints.down('md'));

  return (
    <Box sx={{
      display: 'flex',
      flexDirection: { xs: 'column', sm: 'row' },
      alignItems: { xs: 'flex-start', sm: 'center' },
      justifyContent: 'space-between',
      gap: { xs: 2, sm: 0 },
      mb: 3
    }}>
      <IconButton onClick={() => navigate(-1)}>
        <ArrowBack />
      </IconButton>

      <Typography
        variant="h3"
        component="h2"
        gutterBottom
        sx={{
          fontWeight: 'bold',
          fontSize: { xs: '1.75rem', sm: '2rem', md: '2.5rem' },
          textAlign: { xs: 'center', md: 'left' }
        }}
      >
        {title || 'Untitled Media'}
      </Typography>

      {/* Side by side from `sm` up; stacked under the title on mobile, Edit first */}
      <Box sx={{
        display: 'flex',
        flexDirection: { xs: 'column', sm: 'row' },
        gap: 1,
        alignItems: { xs: 'stretch', sm: 'center' },
        width: { xs: '100%', sm: 'auto' }
      }}>
        {actions}
        {onReindex && (
          <DemoWriteGuard title={DEMO_EDIT_BLOCKED} style={{ order: 2 }}>
            <Button
              onClick={onReindex}
              startIcon={reindexing ? <CircularProgress size={16} /> : <Sync />}
              variant="contained"
              size={isTablet ? 'medium' : 'large'}
              disabled={reindexing}
              sx={{ order: { xs: 2, sm: 0 }, flex: 1 }}
            >
              {reindexing ? 'Reindexing...' : 'Reindex'}
            </Button>
          </DemoWriteGuard>
        )}
        <DemoWriteGuard title={DEMO_EDIT_BLOCKED} style={{ order: 1 }}>
          <Button
            onClick={() => navigate(`/media/${mediaId}/edit`)}
            startIcon={<Edit />}
            variant="contained"
            size={isTablet ? 'medium' : 'large'}
            sx={{ order: { xs: 1, sm: 0 }, flex: 1 }}
          >
            Edit Media
          </Button>
        </DemoWriteGuard>
      </Box>
    </Box>
  );
}

export default React.memo(MediaHeader);
