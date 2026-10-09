/**
 * What this build is, for the page footer and the post-deploy smoke check.
 *
 * The values are injected at build time by Vite from the environment, so a
 * deployed bundle can be traced back to the commit that produced it. Missing
 * values are reported as "unknown" rather than guessed: a smoke check that
 * invents a commit is worse than one that admits it has none.
 */
export interface BuildInfo {
  version: string;
  commit: string;
}

export function buildInfo(): BuildInfo {
  const env = import.meta.env as Record<string, string | undefined>;
  return {
    version: env.VITE_APP_VERSION ?? '0.0.0-dev',
    commit: env.VITE_COMMIT_SHA ?? 'unknown',
  };
}
