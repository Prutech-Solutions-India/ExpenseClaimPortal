/**
 * The one place this bundle knows how to reach the API.
 *
 * Paths are relative, never absolute: in development Vite proxies /api to the
 * service, and in production ASP.NET serves this bundle itself, so the same
 * path works in both without a base URL to configure or to leak.
 */

export interface Health {
  status: string;
  version: string;
  commit: string;
}

export interface Greeting {
  greeting: string;
}

/** A failure the UI can render, rather than a thrown string nobody catches. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

async function readProblem(response: Response): Promise<string> {
  // The API answers failures with a problem document. Validation failures put
  // the useful part in `errors`, so surface that rather than the generic title.
  try {
    const body = (await response.json()) as {
      title?: string;
      errors?: Record<string, string[]>;
    };
    const first = Object.values(body.errors ?? {})[0]?.[0];
    return first ?? body.title ?? `Request failed with status ${response.status}.`;
  } catch {
    return `Request failed with status ${response.status}.`;
  }
}

export async function getHealth(): Promise<Health> {
  const response = await fetch('/api/health');
  if (!response.ok) {
    throw new ApiError(await readProblem(response), response.status);
  }
  return (await response.json()) as Health;
}

export async function createGreeting(name: string): Promise<Greeting> {
  const response = await fetch('/api/greetings', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name }),
  });
  if (!response.ok) {
    throw new ApiError(await readProblem(response), response.status);
  }
  return (await response.json()) as Greeting;
}
