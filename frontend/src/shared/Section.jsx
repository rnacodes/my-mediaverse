import { Box, Typography } from '@mui/material';

// A titled vertical section with consistent responsive spacing.
const Section = ({ title, children }) => (
    <Box sx={{ my: { xs: 3, sm: 4, md: 6 } }}>
        {title && (
            <Typography
                variant="h4"
                sx={{
                    fontSize: { xs: '1.5rem', sm: '1.8rem', md: '2.125rem' },
                    mb: { xs: 2, sm: 3 }
                }}
            >
                {title}
            </Typography>
        )}
        {children}
    </Box>
);

export default Section;
