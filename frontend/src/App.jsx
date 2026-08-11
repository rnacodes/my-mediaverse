import { lazy, Suspense } from 'react';
import { BrowserRouter as Router, Route, Routes, Link, Navigate, useLocation } from 'react-router-dom';
import { ErrorBoundary } from 'react-error-boundary';
import * as Sentry from '@sentry/react';
import { ThemeProvider, CssBaseline, Typography, Button, Box } from '@mui/material';

// --- Eager imports: providers, route guards, chrome (always rendered) ---
import { AuthProvider } from './contexts/AuthProvider';
import { DemoReadOnlyProvider } from './contexts/DemoReadOnlyProvider';
import { isDemoMode } from '@/utils/demoMode';
import ConditionalProtectedRoute from './features/auth/ConditionalProtectedRoute';
import ApiErrorListener from './features/auth/ApiErrorListener';
import DemoRestrictedRoute from './features/demo/DemoRestrictedRoute';
import DemoReadOnlyDialog from '@/shared/DemoReadOnlyDialog';
import { theme } from '@/shared/DesignSystem';
import ResponsiveNavigation from '@/shared/ResponsiveNavigation';
import DemoBanner from '@/shared/DemoBanner';
import Footer from '@/shared/Footer';
import LoadingSpinner from '@/shared/LoadingSpinner';

// --- Eager route components: bundled into the main chunk for instant nav ---
import HomePage from './features/homepage';
import LoginPage from './features/auth/pages/LoginPage';
import AddMediaForm from './features/media/pages/AddMediaForm';
import CreateMixlistForm from './features/mixlists/pages/CreateMixlistForm';
import MixlistProfilePage from './features/mixlists/pages/MixlistProfilePage';
import MediaProfilePage from './features/media/pages/MediaProfilePage';
import EditMediaForm from './features/media/pages/EditMediaForm';
import EditMixlistForm from './features/mixlists/pages/EditMixlistForm';
import ImportMediaPage from './features/imports/pages/ImportMedia';
import ImportGenresTopicsPage from './features/imports/pages/ImportGenresTopicsPage';
import SearchByTopicOrGenre from './features/search/pages/SearchByTopicOrGenre';
import Search from './features/search/pages/Search';
import UploadMediaPage from './features/media/pages/UploadMediaPage';
import YouTubeCallback from './features/videos/pages/YouTubeCallback';
import ReadwiseSyncPage from './features/imports/pages/ReadwiseSyncPage';
import TraktSyncPage from './features/imports/pages/TraktSyncPage';
import HighlightLinkingPage from './features/notes/pages/HighlightLinkingPage';
import ArticlesPage from './features/imports/pages/ArticlesPage';
import DocumentsPage from './features/imports/pages/DocumentsPage';
import SourceDirectoryPage from './features/imports/pages/SourceDirectoryPage';
import YouTubeChannelList from './features/videos/pages/YouTubeChannelList';
import YouTubeChannelProfile from './features/videos/pages/YouTubeChannelProfile';
import YouTubePlaylistProfile from './features/videos/pages/YouTubePlaylistProfile';
import PodcastSeriesProfile from './features/podcasts/pages/PodcastSeriesProfile';
import TvShowProfile from './features/videos/pages/TvShowProfile';
import WebsiteImportPage from './features/imports/pages/WebsiteImportPage';
import WebsitesPage from './features/imports/pages/WebsitesPage';
import GoodreadsUploadPage from './features/media/pages/GoodreadsUploadPage';
import NoteProfilePage from './features/notes/pages/NoteProfilePage';
import HighlightProfilePage from './features/notes/pages/HighlightProfilePage';
import NotesListingPage from './features/notes/pages/NotesListingPage';
import AiAdminPage from './features/admin/pages/AiAdminPage';
import SearchByVibePage from './features/search/pages/SearchByVibePage';
import DemoDataUploadPage from './features/demo/pages/DemoDataUploadPage';

const DemoUnlockPage = lazy(() => import('./features/demo/pages/DemoUnlockPage'));
const DemoPage = lazy(() => import('./features/demo/pages/DemoPage'));
const ImportMixlistPage = lazy(() => import('./features/imports/pages/ImportMixlistPage'));
const TypesenseAdminPage = lazy(() => import('./features/admin/pages/TypesenseAdminPage'));
const BackgroundJobsPage = lazy(() => import('./features/admin/pages/BackgroundJobsPage'));

function RouteErrorFallback({ error, resetErrorBoundary }) {
  return (
    <Box role="alert" sx={{ p: 4, maxWidth: 600, mx: 'auto', textAlign: 'center' }}>
      <Typography variant="h5" gutterBottom>This page hit an error</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        You can try this page again, or navigate elsewhere using the menu above.
      </Typography>
      <Box component="pre" sx={{ textAlign: 'left', bgcolor: 'rgba(255,255,255,0.05)', p: 2, borderRadius: 1, overflow: 'auto', fontSize: '0.85rem', mb: 2 }}>
        {error?.message || 'Unknown error'}
      </Box>
      <Button variant="contained" onClick={resetErrorBoundary}>Try again</Button>
    </Box>
  );
}

