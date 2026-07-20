const LOCAL_HOSTNAMES = ['localhost', '127.0.0.1', '[::1]', ''];

export const isDemoMode = () => import.meta.env.VITE_DEMO_MODE === 'true';

const isLocalHost = () =>
    typeof window !== 'undefined' &&
    LOCAL_HOSTNAMES.includes(window.location.hostname);

export const isPublicDemo = () => isDemoMode() && !isLocalHost();
