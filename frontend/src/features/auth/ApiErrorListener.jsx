import { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { Alert, Snackbar } from '@mui/material';
import { isDemoMode } from '@/utils/demoMode';

/**
 * Turns the api client's error events into user-visible outcomes.
 */
const ApiErrorListener = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [notice, setNotice] = useState(null);

    useEffect(() => {
        const handleSessionExpired = () => {
            const destination = isDemoMode() ? '/demo-unlock' : '/login';

            setNotice({
                severity: 'warning',
                message: isDemoMode()
                    ? 'Demo write access has expired. Unlock again to continue making changes.'
                    : 'Your session has expired. Please sign in again.',
            });

            navigate(destination, { state: { from: location }, replace: true });
        };

        const handleForbidden = (event) => {
            setNotice({
                severity: 'error',
                message: event.detail?.message || 'You do not have permission to perform this action.',
            });
        };

        window.addEventListener('sessionExpired', handleSessionExpired);
        window.addEventListener('apiForbidden', handleForbidden);
        return () => {
            window.removeEventListener('sessionExpired', handleSessionExpired);
            window.removeEventListener('apiForbidden', handleForbidden);
        };
    }, [navigate, location]);

    return (
        <Snackbar
            open={notice !== null}
            autoHideDuration={6000}
            onClose={() => setNotice(null)}
            anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
        >
            {notice ? (
                <Alert severity={notice.severity} onClose={() => setNotice(null)}>
                    {notice.message}
                </Alert>
            ) : null}
        </Snackbar>
    );
};

export default ApiErrorListener;
