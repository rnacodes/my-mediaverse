import { createContext, useContext } from 'react';

export const DemoAdminContext = createContext(null);

export const useDemoAdmin = () => {
    const context = useContext(DemoAdminContext);
    if (!context) {
        throw new Error('useDemoAdmin must be used within a DemoAdminProvider');
    }
    return context;
};
