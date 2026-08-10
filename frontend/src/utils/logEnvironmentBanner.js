import { isDemoMode, isPublicDemo } from './demoMode';

// Development-only console summary of the flags that decide demo behavior.e.
export function logEnvironmentBanner() {

    const publicDemo = isPublicDemo();
    const demoFlag = isDemoMode();

    const meaning = publicDemo
        ? 'Public demo: writes disabled in the UI until unlocked'
        : demoFlag
            ? 'Demo flag set but host is local — demo restrictions are INACTIVE'
            : 'Normal mode: login required, writes enabled';

    console.info(
        [
            'My MediaVerse — frontend environment',
            `  hostname        : ${window.location.hostname}`,
            `  VITE_DEMO_MODE  : ${import.meta.env.VITE_DEMO_MODE ?? '(unset)'}`,
            `  isDemoMode()    : ${demoFlag}`,
            `  isPublicDemo()  : ${publicDemo}`,
            `  API             : ${import.meta.env.VITE_API_URL ?? '(unset)'}`,
            `  → ${meaning}`,
        ].join('\n')
    );
}

// Reachable from the devtools console as mmvEnv() so the banner can be reprinted without
// reloading the page.
if (import.meta.env.DEV && typeof window !== 'undefined') {
    window.mmvEnv = logEnvironmentBanner;
}
