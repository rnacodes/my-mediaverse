import React, { useState, useEffect } from 'react';

const SafeImage = ({ src, alt, style, className, onError }) => {
    const [error, setError] = useState(false);

    useEffect(() => {
        setError(false);
    }, [src]);

    if (error || !src) {
        return (
            <img
                src="/placeholder-video.png"
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
