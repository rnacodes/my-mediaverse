import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Box, CircularProgress } from '@mui/material';

/**
 * ConditionalProtectedRoute Component
 *
 * Conditionally protects routes based on environment mode.
 * - In production (VITE_DEMO_MODE=false): Requires authentication
 *   - Shows a loading spinner while checking auth status
 *   - Redirects to /login if the user is not authenticated, preserving the
 *     intended destination for redirect after login
 * - In demo mode (VITE_DEMO_MODE=true): Publicly accessible, no auth required
 *
 * This allows the same codebase to be deployed to:
 * - Production environment: Private site requiring login
 * - Demo environment: Public site for showcasing
 *
 * Environment variables are set in Render Environment Groups:
 * - Production: VITE_DEMO_MODE=false
 * - Demo: VITE_DEMO_MODE=true
 *
 * Usage:
 *   <ConditionalProtectedRoute>
 *     <YourComponent />
 *   </ConditionalProtectedRoute>
 */
const ConditionalProtectedRoute = ({ children }) => {
  const { isAuthenticated, loading } = useAuth();
  const location = useLocation();

  // Check if we're in demo mode via environment variable
  const isDemoMode = import.meta.env.VITE_DEMO_MODE === 'true';

  // If demo mode, don't require auth - just render children directly
  if (isDemoMode) {
    return children;
  }

  // Show loading spinner while checking authentication status
  if (loading) {
    return (
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          minHeight: '100vh'
        }}
      >
        <CircularProgress />
      </Box>
    );
  }

  // Redirect to login if not authenticated
  // Save current location so we can redirect back after login
  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  // User is authenticated, render the protected content
  return children;
};

export default ConditionalProtectedRoute;
