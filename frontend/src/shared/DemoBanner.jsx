import { Box, Typography } from '@mui/material';
import { LockOutlined, EditOutlined } from '@mui/icons-material';
import { useDemoAdmin } from '@/contexts/DemoAdminContext';

const isDemoMode = () => import.meta.env.VITE_DEMO_MODE === 'true';

const DemoBanner = () => {
    const { isAdminMode } = useDemoAdmin();

    if (!isDemoMode()) {
        return null;
    }

    const { icon, text, bg, fg } = isAdminMode
        ? {
              icon: <EditOutlined fontSize="small" />,
              text: 'Demo — write access enabled (admin mode).',
              bg: 'success.main',
              fg: 'success.contrastText',
          }
        : {
              icon: <LockOutlined fontSize="small" />,
              text: 'Read-Only Demo — browse freely; creating, editing, and deleting are disabled.',
              bg: 'warning.main',
              fg: 'warning.contrastText',
          };

    return (
        <Box
            role="status"
            sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 1,
                px: 2,
                py: 0.75,
                backgroundColor: bg,
                color: fg,
                textAlign: 'center',
            }}
        >
            {icon}
            <Typography variant="body2" sx={{ fontWeight: 600 }}>
                {text}
            </Typography>
        </Box>
    );
};

export default DemoBanner;
