import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { DemoReadOnlyProvider } from './DemoReadOnlyProvider';
import { useDemoReadOnly } from './DemoReadOnlyContext';

const TestConsumer = ({ onRender }) => {
    const context = useDemoReadOnly();
    if (onRender) onRender(context);
    return (
        <div>
            <div data-testid="dialog-open">{context.isDialogOpen ? 'open' : 'closed'}</div>
            <div data-testid="blocked-action">{context.blockedAction ? JSON.stringify(context.blockedAction) : 'none'}</div>
            <button data-testid="show-dialog" onClick={() => context.showReadOnlyDialog('test action')}>
                Show
            </button>
            <button data-testid="hide-dialog" onClick={() => context.hideReadOnlyDialog()}>
                Hide
            </button>
        </div>
    );
};

describe('DemoReadOnlyContext', () => {
    beforeEach(() => {
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.runAllTimers();
        vi.useRealTimers();
    });

    describe('Initial State', () => {
        it('should have dialog closed by default', () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            expect(screen.getByTestId('dialog-open')).toHaveTextContent('closed');
        });

        it('should have no blocked action by default', () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            expect(screen.getByTestId('blocked-action')).toHaveTextContent('none');
        });
    });

    describe('Dialog Management', () => {
        it('should open dialog and set blocked action when showReadOnlyDialog is called', () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            act(() => {
                screen.getByTestId('show-dialog').click();
            });

            expect(screen.getByTestId('dialog-open')).toHaveTextContent('open');
            expect(screen.getByTestId('blocked-action')).toHaveTextContent('"test action"');
        });

        it('should close dialog and reset blocked action when hideReadOnlyDialog is called', () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            act(() => {
                screen.getByTestId('show-dialog').click();
            });
            expect(screen.getByTestId('dialog-open')).toHaveTextContent('open');

            act(() => {
                screen.getByTestId('hide-dialog').click();
            });

            expect(screen.getByTestId('dialog-open')).toHaveTextContent('closed');
            expect(screen.getByTestId('blocked-action')).toHaveTextContent('none');
        });

        it('should debounce rapid re-opens within 2 seconds', () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            act(() => {
                screen.getByTestId('show-dialog').click();
            });
            expect(screen.getByTestId('dialog-open')).toHaveTextContent('open');

            act(() => {
                screen.getByTestId('hide-dialog').click();
            });
            expect(screen.getByTestId('dialog-open')).toHaveTextContent('closed');

            act(() => {
                screen.getByTestId('show-dialog').click();
            });
            expect(screen.getByTestId('dialog-open')).toHaveTextContent('closed');

            act(() => {
                vi.advanceTimersByTime(2001);
            });

            act(() => {
                screen.getByTestId('show-dialog').click();
            });
            expect(screen.getByTestId('dialog-open')).toHaveTextContent('open');
        });
    });

    describe('Event Handling', () => {
        it('should open dialog when demoWriteBlocked event is dispatched', async () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            act(() => {
                window.dispatchEvent(new CustomEvent('demoWriteBlocked', {
                    detail: { blockedOperation: 'POST', path: '/api/media', message: 'Write blocked' }
                }));
            });

            expect(screen.getByTestId('dialog-open')).toHaveTextContent('open');
        });

        it('should pass event detail as blocked action info', () => {
            render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            const eventDetail = { blockedOperation: 'DELETE', path: '/api/media/123' };

            act(() => {
                window.dispatchEvent(new CustomEvent('demoWriteBlocked', {
                    detail: eventDetail
                }));
            });

            expect(screen.getByTestId('blocked-action')).toHaveTextContent(JSON.stringify(eventDetail));
        });

        it('should clean up event listener on unmount', () => {
            const removeEventListenerSpy = vi.spyOn(window, 'removeEventListener');

            const { unmount } = render(
                <DemoReadOnlyProvider>
                    <TestConsumer />
                </DemoReadOnlyProvider>
            );

            unmount();

            expect(removeEventListenerSpy).toHaveBeenCalledWith(
                'demoWriteBlocked',
                expect.any(Function)
            );

            removeEventListenerSpy.mockRestore();
        });
    });

    describe('useDemoReadOnly hook', () => {
        it('should throw error when used outside provider', () => {
            const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

            expect(() => {
                render(<TestConsumer />);
            }).toThrow('useDemoReadOnly must be used within a DemoReadOnlyProvider');

            consoleSpy.mockRestore();
        });
    });
});
