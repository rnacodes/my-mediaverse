import { useState, useEffect } from 'react';
import {
    Container,
    Paper,
    Typography,
    Button,
    Box,
    Alert,
    CircularProgress,
    TextField,
    Accordion,
    AccordionSummary,
    AccordionDetails,
    List,
    ListItem,
    ListItemIcon,
    ListItemText,
} from '@mui/material';
import {
    LockOpen as LockOpenIcon,
    Lock as LockIcon,
    Timer as TimerIcon,
    ExpandMore as ExpandMoreIcon,
    PhoneAndroid as PhoneIcon,
    QrCode as QrCodeIcon,
    Numbers as NumbersIcon,
    Schedule as ScheduleIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { unlockDemo, lockDemo } from '@/api/demoService';

/**
 * The demo site's sign-in page. A valid TOTP code opens a 20-minute write window:
 * the API sets the write-window cookie and returns an access token, which becomes
 * the visitor's identity for the rest of the window.
 */
const DemoUnlockPage = () => {
    const navigate = useNavigate();
    const { user, isAuthenticated, applyDemoUnlock, endSession } = useAuth();
    const [totpCode, setTotpCode] = useState('');
    const [submitting, setSubmitting] = useState(false);
    const [feedback, setFeedback] = useState(null);
    const [minutesLeft, setMinutesLeft] = useState(null);

    // Keep the countdown fresh while a write window is open.
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

    const handleUnlock = async () => {
        if (totpCode.length !== 6 || submitting) return;

        setSubmitting(true);
        setFeedback(null);
        try {
            const session = await unlockDemo(totpCode);
            applyDemoUnlock(session);
            setFeedback({
                severity: 'success',
                message: `Write access unlocked for ${session.expiresInMinutes} minutes.`,
            });
        } catch (error) {
            const status = error.response?.status;
            setFeedback({
                severity: 'error',
                message: status === 401
                    ? 'Invalid code. Codes rotate every 30 seconds — try the current one.'
                    : status === 429
                        ? 'Too many attempts. Wait a minute, then try again.'
                        : 'Could not reach the unlock endpoint. Please try again.',
            });
        } finally {
            setSubmitting(false);
            setTotpCode('');
        }
    };

    const handleLock = async () => {
        setFeedback(null);
        try {
            await lockDemo();
        } catch {
            // The cookie may already be gone; ending the local session is what matters.
        }
        endSession();
        setFeedback({ severity: 'info', message: 'Write access revoked. The demo is read-only again.' });
    };

    const handleTotpChange = (e) => {
        setTotpCode(e.target.value.replace(/\D/g, '').slice(0, 6));
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter' && totpCode.length === 6) {
            handleUnlock();
        }
    };

    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Typography variant="h3" gutterBottom sx={{ mb: 1, fontWeight: 'bold' }}>
                Demo Write Access
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 3 }}>
                This demo is read-only for visitors. With a Google Authenticator code you can open a
                20-minute window to create, edit, and delete data.
            </Typography>

            {feedback && (
                <Alert severity={feedback.severity} onClose={() => setFeedback(null)} sx={{ mb: 3 }}>
                    {feedback.message}
                </Alert>
            )}

            {isAuthenticated ? (
                <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2 }}>
                        <LockOpenIcon color="success" sx={{ fontSize: 32 }} />
                        <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
                            Write access is enabled
                        </Typography>
                    </Box>
                    {minutesLeft !== null && (
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                            <TimerIcon fontSize="small" />
                            <Typography variant="body1">
                                {minutesLeft} {minutesLeft === 1 ? 'minute' : 'minutes'} left in this window.
                            </Typography>
                        </Box>
                    )}
                    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                        <Button variant="contained" onClick={() => navigate('/')} sx={{ color: '#fcfafa' }}>
                            Start Browsing
                        </Button>
                        <Button
                            variant="contained"
                            startIcon={<LockIcon />}
                            onClick={handleLock}
                            sx={{ color: '#fcfafa' }}
                        >
                            Revoke Write Access
                        </Button>
                    </Box>
                </Paper>
            ) : (
                <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 2 }}>
                        Unlock Write Access
                    </Typography>

                    <Alert severity="info" sx={{ mb: 3 }}>
                        Open Google Authenticator, find <strong>&quot;MyMediaVerse Demo,&quot;</strong> and
                        enter the 6-digit code below.
                    </Alert>

                    <Box sx={{ display: 'flex', gap: 2, alignItems: 'flex-start', flexWrap: 'wrap' }}>
                        <TextField
                            label="TOTP Code"
                            value={totpCode}
                            onChange={handleTotpChange}
                            onKeyDown={handleKeyDown}
                            placeholder="123456"
                            variant="outlined"
                            disabled={submitting}
                            inputProps={{
                                maxLength: 6,
                                pattern: '[0-9]*',
                                inputMode: 'numeric',
                                style: { letterSpacing: '0.5em', fontSize: '1.2em', textAlign: 'center' },
                            }}
                            InputLabelProps={{
                                sx: { color: 'white' }
                            }}
                            sx={{
                                width: 180,
                                '& .MuiInputLabel-root.Mui-focused': {
                                    color: 'white'
                                }
                            }}
                        />
                        <Button
                            variant="contained"
                            startIcon={submitting ? <CircularProgress size={18} color="inherit" /> : <LockOpenIcon />}
                            onClick={handleUnlock}
                            disabled={totpCode.length !== 6 || submitting}
                            sx={{
                                bgcolor: '#4caf50',
                                color: 'white',
                                height: 56,
                                '&:hover': { bgcolor: '#388e3c' },
                            }}
                        >
                            Unlock
                        </Button>
                    </Box>
                </Paper>
            )}

            {/* Quick Reference Notes */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 2 }}>
                    Quick Reference
                </Typography>

                <List disablePadding>
                    <ListItem>
                        <ListItemIcon>
                            <ScheduleIcon sx={{ color: '#fcfafa' }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="TOTP code changes every 30 seconds"
                            secondary="If a code doesn't work, wait for the next one. Codes have a small time window tolerance (±1 step)."
                        />
                    </ListItem>
                    <ListItem>
                        <ListItemIcon>
                            <TimerIcon sx={{ color: '#fcfafa' }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Write access lasts 20 minutes"
                            secondary="The access token has a hard expiration time — there is no automatic renewal. To get write access in another window, a new code must be entered."
                        />
                    </ListItem>
                    <ListItem>
                        <ListItemIcon>
                            <LockIcon sx={{ color: '#fcfafa' }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Refreshing the page ends the session"
                            secondary="The access token lives in memory only. After a reload, unlock again with a fresh code."
                        />
                    </ListItem>
                </List>
            </Paper>

            {/* Setup Instructions */}
            <Accordion>
                <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                        First-Time Setup Instructions
                    </Typography>
                </AccordionSummary>
                <AccordionDetails>
                    <Typography variant="body1" sx={{ mb: 2 }}>
                        To unlock write access, you need the Google Authenticator app configured with the
                        demo secret.
                    </Typography>

                    <List>
                        <ListItem>
                            <ListItemIcon>
                                <PhoneIcon sx={{ color: '#fcfafa' }} />
                            </ListItemIcon>
                            <ListItemText
                                primary="Step 1: Install the Google Authenticator app"
                                secondary="Download Google Authenticator on your phone from the App Store or Google Play"
                            />
                        </ListItem>
                        <ListItem>
                            <ListItemIcon>
                                <QrCodeIcon sx={{ color: '#fcfafa' }} />
                            </ListItemIcon>
                            <ListItemText
                                primary='Step 2: Add the demo account'
                                secondary='Manually enter the secret key. The account should appear as "MyMediaVerse Demo".'
                            />
                        </ListItem>
                        <ListItem>
                            <ListItemIcon>
                                <NumbersIcon sx={{ color: '#fcfafa' }} />
                            </ListItemIcon>
                            <ListItemText
                                primary="Step 3: Enter the code"
                                secondary="Enter the 6-digit code shown in Google Authenticator in the Unlock section above to enable write access for 20 minutes."
                            />
                        </ListItem>
                    </List>
                </AccordionDetails>
            </Accordion>
        </Container>
    );
};

export default DemoUnlockPage;
