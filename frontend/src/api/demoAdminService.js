import axios from 'axios';

export const DEMO_API_BASE = 'https://demo-api.mymediaverseuniverse.com/api/demo';
export const DEMO_SITE_URL = 'https://demo.mymediaverseuniverse.com';

const demoAdminClient = axios.create({
    baseURL: DEMO_API_BASE,
    timeout: 15000,
    withCredentials: true,
});

/**
 * Read the demo site's current write-access status. */
export const getDemoStatus = async () => {
    const response = await demoAdminClient.get('/status');
    return response.data;
};
