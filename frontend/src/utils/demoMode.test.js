import { describe, it, expect, afterEach, vi } from 'vitest';
import { stubHostname } from '@/test/test-utils';
import { isDemoMode, isPublicDemo } from './demoMode';

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe('isDemoMode', () => {
  it('is true only for the exact string "true"', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    expect(isDemoMode()).toBe(true);
  });

  it('is false when the flag is "false"', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    expect(isDemoMode()).toBe(false);
  });

  it('is false when the flag is unset', () => {
    vi.stubEnv('VITE_DEMO_MODE', undefined);
    expect(isDemoMode()).toBe(false);
  });

  it('ignores hostname entirely', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname('localhost');
    // Local dev keeps its auth bypass — that is the whole point of the split.
    expect(isDemoMode()).toBe(true);
  });
});

describe('isPublicDemo', () => {
  it('is true on the deployed demo host with the flag on', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname('demo.mymediaverseuniverse.com');
    expect(isPublicDemo()).toBe(true);
  });

  it.each(['localhost', '127.0.0.1', '[::1]'])(
    'is false on %s even with the flag on',
    (hostname) => {
      vi.stubEnv('VITE_DEMO_MODE', 'true');
      stubHostname(hostname);
      expect(isPublicDemo()).toBe(false);
    },
  );

  it('is false on a deployed host when the flag is off', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    stubHostname('www.mymediaverseuniverse.com');
    expect(isPublicDemo()).toBe(false);
  });
});
