import { useAuth } from '@/contexts/AuthContext';
import { isPublicDemo } from '@/utils/demoMode';

// True when the visitor is browsing the public demo without having unlocked write
// access. 
export const useDemoWriteBlocked = () => {
    const { isAuthenticated } = useAuth();
    return isPublicDemo() && !isAuthenticated;
};

export default useDemoWriteBlocked;
