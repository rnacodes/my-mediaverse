import { createContext, useContext } from 'react';

export const DemoReadOnlyContext = createContext(null);

export const useDemoReadOnly = () => {
    const context = useContext(DemoReadOnlyContext);
    if (!context) {
        throw new Error('useDemoReadOnly must be used within a DemoReadOnlyProvider');
    }
    return context;
};

export default DemoReadOnlyContext;
