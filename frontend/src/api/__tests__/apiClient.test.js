import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

vi.mock('axios', () => {
    globalThis.__testInterceptors = globalThis.__testInterceptors || {};

    const mockAxiosInstance = {
        interceptors: {
            request: {
                use: vi.fn((onFulfilled) => {
                    globalThis.__testInterceptors.request = onFulfilled;
                })
            },
            response: {
                use: vi.fn((onFulfilled, onRejected) => {
                    globalThis.__testInterceptors.responseError = onRejected;
                })
            }
        },
        get: vi.fn(),
        post: vi.fn(),
        put: vi.fn(),
        delete: vi.fn()
    };

    return {
        default: {
            create: vi.fn(() => mockAxiosInstance),
            post: vi.fn()
        }
    };
});

import { apiClient } from '../apiClient';

describe('apiClient - Demo Mode Features', () => {
    let originalSessionStorage;

    beforeEach(() => {
        vi.clearAllMocks();
        originalSessionStorage = window.sessionStorage;
        const store = {};
        Object.defineProperty(window, 'sessionStorage', {
            value: {
                getItem: vi.fn((key) => store[key] || null),
                setItem: vi.fn((key, value) => { store[key] = value; }),
                removeItem: vi.fn((key) => { delete store[key]; }),
                clear: vi.fn(() => { Object.keys(store).forEach(k => delete store[k]); }),
            },
            writable: true,
            configurable: true,
        });
    });

    afterEach(() => {
        Object.defineProperty(window, 'sessionStorage', {
            value: originalSessionStorage,
            writable: true,
            configurable: true,
        });
    });

    describe('Demo Admin Key Header (Request Interceptor)', () => {
        it('should add X-Demo-Admin-Key header when key exists in sessionStorage', () => {
            sessionStorage.getItem.mockReturnValue('test-admin-key-123');

            const config = { headers: {} };
            const result = globalThis.__testInterceptors.request(config);

            expect(result.headers['X-Demo-Admin-Key']).toBe('test-admin-key-123');
        });

        it('should omit X-Demo-Admin-Key header when no key in sessionStorage', () => {
            sessionStorage.getItem.mockReturnValue(null);

            const config = { headers: {} };
            const result = globalThis.__testInterceptors.request(config);

            expect(result.headers['X-Demo-Admin-Key']).toBeUndefined();
        });
    });

    describe('Demo 403 Interception (Response Interceptor)', () => {
        it('should dispatch demoWriteBlocked event on demo-specific 403', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

            const error = {
                response: {
                    status: 403,
                    data: {
                        error: 'Write operations are disabled in demo mode',
                        blockedOperation: 'POST',
                        message: 'Read-only demo'
                    }
                },
                config: { url: '/api/media' }
            };

            await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(error);

            expect(dispatchSpy).toHaveBeenCalledWith(
                expect.objectContaining({
                    type: 'demoWriteBlocked',
                    detail: expect.objectContaining({
                        blockedOperation: 'POST',
                        path: '/api/media'
                    })
                })
            );

            dispatchSpy.mockRestore();
        });

        it('should NOT dispatch event for non-demo 403 errors', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

            const error = {
                response: {
                    status: 403,
                    data: {
                        error: 'Access denied',
                        message: 'You do not have permission'
                    }
                },
                config: { url: '/api/media' }
            };

            await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(error);

            expect(dispatchSpy).not.toHaveBeenCalledWith(
                expect.objectContaining({ type: 'demoWriteBlocked' })
            );

            dispatchSpy.mockRestore();
        });

        it('should re-throw the error after dispatching event', async () => {
            const error = {
                response: {
                    status: 403,
                    data: {
                        error: 'Write operations are disabled in demo mode',
                        blockedOperation: 'POST'
                    }
                },
                config: { url: '/api/media' }
            };

            await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(error);
        });
    });
});
