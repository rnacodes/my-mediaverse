import { cloneElement } from 'react';
import { Tooltip } from '@mui/material';
import { useDemoWriteBlocked } from './useDemoWriteBlocked';

// Wraps a single control. When the demo blocks writes, the control is disabled and
// hovering explains why; otherwise the child renders untouched.
const DemoWriteGuard = ({
    children,
    title = 'Not available in the demo',
    style
}) => {
    const blocked = useDemoWriteBlocked();

    if (!blocked) return children;

    return (
        <Tooltip title={title}>
            <span style={{ display: 'inline-flex', ...style }}>
                {cloneElement(children, { disabled: true })}
            </span>
        </Tooltip>
    );
};

export default DemoWriteGuard;
