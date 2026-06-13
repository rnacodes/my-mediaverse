import { Card, CardMedia, CardContent, Typography } from '@mui/material';
import { resolveMediaImage, getPlaceholderImage } from '@/utils/mediaImageUtils';

// Clickable card for a mixlist. Tolerates both camelCase and PascalCase payloads.
const MixlistCard = ({ mixlist, onNavigate }) => (
  <Card
    sx={{
      height: '100%',
      display: 'flex',
      flexDirection: 'column',
      cursor: 'pointer',
      '&:hover': {
        transform: 'translateY(-4px) scale(1.02)',
        boxShadow: 8,
        '& .MuiCardMedia-root': {
          transform: 'scale(1.05)'
        }
      },
      transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
    }}
    onClick={() => onNavigate(`/mixlist/${mixlist.id || mixlist.Id}`)}
  >
    <CardMedia
      component="img"
      sx={{
        flexShrink: 0,
        height: 180,
        transition: 'transform 0.3s cubic-bezier(0.4, 0, 0.2, 1)'
      }}
      image={resolveMediaImage(mixlist, 'Mixlist')}
      alt={mixlist.name || mixlist.Name}
      onError={(e) => { e.target.onerror = null; e.target.src = getPlaceholderImage('Mixlist'); }}
    />
    <CardContent sx={{ flexGrow: 1 }}>
      <Typography gutterBottom variant="h6" component="div" sx={{ fontWeight: 'bold' }}>
        {mixlist.name || mixlist.Name}
      </Typography>
      <Typography variant="body2" color="#ffffff">
        {mixlist.description || mixlist.Description}
      </Typography>
    </CardContent>
  </Card>
);

export default MixlistCard;
