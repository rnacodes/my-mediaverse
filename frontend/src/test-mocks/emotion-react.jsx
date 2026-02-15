import React from 'react';

export const ThemeProvider = ({ children }) => <>{children}</>;
export const css = () => '';
export const keyframes = () => '';
export const Global = () => null;
export const ClassNames = ({ children }) => children({ css: () => '', cx: (...args) => args.filter(Boolean).join(' ') });
export const jsx = React.createElement;
export const jsxs = React.createElement;
export const CacheProvider = ({ children }) => <>{children}</>;
export const useTheme = () => ({});
