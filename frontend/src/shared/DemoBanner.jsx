import { useState, useEffect } from 'react';
import { Box, Typography } from '@mui/material';
import { LockOutlined, EditOutlined } from '@mui/icons-material';
import { useAuth } from '@/contexts/AuthContext';
import { isPublicDemo } from '@/utils/demoMode';

const DemoBanner = () => {
    const { isAuthenticated, user } = useAuth();
    const [minutesLeft, setMinutesLeft] = useState(null);

    // Tick the countdown while a write window is open.
    useEffect(() => {
        if (!isAuthenticated || !user?.expiresAt) {
            setMinutesLeft(null);
            return undefined;
        }

        const update = () => {
            const remainingMs = new Date(user.expiresAt).getTime() - Date.now();
            setMinutesLeft(Math.max(0, Math.ceil(remainingMs / 60000)));
        };

        update();
        const interval = setInterval(update, 30000);
        return () => clearInterval(interval);
    }, [isAuthenticated, user?.expiresAt]);

    if (!isPublicDemo()) {
        return null;
    }

    const { icon, text, bg, fg } = isAuthenticated
        ? {
              icon: <EditOutlined fontSize="small" />,
              text: minutesLeft !== null
                  ? `Demo — write access enabled (${minutesLeft} min left).`
                  : 'Demo — write access enabled.',
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
