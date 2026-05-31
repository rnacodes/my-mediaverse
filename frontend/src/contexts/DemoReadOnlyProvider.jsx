import { useState, useEffect, useCallback } from 'react';
import { DemoReadOnlyContext } from './DemoReadOnlyContext';

export const DemoReadOnlyProvider = ({ children }) => {
    const [isDialogOpen, setIsDialogOpen] = useState(false);
    const [blockedAction, setBlockedAction] = useState(null);
    const [lastDismissedAt, setLastDismissedAt] = useState(0);

    const showReadOnlyDialog = useCallback((actionInfo = null) => {
        const now = Date.now();
        if (now - lastDismissedAt < 2000) {
            return;
        }
        setBlockedAction(actionInfo);
        setIsDialogOpen(true);
    }, [lastDismissedAt]);

    const hideReadOnlyDialog = useCallback(() => {
        setIsDialogOpen(false);
        setBlockedAction(null);
        setLastDismissedAt(Date.now());
    }, []);

    useEffect(() => {
        const handleDemoWriteBlocked = (event) => {
            showReadOnlyDialog(event.detail);
        };

        window.addEventListener('demoWriteBlocked', handleDemoWriteBlocked);
        return () => {
            window.removeEventListener('demoWriteBlocked', handleDemoWriteBlocked);
        };
    }, [showReadOnlyDialog]);

    const value = {
        isDialogOpen,
        blockedAction,
        showReadOnlyDialog,
        hideReadOnlyDialog,
    };

    return (
        <DemoReadOnlyContext.Provider value={value}>
            {children}
        </DemoReadOnlyContext.Provider>
    );
};
