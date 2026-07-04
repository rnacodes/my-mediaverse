import React, { useState } from 'react';
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
  Divider,
  Slider,
  TextField,
} from '@mui/material';
import {
  Refresh as RefreshIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  Info as InfoIcon,
  Psychology as PsychologyIcon,
  Tune as TuneIcon,
} from '@mui/icons-material';
import {
  useAiStatus, usePendingNoteDescriptions, useGenerateNoteDescriptionsBatch,
} from '@/hooks/useAi';
import { useRecommendationStatus } from '@/hooks/useRecommendation';

// AI/recommendation errors use `data.message`; pending counts may be a number or { count }.
const apiMsg = (error, fallback) =>
  error ? (error.response?.data?.message || error.message || fallback) : null;
const toCount = (v) => (v == null ? null : (v?.count ?? v));

const AiAdminPage = () => {
  // ----- Status queries (retry:false: recommendation/pending sub-fetches are non-fatal) -----
  const aiStatusQuery = useAiStatus({ retry: false });
  const recommendationStatusQuery = useRecommendationStatus({ retry: false });
  const pendingDescQuery = usePendingNoteDescriptions({ retry: false });

  const aiStatus = aiStatusQuery.data ?? null;
  const statusLoading = aiStatusQuery.isFetching || recommendationStatusQuery.isFetching
    || pendingDescQuery.isFetching;
  const statusError = apiMsg(aiStatusQuery.error, 'Failed to fetch AI status');
  const fetchAllStatus = () => {
    aiStatusQuery.refetch();
    recommendationStatusQuery.refetch();
    pendingDescQuery.refetch();
  };

  // Map recommendation status into the { isAvailable, message } shape the JSX expects.
  const recommendationStatus = recommendationStatusQuery.data
    ? {
        isAvailable: recommendationStatusQuery.data.available ?? recommendationStatusQuery.data.isAvailable ?? false,
        message: recommendationStatusQuery.data.message,
      }
    : (recommendationStatusQuery.error ? { isAvailable: false } : null);

  // Pending counts (null while loading / on error, as before)
  const pendingDescriptions = toCount(pendingDescQuery.data);

  // ----- Generation mutations (each invalidates its pending query on success) -----
  const descMutation = useGenerateNoteDescriptionsBatch();

  const generatingDescriptions = descMutation.isPending;
  const descriptionsResult = descMutation.data ?? null;
  const descriptionsError = apiMsg(descMutation.error, 'Failed to generate descriptions');

  // State for similarity threshold
  const [similarityThreshold, setSimilarityThreshold] = useState(() => {
    const stored = localStorage.getItem('similarityThreshold');
    return stored !== null ? parseFloat(stored) : 0.40;
  });
  const [thresholdInput, setThresholdInput] = useState(() => {
    const stored = localStorage.getItem('similarityThreshold');
    return stored !== null ? String(Math.round(parseFloat(stored) * 100)) : '40';
  });

  const handleGenerateDescriptions = () => {
    descMutation.mutate(undefined);
  };

  const handleThresholdSliderChange = (_, newValue) => {
    const decimal = newValue / 100;
    setSimilarityThreshold(decimal);
    setThresholdInput(String(newValue));
    localStorage.setItem('similarityThreshold', String(decimal));
  };

  const handleThresholdInputChange = (e) => {
    const val = e.target.value;
    setThresholdInput(val);
    const num = parseInt(val, 10);
    if (!isNaN(num) && num >= 0 && num <= 100) {
      const decimal = num / 100;
      setSimilarityThreshold(decimal);
      localStorage.setItem('similarityThreshold', String(decimal));
    }
  };

  const isAiAvailable = aiStatus?.isAvailable || aiStatus?.available;

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h3" gutterBottom sx={{ mb: 4, fontWeight: 'bold' }}>
        AI Administration
      </Typography>

      {/* Service Status Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
          <Typography variant="h5" sx={{ fontWeight: 'bold' }}>
            Service Status
          </Typography>
          <Button
            variant="contained"
            startIcon={<RefreshIcon />}
            onClick={fetchAllStatus}
            disabled={statusLoading}
            sx={{
              backgroundColor: '#9c27b0',
              color: 'white',
              '&:hover': {
                backgroundColor: '#7b1fa2'
              }
            }}
          >
            Refresh
          </Button>
        </Box>

        {statusLoading && (
          <Box sx={{ display: 'flex', justifyContent: 'center', my: 2 }}>
            <CircularProgress />
          </Box>
        )}

        {statusError && (
          <Alert severity="error" icon={<ErrorIcon />} sx={{ mb: 2 }}>
            <strong>Status Check Failed:</strong> {statusError}
          </Alert>
        )}

        <Grid container spacing={2}>
          {/* Text Generation Service Status (DigitalOcean) */}
          <Grid item xs={12} md={6}>
            <Card variant="outlined" sx={{
              bgcolor: isAiAvailable ? 'success.light' : 'error.light',
              height: '100%'
            }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  {isAiAvailable ? (
                    <CheckCircleIcon sx={{ fontSize: 40, color: 'success.main' }} />
                  ) : (
                    <ErrorIcon sx={{ fontSize: 40, color: 'error.main' }} />
                  )}
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                      Text Generation ({aiStatus?.generationProvider || 'DigitalOcean'})
                    </Typography>
                    <Chip
                      label={isAiAvailable ? 'Available' : 'Unavailable'}
                      color={isAiAvailable ? 'success' : 'error'}
                      size="small"
                    />
                  </Box>
                </Box>
                {aiStatus?.generationModel && (
                  <Typography variant="body2" sx={{ mt: 1 }}>
                    Model: {aiStatus.generationModel}
                  </Typography>
                )}
              </CardContent>
            </Card>
          </Grid>

          {/* Recommendation Service Status */}
          <Grid item xs={12} md={6}>
            <Card variant="outlined" sx={{
              bgcolor: recommendationStatus?.isAvailable ? 'success.light' : 'warning.light',
              height: '100%'
            }}>
              <CardContent>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                  {recommendationStatus?.isAvailable ? (
                    <CheckCircleIcon sx={{ fontSize: 40, color: 'success.main' }} />
                  ) : (
                    <ErrorIcon sx={{ fontSize: 40, color: 'warning.main' }} />
                  )}
                  <Box>
                    <Typography variant="h6" sx={{ fontWeight: 'bold' }}>
                      Recommendation Service
                    </Typography>
                    <Chip
                      label={recommendationStatus?.isAvailable ? 'Available' : 'Unavailable'}
                      color={recommendationStatus?.isAvailable ? 'success' : 'warning'}
                      size="small"
                    />
                  </Box>
                </Box>
                <Typography variant="body2" sx={{ mt: 1 }}>
                  {recommendationStatus?.isAvailable
                    ? 'Typesense vector search is ready'
                    : 'Requires Typesense vector search'}
                </Typography>
              </CardContent>
            </Card>
          </Grid>
        </Grid>

        {/* Pending Counts */}
        {pendingDescriptions !== null && (
          <Box sx={{ mt: 3 }}>
            <Divider sx={{ mb: 2 }} />
            <Typography variant="subtitle1" sx={{ fontWeight: 'bold', mb: 1 }}>
              Pending Operations
            </Typography>
            <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
              <Chip
                icon={<PsychologyIcon />}
                label={`${pendingDescriptions} notes need descriptions`}
                color={pendingDescriptions > 0 ? 'warning' : 'success'}
              />
            </Box>
          </Box>
        )}
      </Paper>

      {/* Note Description Generation Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Note Description Generation
        </Typography>

        <Alert severity="info" icon={<InfoIcon />} sx={{ mb: 2 }}>
          Generate AI descriptions for notes that don&apos;t have manual descriptions.
          The AI analyzes note content and creates concise summaries.
        </Alert>

        <Card variant="outlined">
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Generate Batch Descriptions
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
              Process notes without descriptions in batches. Each batch processes up to 20 notes.
              {pendingDescriptions !== null && pendingDescriptions > 0 && (
                <strong> ({pendingDescriptions} notes pending)</strong>
              )}
            </Typography>
            <Button
              variant="contained"
              color="primary"
              startIcon={generatingDescriptions ? <CircularProgress size={20} color="inherit" /> : <PsychologyIcon />}
              onClick={handleGenerateDescriptions}
              disabled={generatingDescriptions || !isAiAvailable}
              fullWidth
            >
              {generatingDescriptions ? 'Generating...' : 'Generate Descriptions'}
            </Button>
          </CardContent>
        </Card>

        {descriptionsError && (
          <Alert severity="error" sx={{ mt: 2 }}>
            <strong>Generation Failed:</strong> {descriptionsError}
          </Alert>
        )}

        {descriptionsResult && (
          <Card variant="outlined" sx={{ mt: 2, bgcolor: 'success.light' }}>
            <CardContent>
              <Typography variant="h6" gutterBottom sx={{ fontWeight: 'bold', color: 'success.dark' }}>
                Description Generation Complete
              </Typography>
              <Box sx={{ textAlign: 'center', p: 2, bgcolor: 'background.paper', borderRadius: 1 }}>
                <Typography variant="h4" sx={{ fontWeight: 'bold', color: 'white'}}>
                  {descriptionsResult.successCount || descriptionsResult.processed || 0}
                </Typography>
                <Typography variant="body2" color="textSecondary">
                  Descriptions Generated
                </Typography>
              </Box>
              {descriptionsResult.failedCount > 0 && (
                <Typography variant="body2" color="error" sx={{ mt: 1 }}>
                  {descriptionsResult.failedCount} failed
                </Typography>
              )}
              {descriptionsResult.elapsedTime && (
                <Typography variant="body2" sx={{ mt: 1, fontStyle: 'italic' }}>
                  Completed in {descriptionsResult.elapsedTime}
                </Typography>
              )}
            </CardContent>
          </Card>
        )}
      </Paper>

      {/* Recommendation Settings Section */}
      <Paper elevation={3} sx={{ p: 3, mb: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
          Recommendation Settings
        </Typography>

        <Alert severity="info" icon={<TuneIcon />} sx={{ mb: 2 }}>
          Adjust the similarity threshold used to filter recommended items.
          Items below this threshold are hidden from &quot;Similar Items&quot; results.
          Stored locally in your browser.
        </Alert>

        <Card variant="outlined">
          <CardContent>
            <Typography variant="h6" gutterBottom>
              Similarity Threshold
            </Typography>
            <Typography variant="body2" color="textSecondary" sx={{ mb: 2 }}>
              Items with a similarity score below this percentage will be filtered out.
              Lower values show more results (less strict), higher values show fewer but more relevant results.
            </Typography>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 3, px: 1 }}>
              <Slider
                value={Math.round(similarityThreshold * 100)}
                onChange={handleThresholdSliderChange}
                min={0}
                max={100}
                step={5}
                marks={[
                  { value: 0, label: '0%' },
                  { value: 25, label: '25%' },
                  { value: 50, label: '50%' },
                  { value: 75, label: '75%' },
                  { value: 100, label: '100%' },
                ]}
                valueLabelDisplay="auto"
                valueLabelFormat={(v) => `${v}%`}
                sx={{ flex: 1, color: '#9c27b0' }}
              />
              <TextField
                value={thresholdInput}
                onChange={handleThresholdInputChange}
                size="small"
                type="number"
                inputProps={{ min: 0, max: 100, step: 5 }}
                sx={{ width: 80 }}
                InputProps={{
                  endAdornment: <Typography variant="body2">%</Typography>,
                }}
              />
            </Box>
            <Typography variant="body2" sx={{ mt: 2, fontStyle: 'italic', color: 'text.secondary' }}>
              Current: {Math.round(similarityThreshold * 100)}% — Items scoring below {Math.round(similarityThreshold * 100)}% similarity will be hidden
            </Typography>
          </CardContent>
        </Card>
      </Paper>

      {/* Background Service Info */}
      <Paper elevation={3} sx={{ p: 3 }}>
        <Typography variant="h5" gutterBottom sx={{ fontWeight: 'bold' }}>
        Coming Soon: Background Services
        </Typography>

        <Alert severity="info" icon={<InfoIcon />}>
          <Typography variant="body2">
            <strong>Automatic Processing:</strong> When enabled, background services automatically generate
            note descriptions on a schedule.
          </Typography>
          <Typography variant="body2" sx={{ mt: 1 }}>
            Configure with environment variables:
          </Typography>
          <ul style={{ margin: '8px 0', paddingLeft: '20px' }}>
            <li><code>NoteDescriptionGeneration__Enabled=true</code> - Enable automatic description generation (every 12 hours)</li>
          </ul>
        </Alert>
      </Paper>
    </Container>
  );
};

export default AiAdminPage;
