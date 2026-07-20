import { useDemoAdmin } from '@/contexts/DemoAdminContext';
import { isPublicDemo } from '@/utils/demoMode';
import DemoUnavailablePage from './pages/DemoUnavailablePage';

const DemoRestrictedRoute = ({ children }) => {
    const { isAdminMode } = useDemoAdmin();

    if (isPublicDemo() && !isAdminMode) {
        return <DemoUnavailablePage />;
    }

    return children;
};

export default DemoRestrictedRoute;
