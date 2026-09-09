import { Box, Card, CardContent, Typography } from '@mui/material';

/**
 * A row of number tiles, the way the OPML importer reports its counts.
 * `stats` = [{ label, value, color? }].
 */
function StatTiles({ stats }) {
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 2, mb: 2 }}>
      {stats.map((stat) => (
        <Card key={stat.label} sx={{ flex: '1 1 120px', minWidth: 100 }}>
          <CardContent sx={{ textAlign: 'center', py: 2 }}>
            <Typography variant="h4" sx={{ color: stat.color ?? 'text.primary', fontWeight: 'bold' }}>
              {stat.value}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {stat.label}
            </Typography>
          </CardContent>
        </Card>
      ))}
    </Box>
  );
}

export default StatTiles;
