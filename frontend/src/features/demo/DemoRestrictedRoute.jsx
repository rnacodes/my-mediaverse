import { useAuth } from '@/contexts/AuthContext';
import { isPublicDemo } from '@/utils/demoMode';
import DemoUnavailablePage from './pages/DemoUnavailablePage';

const DemoRestrictedRoute = ({ children }) => {
    const { isAuthenticated } = useAuth();

    if (isPublicDemo() && !isAuthenticated) {
        return <DemoUnavailablePage />;
    }

    return children;
};

export default DemoRestrictedRoute;
