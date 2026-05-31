import React from 'react';
import { Button } from '@mui/material';

/**
 * WhiteOutlineButton - A reusable button component with white outline styling
 * 
 * @param {Object} props - Component props
 * @param {React.ReactNode} props.children - Button content
 * @param {Function} props.onClick - Click handler
 * @param {boolean} props.disabled - Whether button is disabled
 * @param {string} props.variant - Button variant (default: 'outlined')
 * @param {string} props.size - Button size (default: 'medium')
 * @param {Object} props.sx - Additional sx styles
 * @param {Object} props...other - Other props passed to Button
 */
const WhiteOutlineButton = ({ 
    children, 
    onClick, 
    disabled = false, 
    variant = 'outlined', 
    size = 'medium',
    sx = {},
    ...other 
}) => {
    const whiteOutlineStyles = {
        color: 'white', 
        borderColor: 'white',
        '&:hover': {
            backgroundColor: 'rgba(255, 255, 255, 0.1)',
            borderColor: 'white'
        },
        '&:disabled': {
            color: 'rgba(255, 255, 255, 0.5)',
            borderColor: 'rgba(255, 255, 255, 0.5)'
        }
    };

    return (
        <Button
            variant={variant}
            size={size}
            onClick={onClick}
            disabled={disabled}
            sx={{
                ...whiteOutlineStyles,
                ...sx
            }}
            {...other}
        >
            {children}
        </Button>
    );
};

export default WhiteOutlineButton;
