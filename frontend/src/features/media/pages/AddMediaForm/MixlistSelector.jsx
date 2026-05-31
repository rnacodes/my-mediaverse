import { useState } from 'react';
import { useFormContext } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { Box, Typography, Button, TextField, Chip } from '@mui/material';
import { useAllMixlists } from '@/hooks/useMixlist';

// Normalize a mixlist to consistent PascalCase Id/Name (the API returns mixed casing).
const normalize = (m) => ({
  ...m,
  Id: m.Id || m.id,
  Name: m.Name || m.name || `Mixlist ${m.Id || m.id}`,
});

function MixlistSelector() {
  const navigate = useNavigate();
  const { watch, setValue } = useFormContext();
  const selectedMixlists = watch('selectedMixlists');

  const [input, setInput] = useState('');
  const availableMixlists = useAllMixlists().data ?? [];

  const isSelected = (id) => selectedMixlists.some((m) => (m.Id || m.id) === id);

  const addMixlist = (mixlist) => {
    setValue('selectedMixlists', [...selectedMixlists, normalize(mixlist)]);
    setInput('');
  };

  const removeMixlist = (toRemove) => {
    setValue('selectedMixlists', selectedMixlists.filter((m) => m.Id !== toRemove.Id));
  };

  const handleKeyPress = (event) => {
    if (event.key === 'Enter' && input.trim()) {
      event.preventDefault();
      const match = availableMixlists.find((m) =>
        (m.Name || m.name || '').toLowerCase().includes(input.toLowerCase())
      );
      if (match && !isSelected(match.Id || match.id)) {
        addMixlist(match);
      }
    }
  };

  const suggestions = availableMixlists
    .filter((m) => {
      const name = m.Name || m.name || '';
      return name.toLowerCase().includes(input.toLowerCase()) && !isSelected(m.Id || m.id);
    })
    .slice(0, 5);

  return (
    <Box sx={{ mb: 3 }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6" sx={{ fontSize: '18px', fontWeight: 'bold', color: '#ffffff' }}>
          Add to Mixlists
        </Typography>
        <Button
          variant="contained"
          color="secondary"
          onClick={() => navigate('/create-mixlist', { state: { returnTo: '/add-media' } })}
          sx={{ fontSize: '16px', fontWeight: 'bold', textTransform: 'none', py: 1.5, px: 3, borderRadius: '8px', color: '#ffffff' }}
        >
          + New Mixlist
        </Button>
      </Box>

      <TextField
        placeholder="Type to search mixlists..."
        variant="outlined"
        fullWidth
        value={input}
        onChange={(e) => setInput(e.target.value)}
        onKeyPress={handleKeyPress}
        sx={{
          mb: 2,
          '& .MuiInputBase-input': { fontSize: '16px' },
          '& .MuiInputBase-input::placeholder': { color: '#ffffff', opacity: 1 },
        }}
      />

      {selectedMixlists.length > 0 && (
        <Box sx={{ mb: 2 }}>
          <Typography variant="body2" sx={{ fontSize: '14px', color: '#ffffff', mb: 1, fontWeight: 'bold' }}>
            Selected mixlists:
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {selectedMixlists.map((mixlist) => (
              <Chip
                key={mixlist.Id}
                label={mixlist.Name}
                onDelete={() => removeMixlist(mixlist)}
                size="small"
                sx={{ fontSize: '14px' }}
              />
            ))}
          </Box>
        </Box>
      )}

      {suggestions.length > 0 && input && (
        <Box sx={{ mt: 1 }}>
          <Typography variant="body2" sx={{ fontSize: '14px', color: '#ffffff', mb: 1, fontWeight: 'bold' }}>
            Available mixlists:
          </Typography>
          <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
            {suggestions.map((mixlist) => {
              const normalized = normalize(mixlist);
              return (
                <Chip
                  key={normalized.Id}
                  label={normalized.Name}
                  variant="outlined"
                  size="small"
                  onClick={() => addMixlist(mixlist)}
                  sx={{
                    fontSize: '12px',
                    cursor: 'pointer',
                    '&:hover': { backgroundColor: 'rgba(255, 255, 255, 0.1)' },
                  }}
                />
              );
            })}
          </Box>
        </Box>
      )}
    </Box>
  );
}

export default MixlistSelector;
