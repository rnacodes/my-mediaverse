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

// Side-effect import: evaluating the module registers its interceptors on the
// mocked axios instance, which the tests below read via globalThis.__testInterceptors.
import axios from 'axios';
import { DEMO_READ_ONLY_CODE } from './apiClient';

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

        it('should dispatch demoWriteBlocked on the machine-readable code', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

            // The code is what the API will emit once the demo write gate lands; it must
            // be recognized without depending on the message wording.
            const error = {
                response: {
                    status: 403,
                    data: {
                        code: DEMO_READ_ONLY_CODE,
                        blockedOperation: 'POST',
                        message: 'Rephrased read-only wording'
                    }
                },
                config: { url: '/api/media' }
            };

            await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(error);

            expect(dispatchSpy).toHaveBeenCalledWith(
                expect.objectContaining({
                    type: 'demoWriteBlocked',
                    detail: expect.objectContaining({ blockedOperation: 'POST' })
                })
            );

            dispatchSpy.mockRestore();
        });

        it('should dispatch apiForbidden, not demoWriteBlocked, for other 403 errors', async () => {
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
            // Previously nothing was dispatched at all, so the user saw no feedback.
            expect(dispatchSpy).toHaveBeenCalledWith(
                expect.objectContaining({
                    type: 'apiForbidden',
                    detail: expect.objectContaining({
                        path: '/api/media',
                        message: 'You do not have permission'
                    })
                })
            );

            dispatchSpy.mockRestore();
        });

        it('should fall back to a generic message when a 403 carries no body', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

            const error = {
                response: { status: 403 },
                config: { url: '/api/media' }
            };

            await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(error);

            expect(dispatchSpy).toHaveBeenCalledWith(
                expect.objectContaining({
                    type: 'apiForbidden',
                    detail: expect.objectContaining({
                        message: 'You do not have permission to perform this action.'
                    })
                })
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

    describe('401 Handling (Response Interceptor)', () => {
        it('should dispatch sessionExpired when the token refresh fails', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');
            const refreshError = new Error('refresh rejected');
            axios.post.mockRejectedValueOnce(refreshError);

            const error = {
                response: { status: 401 },
                config: { url: '/api/media', headers: {} }
            };

            await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(refreshError);

            // The old behavior assigned window.location.href, which reloaded the SPA and
            // silently discarded the route the user was on.
            expect(dispatchSpy).toHaveBeenCalledWith(
                expect.objectContaining({
                    type: 'sessionExpired',
                    detail: expect.objectContaining({ path: '/api/media' })
                })
            );

            dispatchSpy.mockRestore();
        });

        it.each(['/auth/login', '/auth/refresh', '/auth/logout'])(
            'should not attempt a refresh for a 401 from %s',
            async (url) => {
                const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

                const error = {
                    response: { status: 401 },
                    config: { url, headers: {} }
                };

                await expect(globalThis.__testInterceptors.responseError(error)).rejects.toBe(error);

                expect(axios.post).not.toHaveBeenCalled();
                expect(dispatchSpy).not.toHaveBeenCalledWith(
                    expect.objectContaining({ type: 'sessionExpired' })
                );

                dispatchSpy.mockRestore();
            }
        );

        it('should retry the original request when the refresh succeeds', async () => {
            axios.post.mockResolvedValueOnce({ data: { token: 'fresh-token' } });

            const error = {
                response: { status: 401 },
                config: { url: '/api/media', headers: {} }
            };

            await globalThis.__testInterceptors.responseError(error).catch(() => {});

            expect(error.config.headers['Authorization']).toBe('Bearer fresh-token');
        });
    });
});
