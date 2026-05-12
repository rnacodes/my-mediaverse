import { lazy, Suspense } from 'react';
import { BrowserRouter as Router, Route, Routes, Link, useLocation } from 'react-router-dom';
import { ErrorBoundary } from 'react-error-boundary';
import { ThemeProvider, CssBaseline, Typography, Button, Box } from '@mui/material';

// --- Eager imports: providers, route guards, chrome (always rendered) ---
import { AuthProvider } from './contexts/AuthProvider';
import { DemoAdminProvider } from './contexts/DemoAdminProvider';
import { DemoReadOnlyProvider } from './contexts/DemoReadOnlyProvider';
import ConditionalProtectedRoute from './components/ConditionalProtectedRoute';
import DemoReadOnlyDialog from './components/shared/DemoReadOnlyDialog';
import { theme } from './components/shared/DesignSystem';
import ResponsiveNavigation from './components/shared/ResponsiveNavigation';
import Footer from './components/shared/Footer';
import LoadingSpinner from './components/shared/LoadingSpinner';

// --- Lazy-loaded route components: each becomes its own chunk ---
const HomePage = lazy(() => import('./components/HomePage'));
const LoginPage = lazy(() => import('./components/LoginPage'));
const AddMediaForm = lazy(() => import('./components/AddMediaForm'));
const AllMedia = lazy(() => import('./components/AllMedia'));
const MixlistsPage = lazy(() => import('./components/MixlistsPage'));
const CreateMixlistForm = lazy(() => import('./components/CreateMixlistForm'));
const MixlistProfilePage = lazy(() => import('./components/MixlistProfilePage'));
const MediaProfilePage = lazy(() => import('./components/MediaProfilePage'));
const EditMediaForm = lazy(() => import('./components/EditMediaForm'));
const EditMixlistForm = lazy(() => import('./components/EditMixlistForm'));
const ImportMediaPage = lazy(() => import('./components/ImportMedia'));
const ImportMixlistPage = lazy(() => import('./components/ImportMixlistPage'));
const ImportGenresTopicsPage = lazy(() => import('./components/ImportGenresTopicsPage'));
const SearchByTopicOrGenre = lazy(() => import('./components/SearchByTopicOrGenre'));
const Search = lazy(() => import('./components/Search'));
const DemoPage = lazy(() => import('./components/DemoPage'));
const UploadMediaPage = lazy(() => import('./components/UploadMediaPage'));
const YouTubeCallback = lazy(() => import('./pages/YouTubeCallback'));
const ReadwiseSyncPage = lazy(() => import('./components/ReadwiseSyncPage'));
const TraktSyncPage = lazy(() => import('./components/TraktSyncPage'));
const HighlightLinkingPage = lazy(() => import('./components/HighlightLinkingPage'));
const ArticlesPage = lazy(() => import('./components/ArticlesPage'));
const DocumentsPage = lazy(() => import('./components/DocumentsPage'));
const SourceDirectoryPage = lazy(() => import('./components/SourceDirectoryPage'));
const YouTubeChannelList = lazy(() => import('./components/YouTubeChannelList'));
const YouTubeChannelProfile = lazy(() => import('./components/YouTubeChannelProfile'));
const YouTubePlaylistProfile = lazy(() => import('./components/YouTubePlaylistProfile'));
const PodcastSeriesProfile = lazy(() => import('./components/PodcastSeriesProfile'));
const TvShowProfile = lazy(() => import('./components/TvShowProfile'));
const CleanupManagementPage = lazy(() => import('./components/CleanupManagementPage'));
const WebsiteImportPage = lazy(() => import('./components/WebsiteImportPage'));
const WebsitesPage = lazy(() => import('./components/WebsitesPage'));
const TypesenseAdminPage = lazy(() => import('./components/TypesenseAdminPage'));
const GoodreadsUploadPage = lazy(() => import('./components/GoodreadsUploadPage'));
const BackgroundJobsPage = lazy(() => import('./components/BackgroundJobsPage'));
const NoteProfilePage = lazy(() => import('./components/NoteProfilePage'));
const HighlightProfilePage = lazy(() => import('./components/HighlightProfilePage'));
const ScriptExecutionPage = lazy(() => import('./components/ScriptExecutionPage'));
const NotesListingPage = lazy(() => import('./components/NotesListingPage'));
const AiAdminPage = lazy(() => import('./components/AiAdminPage'));
const SearchByVibePage = lazy(() => import('./components/SearchByVibePage'));
const DemoUnlockPage = lazy(() => import('./components/DemoUnlockPage'));
const DemoDataUploadPage = lazy(() => import('./components/DemoDataUploadPage'));

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
      onError={(error) => console.error('Route error:', error)}
    >
      <Suspense fallback={<LoadingSpinner fullScreen message="Loading page..." />}>
        <Routes>
            {/* Public routes - always accessible */}
            <Route path="/login" element={<LoginPage />} />
            <Route path="/demo" element={<DemoPage />} />

            {/* Protected routes - require login in production, open in demo */}
            <Route path="/" element={
              <ConditionalProtectedRoute><HomePage /></ConditionalProtectedRoute>
            } />
            <Route path="/add-media" element={
              <ConditionalProtectedRoute><AddMediaForm /></ConditionalProtectedRoute>
            } />
            <Route path="/all-media" element={
              <ConditionalProtectedRoute><AllMedia /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlists" element={
              <ConditionalProtectedRoute><MixlistsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlist/:id" element={
              <ConditionalProtectedRoute><MixlistProfilePage /></ConditionalProtectedRoute>
            } />
            <Route path="/mixlist/:id/edit" element={
              <ConditionalProtectedRoute><EditMixlistForm /></ConditionalProtectedRoute>
            } />
            <Route path="/create-mixlist" element={
              <ConditionalProtectedRoute><CreateMixlistForm /></ConditionalProtectedRoute>
            } />
            <Route path="/import-media" element={
              <ConditionalProtectedRoute><ImportMediaPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-mixlist" element={
              <ConditionalProtectedRoute><ImportMixlistPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-genres-topics" element={
              <ConditionalProtectedRoute><ImportGenresTopicsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/upload-media" element={
              <ConditionalProtectedRoute><UploadMediaPage /></ConditionalProtectedRoute>
            } />
            <Route path="/upload-goodreads" element={
              <ConditionalProtectedRoute><GoodreadsUploadPage /></ConditionalProtectedRoute>
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
              <ConditionalProtectedRoute><ReadwiseSyncPage /></ConditionalProtectedRoute>
            } />
            <Route path="/trakt-sync" element={
              <ConditionalProtectedRoute><TraktSyncPage /></ConditionalProtectedRoute>
            } />
            <Route path="/highlight-linking" element={
              <ConditionalProtectedRoute><HighlightLinkingPage /></ConditionalProtectedRoute>
            } />
            <Route path="/articles" element={
              <ConditionalProtectedRoute><ArticlesPage /></ConditionalProtectedRoute>
            } />
            <Route path="/documents" element={
              <ConditionalProtectedRoute><DocumentsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/sources" element={
              <ConditionalProtectedRoute><SourceDirectoryPage /></ConditionalProtectedRoute>
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
            <Route path="/cleanup" element={
              <ConditionalProtectedRoute><CleanupManagementPage /></ConditionalProtectedRoute>
            } />
            <Route path="/import-website" element={
              <ConditionalProtectedRoute><WebsiteImportPage /></ConditionalProtectedRoute>
            } />
            <Route path="/websites" element={
              <ConditionalProtectedRoute><WebsitesPage /></ConditionalProtectedRoute>
            } />
            <Route path="/typesense-admin" element={
              <ConditionalProtectedRoute><TypesenseAdminPage /></ConditionalProtectedRoute>
            } />
            <Route path="/background-jobs" element={
              <ConditionalProtectedRoute><BackgroundJobsPage /></ConditionalProtectedRoute>
            } />
            <Route path="/script-execution" element={
              <ConditionalProtectedRoute><ScriptExecutionPage /></ConditionalProtectedRoute>
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
              <ConditionalProtectedRoute><AiAdminPage /></ConditionalProtectedRoute>
            } />
            <Route path="/search-by-vibe" element={
              <ConditionalProtectedRoute><SearchByVibePage /></ConditionalProtectedRoute>
            } />
            <Route path="/demo-unlock" element={
              <ConditionalProtectedRoute><DemoUnlockPage /></ConditionalProtectedRoute>
            } />
            <Route path="/upload-demo-data" element={
              <ConditionalProtectedRoute><DemoDataUploadPage /></ConditionalProtectedRoute>
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
          <DemoAdminProvider>
          <DemoReadOnlyProvider>
          <DemoReadOnlyDialog />
          <Box
            sx={{
              display: 'flex',
              flexDirection: 'column',
              minHeight: '100vh'
            }}
          >
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
          </DemoAdminProvider>
        </AuthProvider>
      </Router>
    </ThemeProvider>
  );
}

export default App;
