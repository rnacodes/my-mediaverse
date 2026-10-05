import { Box, Tooltip } from '@mui/material';
import { useDemoWriteBlocked } from './useDemoWriteBlocked';

// The React version in use does not pass a boolean `inert` prop through, so it is set on the element.
const setInert = (element) => element?.setAttribute('inert', '');

// Wraps a group of fields and buttons.
const DemoDisabledArea = ({ children, title = 'Not available in the demo' }) => {
    const blocked = useDemoWriteBlocked();

    if (!blocked) return children;

    return (
        <Tooltip title={title} followCursor>
            <Box data-testid="demo-disabled-area">
                <Box
                    component="fieldset"
                    disabled
                    ref={setInert}
                    sx={{ border: 0, p: 0, m: 0, minWidth: 0, opacity: 0.5, pointerEvents: 'none' }}
                >
                    {children}
                </Box>
            </Box>
        </Tooltip>
    );
};

export default DemoDisabledArea;
