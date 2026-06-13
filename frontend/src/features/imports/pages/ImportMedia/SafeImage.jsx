import React, { useState, useEffect } from 'react';
import { getPlaceholderImage } from '@/utils/mediaImageUtils';

const SafeImage = ({ src, alt, style, className, onError, fallbackSrc }) => {
    const [error, setError] = useState(false);
    const placeholder = fallbackSrc || getPlaceholderImage('Video');

    useEffect(() => {
        setError(false);
    }, [src]);

    if (error || !src) {
        return (
            <img
                src={placeholder}
                alt={alt}
                style={style}
                className={className}
            />
        );
    }

    return (
        <img
            src={src}
            alt={alt}
            style={style}
            className={className}
            onError={(e) => {
                setError(true);
                if (onError) onError(e);
            }}
            referrerPolicy="no-referrer"
            loading="lazy"
        />
    );
};

export default SafeImage;
