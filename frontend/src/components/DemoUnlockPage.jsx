import React, { useState, useEffect } from 'react';
import {
    Container,
    Paper,
    Typography,
    Button,
    Box,
    Alert,
    CircularProgress,
    Card,
    CardContent,
    Grid,
    Chip,
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
    Refresh as RefreshIcon,
    LockOpen as LockOpenIcon,
    Lock as LockIcon,
    Timer as TimerIcon,
    Key as KeyIcon,
    ExpandMore as ExpandMoreIcon,
    PhoneAndroid as PhoneIcon,
    QrCode as QrCodeIcon,
    Numbers as NumbersIcon,
    OpenInNew as OpenInNewIcon,
    Info as InfoIcon,
    Schedule as ScheduleIcon,
    Language as LanguageIcon,
    Upload as UploadIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

const DEMO_API_BASE = 'https://demo-api.mymediaverseuniverse.com/api/demo';
const DEMO_SITE_URL = 'https://demo.mymediaverseuniverse.com';

const DemoUnlockPage = () => {
    const navigate = useNavigate();
    const [totpCode, setTotpCode] = useState('');
    const [unlockClicked, setUnlockClicked] = useState(false);
    const [statusData, setStatusData] = useState(null);
    const [statusLoading, setStatusLoading] = useState(true);
    const [statusError, setStatusError] = useState(null);

    useEffect(() => {
        fetchStatus();
    }, []);

    const fetchStatus = async () => {
        setStatusLoading(true);
        setStatusError(null);
        try {
            const response = await fetch(`${DEMO_API_BASE}/status`, {
                credentials: 'include',
            });
            if (response.ok) {
                const data = await response.json();
                setStatusData(data);
            } else {
                setStatusError('Failed to fetch status from demo API.');
            }
        } catch (error) {
            setStatusError(
                'Could not connect to the demo API. Use "Check Status Directly" to view in a new tab.'
            );
        } finally {
            setStatusLoading(false);
        }
    };

    const handleUnlock = () => {
        if (totpCode.length !== 6) return;
        window.open(`${DEMO_API_BASE}/unlock?code=${totpCode}`, '_blank');
        setUnlockClicked(true);
        setTotpCode('');
    };

    const handleLock = () => {
        window.open(`${DEMO_API_BASE}/lock`, '_blank');
        setUnlockClicked(false);
    };

    const handleCheckStatus = () => {
        window.open(`${DEMO_API_BASE}/status`, '_blank');
    };

    const handleGoToDemoSite = () => {
        window.open(DEMO_SITE_URL, '_blank');
    };

    const handleTotpChange = (e) => {
        const value = e.target.value.replace(/\D/g, '').slice(0, 6);
        setTotpCode(value);
        setUnlockClicked(false);
    };

    const handleKeyDown = (e) => {
        if (e.key === 'Enter' && totpCode.length === 6) {
            handleUnlock();
        }
    };

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Typography variant="h3" gutterBottom sx={{ mb: 1, fontWeight: 'bold' }}>
                Demo Mode Administration
            </Typography>
            <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
                Manage write access for the demo site at{' '}
                <a href={DEMO_SITE_URL} target="_blank" rel="noopener noreferrer">
                    {DEMO_SITE_URL}
                </a>
            </Typography>

            {/* Status Section */}
            <Paper elevation={3} sx={{ p: 3, mb: 3, bgcolor: '#1a1a2e', color: 'white' }}>
                <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2, flexWrap: 'wrap', gap: 1 }}>
                    <Typography variant="h5" sx={{ fontWeight: 'bold', color: 'white' }}>
                        Demo Write Access Status
                    </Typography>
                    <Box sx={{ display: 'flex', gap: 1 }}>
                        <Button
                            variant="outlined"
                            startIcon={<OpenInNewIcon />}
                            onClick={handleCheckStatus}
                            size="small"
                            sx={{
                                color: 'white',
                                borderColor: 'rgba(255,255,255,0.5)',
                                '&:hover': { borderColor: 'white', bgcolor: 'rgba(255,255,255,0.1)' },
                            }}
                        >
                            Check Directly
                        </Button>
                        <Button
                            variant="contained"
                            startIcon={statusLoading ? <CircularProgress size={18} color="inherit" /> : <RefreshIcon />}
                            onClick={fetchStatus}
                            disabled={statusLoading}
                            size="small"
                            color="primary"
                            sx={{ color: '#fcfafa' }}
                        >
                            Refresh
                        </Button>
                    </Box>
                </Box>

                {statusLoading && !statusData && (
                    <Box sx={{ display: 'flex', justifyContent: 'center', my: 3 }}>
                        <CircularProgress sx={{ color: 'white' }} />
                    </Box>
                )}

                {statusError && (
                    <Alert severity="warning" sx={{ mb: 2 }}>
                        {statusError}
                    </Alert>
                )}

                {statusData && (
                    <Grid container spacing={2}>
                        <Grid item xs={12} md={6}>
                            <Card sx={{
                                bgcolor: statusData.isDemoEnvironment ? '#0a2647' : '#2d2d2d',
                                color: 'white',
                                height: '100%',
                            }}>
                                <CardContent>
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                                        <KeyIcon sx={{ fontSize: 40, color: statusData.isDemoEnvironment ? '#64b5f6' : '#9e9e9e' }} />
                                        <Box>
                                            <Typography variant="h6" sx={{ fontWeight: 'bold', color: 'white' }}>
                                                Demo Environment
                                            </Typography>
                                            <Chip
                                                label={statusData.isDemoEnvironment ? 'Active' : 'Not Active'}
                                                sx={{
                                                    bgcolor: statusData.isDemoEnvironment ? '#1565c0' : '#616161',
                                                    color: 'white',
                                                    fontWeight: 'bold',
                                                }}
                                                size="small"
                                            />
                                        </Box>
                                    </Box>
                                </CardContent>
                            </Card>
                        </Grid>
                        <Grid item xs={12} md={6}>
                            <Card sx={{
                                bgcolor: statusData.writeAccessEnabled ? '#1b5e20' : '#bf360c',
                                color: 'white',
                                height: '100%',
                            }}>
                                <CardContent>
                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5 }}>
                                        {statusData.writeAccessEnabled ? (
                                            <LockOpenIcon sx={{ fontSize: 40, color: '#a5d6a7' }} />
                                        ) : (
                                            <LockIcon sx={{ fontSize: 40, color: '#ffcc80' }} />
                                        )}
                                        <Box>
                                            <Typography variant="h6" sx={{ fontWeight: 'bold', color: 'white' }}>
                                                Write Access
                                            </Typography>
                                            <Chip
                                                label={statusData.writeAccessEnabled ? 'Enabled' : 'Disabled (Read-Only)'}
                                                sx={{
                                                    bgcolor: statusData.writeAccessEnabled ? '#2e7d32' : '#e65100',
                                                    color: 'white',
                                                    fontWeight: 'bold',
                                                }}
                                                size="small"
                                            />
                                        </Box>
                                    </Box>
                                    {statusData.writeAccessEnabled && (
                                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 1 }}>
                                            <TimerIcon sx={{ fontSize: 18, color: '#a5d6a7' }} />
                                            <Typography variant="body2" sx={{ color: '#a5d6a7' }}>
                                                Access expires ~20 minutes from unlock
                                            </Typography>
                                        </Box>
                                    )}
                                </CardContent>
                            </Card>
                        </Grid>
                    </Grid>
                )}

                {unlockClicked && (
                    <Alert severity="info" sx={{ mt: 2 }}>
                        Unlock request opened in a new tab. If the TOTP code was valid, write access is now enabled
                        for 20 minutes in that browser session. Click "Refresh" or "Check Directly" to verify.
                    </Alert>
                )}

                <Typography variant="body2" sx={{ mt: 2, color: 'rgba(255,255,255,0.6)', fontStyle: 'italic' }}>
                    Status may not reflect the demo site's cookie state when viewed from a different domain.
                    Use "Check Directly" to open the status endpoint in a new tab for the most accurate result.
                </Typography>
            </Paper>

            {/* Unlock Section */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 2 }}>
                    Unlock Write Access
                </Typography>

                <Alert severity="info" sx={{ mb: 3 }}>
                    Open Google Authenticator on your phone, find <strong>"MyMediaVerse Demo"</strong>, and enter the 6-digit code below.
                    Clicking "Unlock" opens the demo API unlock endpoint in a new tab, setting a 20-minute write access cookie.
                    Then visit the demo site to make changes.
                </Alert>

                <Box sx={{ display: 'flex', gap: 2, alignItems: 'flex-start', flexWrap: 'wrap' }}>
                    <TextField
                        label="TOTP Code"
                        value={totpCode}
                        onChange={handleTotpChange}
                        onKeyDown={handleKeyDown}
                        placeholder="123456"
                        variant="outlined"
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
                        startIcon={<LockOpenIcon />}
                        endIcon={<OpenInNewIcon />}
                        onClick={handleUnlock}
                        disabled={totpCode.length !== 6}
                        sx={{
                            bgcolor: '#4caf50',
                            color: 'white',
                            height: 56,
                            '&:hover': { bgcolor: '#388e3c' },
                        }}
                    >
                        Unlock
                    </Button>
                    <Button
                        variant="contained"
                        color="primary"
                        startIcon={<OpenInNewIcon />}
                        onClick={handleGoToDemoSite}
                        sx={{ height: 56, color: '#fcfafa' }}
                    >
                        Go to Demo Site
                    </Button>
                    <Button
                        variant="contained"
                        startIcon={<UploadIcon />}
                        onClick={() => navigate('/upload-demo-data')}
                        sx={{
                            height: 56,
                            bgcolor: '#7b1fa2',
                            color: 'white',
                            '&:hover': { bgcolor: '#6a1b9a' },
                        }}
                    >
                        Upload Demo Data
                    </Button>
                </Box>
            </Paper>

            {/* Lock Section */}
            <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
                <Typography variant="h5" sx={{ fontWeight: 'bold', mb: 2 }}>
                    Revoke Write Access
                </Typography>

                <Alert severity="warning" sx={{ mb: 3 }}>
                    Click below to immediately revoke write access on the demo site. This opens the lock endpoint
                    in a new tab, clearing the write access cookie. Use this when you're done making changes or
                    want to return the demo site to read-only mode early.
                </Alert>

                <Button
                    variant="contained"
                    startIcon={<LockIcon />}
                    endIcon={<OpenInNewIcon />}
                    onClick={handleLock}
                    sx={{
                        bgcolor: '#f44336',
                        color: 'white',
                        '&:hover': { bgcolor: '#d32f2f' },
                    }}
                >
                    Revoke Write Access
                </Button>
            </Paper>

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
                            secondary="After 20 minutes, write access expires automatically. Enter a new code to continue making changes."
                        />
                    </ListItem>
                    <ListItem>
                        <ListItemIcon>
                            <LanguageIcon sx={{ color: '#fcfafa' }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Write access is cookie-based and browser-specific"
                            secondary="The unlock sets a cookie in the browser where you opened the unlock link. Other browsers or private/incognito windows won't have access."
                        />
                    </ListItem>
                    <ListItem>
                        <ListItemIcon>
                            <InfoIcon sx={{ color: '#fcfafa' }} />
                        </ListItemIcon>
                        <ListItemText
                            primary="Clearing browser cookies will revoke access"
                            secondary="If you clear cookies or close the browser, you'll need to unlock again with a new code."
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
                        To unlock write access, you need a TOTP (Time-based One-Time Password) authenticator app
                        configured with the demo secret.
                    </Typography>

                    <List>
                        <ListItem>
                            <ListItemIcon>
                                <PhoneIcon sx={{ color: '#fcfafa' }} />
                            </ListItemIcon>
                            <ListItemText
                                primary="Step 1: Install an authenticator app"
                                secondary="Download Google Authenticator, Authy, or any TOTP-compatible app on your phone"
                            />
                        </ListItem>
                        <ListItem>
                            <ListItemIcon>
                                <QrCodeIcon sx={{ color: '#fcfafa' }} />
                            </ListItemIcon>
                            <ListItemText
                                primary='Step 2: Add the demo account'
                                secondary='Scan the QR code or manually enter the secret key provided by the administrator. The account should appear as "MyMediaVerse Demo".'
                            />
                        </ListItem>
                        <ListItem>
                            <ListItemIcon>
                                <NumbersIcon sx={{ color: '#fcfafa' }} />
                            </ListItemIcon>
                            <ListItemText
                                primary="Step 3: Enter the code"
                                secondary="Enter the 6-digit code shown in your authenticator app in the Unlock section above to enable write access for 20 minutes."
                            />
                        </ListItem>
                    </List>

                    <Alert severity="warning" sx={{ mt: 2 }}>
                        <strong>Note:</strong> Write access automatically expires after 20 minutes. You'll need to
                        enter a new code to continue making changes.
                    </Alert>
                </AccordionDetails>
            </Accordion>
        </Container>
    );
};

export default DemoUnlockPage;
