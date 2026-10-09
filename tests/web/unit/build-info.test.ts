import { describe, expect, it } from 'vitest';

import { buildInfo } from '@/lib/build-info';

describe('buildInfo', () => {
  it('admits when the commit was not injected at build time', () => {
    // A deployed bundle with no commit is a traceability gap, so the smoke
    // check has to be able to see it rather than read a plausible default.
    expect(buildInfo().commit).toBe('unknown');
  });

  it('falls back to a development version rather than an empty string', () => {
    expect(buildInfo().version).toMatch(/\d+\.\d+\.\d+/);
  });
});
