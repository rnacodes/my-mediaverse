import { describe, it, expect, vi, beforeEach } from 'vitest';

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
    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe('Demo 403 Interception (Response Interceptor)', () => {
        it('should not treat a 403 without the machine-readable code as demo read-only', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

            // Message text alone must never trigger the read-only dialog — only the code.
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

            expect(dispatchSpy).not.toHaveBeenCalledWith(
                expect.objectContaining({ type: 'demoWriteBlocked' })
            );
            expect(dispatchSpy).toHaveBeenCalledWith(
                expect.objectContaining({ type: 'apiForbidden' })
            );

            dispatchSpy.mockRestore();
        });

        it('should dispatch demoWriteBlocked on the machine-readable code', async () => {
            const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

            // The code is what the API emits from the demo write gate; it must be
            // recognized without depending on the message wording.
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
                        code: DEMO_READ_ONLY_CODE,
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

        it.each(['/auth/login', '/auth/refresh', '/auth/logout', '/demo/unlock'])(
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
