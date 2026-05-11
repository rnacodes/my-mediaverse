import React, { useEffect, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Box,
  Typography,
  CircularProgress,
  Alert,
  Button,
  Container,
  Paper
} from '@mui/material';
import { CheckCircle, Error, YouTube } from '@mui/icons-material';

function YouTubeCallback() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [status, setStatus] = useState('loading'); // 'loading', 'success', 'error'
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  useEffect(() => {
    const handleCallback = async () => {
      try {
        // Get the authorization code from URL parameters
        const code = searchParams.get('code');
        const error = searchParams.get('error');
        const state = searchParams.get('state');

        if (error) {
          setStatus('error');
          setError(`OAuth Error: ${error}`);
          setMessage('There was an error during YouTube authentication.');
          return;
        }

        if (!code) {
          setStatus('error');
          setError('No authorization code received');
          setMessage('The authorization code was not found in the callback URL.');
          return;
        }

        // Here you would typically send the code to your backend
        // to exchange it for access and refresh tokens
        console.log('Authorization Code:', code);
        console.log('State:', state);

        // Simulate API call to your backend
        // const response = await fetch('/api/youtube/oauth/callback', {
        //   method: 'POST',
        //   headers: {
        //     'Content-Type': 'application/json',
        //   },
        //   body: JSON.stringify({ code, state })
        // });

        // For now, just show success
        setStatus('success');
        setMessage('YouTube authentication successful! You can now import videos from YouTube.');

        // Redirect to import page after 3 seconds
        setTimeout(() => {
          navigate('/import-media');
        }, 3000);

      } catch (err) {
        console.error('Callback error:', err);
        setStatus('error');
        setError(err.message || 'An unexpected error occurred');
        setMessage('Failed to complete YouTube authentication.');
      }
    };

    handleCallback();
  }, [searchParams, navigate]);

  const handleRetry = () => {
    navigate('/import-media');
  };

  const handleGoHome = () => {
    navigate('/');
  };

  return (
    <Container maxWidth="sm" sx={{ mt: 8 }}>
      <Paper
        elevation={3}
        sx={{
          p: 4,
          textAlign: 'center',
          background: 'linear-gradient(135deg, rgba(54, 39, 89, 0.1) 0%, rgba(71, 67, 80, 0.1) 100%)',
          border: '1px solid rgba(255, 255, 255, 0.1)'
        }}
      >
        <Box sx={{ mb: 3 }}>
          <YouTube 
            sx={{ 
              fontSize: 64, 
              color: '#ff0000',
              mb: 2
            }} 
          />
          <Typography variant="h4" component="h1" sx={{ 
            fontWeight: 'bold',
            color: '#ffffff',
            mb: 1
          }}>
            YouTube Authentication
          </Typography>
          <Typography variant="body1" sx={{ color: '#b0b0b0' }}>
            Processing your authentication...
          </Typography>
        </Box>

        {status === 'loading' && (
          <Box sx={{ mt: 3 }}>
            <CircularProgress 
              size={40} 
              sx={{ color: '#ff0000', mb: 2 }} 
            />
            <Typography variant="body2" sx={{ color: '#b0b0b0' }}>
              Please wait while we complete your YouTube authentication...
            </Typography>
          </Box>
        )}

        {status === 'success' && (
          <Box sx={{ mt: 3 }}>
            <CheckCircle 
              sx={{ 
                fontSize: 48, 
                color: '#4caf50',
                mb: 2
              }} 
            />
            <Alert 
              severity="success" 
              sx={{ 
                mb: 3,
                backgroundColor: 'rgba(76, 175, 80, 0.1)',
                border: '1px solid rgba(76, 175, 80, 0.3)',
                color: '#ffffff'
              }}
            >
              {message}
            </Alert>
            <Typography variant="body2" sx={{ color: '#b0b0b0', mb: 3 }}>
              Redirecting to import page in a few seconds...
            </Typography>
            <Button
              variant="contained"
              onClick={handleGoHome}
              sx={{
                backgroundColor: '#ff0000',
                '&:hover': {
                  backgroundColor: '#cc0000'
                },
                mr: 2
              }}
            >
              Go to Home
            </Button>
            <Button
              variant="outlined"
              onClick={() => navigate('/import-media')}
              sx={{
                borderColor: '#ff0000',
                color: '#ff0000',
                '&:hover': {
                  borderColor: '#cc0000',
                  backgroundColor: 'rgba(255, 0, 0, 0.1)'
                }
              }}
            >
              Go to Import
            </Button>
          </Box>
        )}

        {status === 'error' && (
          <Box sx={{ mt: 3 }}>
            <Error 
              sx={{ 
                fontSize: 48, 
                color: '#f44336',
                mb: 2
              }} 
            />
            <Alert 
              severity="error" 
              sx={{ 
                mb: 3,
                backgroundColor: 'rgba(244, 67, 54, 0.1)',
                border: '1px solid rgba(244, 67, 54, 0.3)',
                color: '#ffffff'
              }}
            >
              {message}
            </Alert>
            {error && (
              <Typography 
                variant="body2" 
                sx={{ 
                  color: '#ffcdd2',
                  mb: 3,
                  fontFamily: 'monospace',
                  fontSize: '0.8rem',
                  backgroundColor: 'rgba(0, 0, 0, 0.3)',
                  p: 1,
                  borderRadius: 1
                }}
              >
                {error}
              </Typography>
            )}
            <Button
              variant="contained"
              onClick={handleRetry}
              sx={{
                backgroundColor: '#ff0000',
                '&:hover': {
                  backgroundColor: '#cc0000'
                },
                mr: 2
              }}
            >
              Try Again
            </Button>
            <Button
              variant="outlined"
              onClick={handleGoHome}
              sx={{
                borderColor: '#ff0000',
                color: '#ff0000',
                '&:hover': {
                  borderColor: '#cc0000',
                  backgroundColor: 'rgba(255, 0, 0, 0.1)'
                }
              }}
            >
              Go Home
            </Button>
          </Box>
        )}

        {/* Debug information (only in development) */}
        {import.meta.env.DEV && (
          <Box sx={{ mt: 4, p: 2, backgroundColor: 'rgba(0, 0, 0, 0.3)', borderRadius: 1 }}>
            <Typography variant="caption" sx={{ color: '#b0b0b0', display: 'block', mb: 1 }}>
              Debug Information:
            </Typography>
            <Typography variant="caption" sx={{ color: '#b0b0b0', fontFamily: 'monospace', fontSize: '0.7rem' }}>
              Code: {searchParams.get('code') ? 'Present' : 'Missing'}<br/>
              State: {searchParams.get('state') || 'None'}<br/>
              Error: {searchParams.get('error') || 'None'}<br/>
              Full URL: {window.location.href}
            </Typography>
          </Box>
        )}
      </Paper>
    </Container>
  );
}

export default YouTubeCallback;