function RoutedContent() {
  const location = useLocation();
  return (
    <ErrorBoundary
      FallbackComponent={RouteErrorFallback}
      resetKeys={[location.pathname]}
      onError={(error) => {
        console.error('Route error:', error);
        Sentry.captureException(error);
      }}
    >
      <Suspense fallback={<LoadingSpinner fullScreen message="Loading page..." />}>
        <Routes>
            {/* Public routes - always accessible. The demo site has no login;
                send visitors to the unlock page instead. */}
            <Route path="/login" element={isDemoMode() ? <Navigate to="/demo-unlock" replace /> : <LoginPage />} />
            <Route path="/demo" element={<DemoPage />} />

            {/* Protected routes - require login in production, open in demo */}
            <Route path="/" element={
              <ConditionalProtectedRoute><HomePage /></ConditionalProtectedRoute>
            } />
            <Route path="/add-media" element={
              <ConditionalProtectedRoute><AddMediaForm /></ConditionalProtectedRoute>
            } />
            <Route path="/all-media" element={
              <ConditionalProtectedRoute><Search defaultMediaTypes={['all']} /></ConditionalProtectedRoute>
            } />
            {/* The standalone mixlists page was retired in favor of the search page's
                mixlists mode. The route stays as a redirect so existing links and
                bookmarks keep working. */}
            <Route path="/mixlists" element={<Navigate to="/search?searchMode=mixlists" replace />} />
            <Route path="/mixlist/:id" element={
              <ConditionalProtectedRoute><MixlistProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlist/:id/edit" element={
              <ConditionalProtectedRoute><EditMixlistForm /></ConditionalProtectedRoute>
            } />
            <Route path="/create-mixlist" element={
              <ConditionalProtectedRoute><CreateMixlistForm /></ConditionalProtectedRoute>
            } />
            {/* Browsable in the demo so visitors can see how importing works; the
                page's own actions are disabled there. */}
            <Route path="/import-media" element={
              <ConditionalProtectedRoute><ImportMediaPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-mixlist" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><ImportMixlistPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/import-genres-topics" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><ImportGenresTopicsPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/upload-media" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><UploadMediaPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/upload-goodreads" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><GoodreadsUploadPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/search-by-topic-genre" element={
              <ConditionalProtectedRoute><SearchByTopicOrGenre /></ConditionalProtectedRoute>
            } />
            <Route path="/search" element={
              <ConditionalProtectedRoute><Search /></ConditionalProtectedRoute>
            } />
            <Route path="/media/:id" element={
              <ConditionalProtectedRoute><MediaProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/media/:id/edit" element={
              <ConditionalProtectedRoute><EditMediaForm /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube/callback" element={
              <ConditionalProtectedRoute><YouTubeCallback /></ConditionalProtectedRoute>
            } />
            <Route path="/readwise-sync" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><ReadwiseSyncPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/trakt-sync" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><TraktSyncPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/highlight-linking" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><HighlightLinkingPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/articles" element={
              <ConditionalProtectedRoute><ArticlesPage /></ConditionalProtectedRoute>
            } />
            <Route path="/documents" element={
              <ConditionalProtectedRoute><DocumentsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/sources" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><SourceDirectoryPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/youtube-channels" element={
              <ConditionalProtectedRoute><YouTubeChannelList /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube-channel/:id" element={
              <ConditionalProtectedRoute><YouTubeChannelProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/youtube-playlist/:id" element={
              <ConditionalProtectedRoute><YouTubePlaylistProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/podcast-series/:id" element={
              <ConditionalProtectedRoute><PodcastSeriesProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/tv-show/:id" element={
              <ConditionalProtectedRoute><TvShowProfile /></ConditionalProtectedRoute>
            } />
            <Route path="/podcast-episode/:id" element={
              <ConditionalProtectedRoute><MediaProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-website" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><WebsiteImportPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/websites" element={
              <ConditionalProtectedRoute><WebsitesPage /></ConditionalProtectedRoute>
            } />
            <Route path="/typesense-admin" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><TypesenseAdminPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/background-jobs" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><BackgroundJobsPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/note/:id" element={
              <ConditionalProtectedRoute><NoteProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/notes" element={
              <ConditionalProtectedRoute><NotesListingPage /></ConditionalProtectedRoute>
            } />
            <Route path="/highlight/:id" element={
              <ConditionalProtectedRoute><HighlightProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/ai-admin" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><AiAdminPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
            <Route path="/search-by-vibe" element={
              <ConditionalProtectedRoute><SearchByVibePage /></ConditionalProtectedRoute>
            } />
            {/* The demo site's sign-in page: public, and absent from production builds. */}
            {isDemoMode() && <Route path="/demo-unlock" element={<DemoUnlockPage />} />}
            <Route path="/upload-demo-data" element={
              <ConditionalProtectedRoute><DemoRestrictedRoute><DemoDataUploadPage /></DemoRestrictedRoute></ConditionalProtectedRoute>
            } />
          {/* Catch-all route for 404 */}
          <Route path="*" element={
            <div style={{ padding: '2rem', textAlign: 'center' }}>
              <Typography variant="h4">Page Not Found</Typography>
              <Typography variant="body1">The page you&apos;re looking for doesn&apos;t exist.</Typography>
              <Button component={Link} to="/" variant="contained" sx={{ mt: 2 }}>
                Go Home
              </Button>
            </div>
          } />
        </Routes>
      </Suspense>
    </ErrorBoundary>
  );
}

function App() {
  return (
    // The ThemeProvider wraps the entire application
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Router>
        <AuthProvider>
          <DemoReadOnlyProvider>
          <DemoReadOnlyDialog />
          <ApiErrorListener />
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              minHeight: '100vh'
            }}
          >
            {/* Demo-only read-only / write-mode banner (renders null outside demo mode) */}
            <DemoBanner />

            {/* Responsive Navigation Component */}
            <ResponsiveNavigation />

            {/* Routes without outer container - each component handles its own layout */}
            <Box component="main" sx={{ flexGrow: 1 }}>
              <RoutedContent />
            </Box>

            {/* Footer Component */}
            <Footer />
          </Box>
          </DemoReadOnlyProvider>
        </AuthProvider>
      </Router>
    </ThemeProvider>
  );
}

export default App;
