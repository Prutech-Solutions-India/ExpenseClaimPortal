/**
 * The one place this bundle knows how to reach the API.
 *
 * Paths are relative, never absolute: in development Vite proxies /api to the
 * service, and in production ASP.NET serves this bundle itself, so the same
 * path works in both without a base URL to configure or to leak.
 *
 * This module also owns the acting-identity header. Nothing else in the
 * frontend attaches it, and nothing else calls fetch - if a request can be made
 * from two places, one of them will eventually forget to say who is calling.
 */

/** Prefix every API path carries. Mirrors the `/api` group on the server. */
const API_PREFIX = '/api';

/**
 * Request header carrying the acting employee's id.
 *
 * Must match `PilotService.Identity.ActingIdentity.HeaderName` exactly - two
 * spellings of the same header is how an identity quietly stops arriving.
 */
export const ACTING_EMPLOYEE_HEADER = 'X-Acting-Employee-Id';

/** The role codes the server recognises. */
export type RoleCode = 'Employee' | 'Manager' | 'FinanceOfficer';

/** One entry of `GET /api/employees`, as shown in the user switcher. */
export interface EmployeeSummary {
  id: number;
  displayName: string;
  role: RoleCode;
  managerId: number | null;
}

/** The acting identity as resolved on the server by `GET /api/me`. */
export interface MeResponse {
  id: number;
  displayName: string;
  email: string;
  role: RoleCode;
  managerId: number | null;
}

/** The machine-readable error payload every rejection carries. */
export interface ApiErrorPayload {
  code: string;
  message: string;
}

/** Code used when the response body carried no usable error payload. */
export const UNKNOWN_ERROR_CODE = 'unknown_error';

/** A failure the UI can render and branch on, rather than a thrown string nobody catches. */
export class ApiError extends Error {
  readonly status: number;

  readonly code: string;

  constructor(message: string, status: number, code: string = UNKNOWN_ERROR_CODE) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}

/**
 * The employee the browser is acting as. Held in module state for the lifetime
 * of the page only: the server is what decides the role, so there is nothing
 * here worth persisting and nothing a reload should be able to resurrect.
 */
let actingEmployeeId: number | null = null;

/** Sets (or clears) the employee id sent with every subsequent API call. */
export function setActingEmployeeId(id: number | null): void {
  actingEmployeeId = id;
}

/** Returns the employee id currently being sent, or null when none is selected. */
export function getActingEmployeeId(): number | null {
  return actingEmployeeId;
}

/** Joins a caller-supplied path onto the `/api` prefix without doubling it. */
function resolvePath(path: string): string {
  if (path.startsWith(`${API_PREFIX}/`) || path === API_PREFIX) {
    return path;
  }
  return `${API_PREFIX}${path.startsWith('/') ? '' : '/'}${path}`;
}

/** Turns a failed response into an ApiError, preserving the server's code when there is one. */
async function toApiError(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as Partial<ApiErrorPayload> | null;
    if (body && typeof body.code === 'string') {
      const message =
        typeof body.message === 'string' && body.message.length > 0
          ? body.message
          : `Request failed with status ${response.status}.`;
      return new ApiError(message, response.status, body.code);
    }
  } catch {
    // Fall through: a body that is not JSON tells us nothing beyond the status.
  }

  return new ApiError(`Request failed with status ${response.status}.`, response.status);
}

/**
 * Calls an API endpoint and returns its parsed JSON body.
 *
 * @param path Path relative to `/api`, for example `/employees`.
 * @param init Optional fetch options; headers supplied here are merged.
 * @throws ApiError when the response is not successful.
 */
export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  headers.set('Accept', 'application/json');

  if (actingEmployeeId !== null) {
    headers.set(ACTING_EMPLOYEE_HEADER, String(actingEmployeeId));
  }

  const response = await fetch(resolvePath(path), { ...init, headers });

  if (!response.ok) {
    throw await toApiError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

/** GETs an API endpoint and returns its parsed JSON body. */
export function apiGet<T>(path: string): Promise<T> {
  return apiFetch<T>(path, { method: 'GET' });
}

/** Alias kept for callers that read better as `getJson`. Same function, one implementation. */
export const getJson = apiGet;

/** The seeded staff list that backs the user switcher. Reachable without an acting identity. */
export function getEmployees(): Promise<EmployeeSummary[]> {
  return apiGet<EmployeeSummary[]>('/employees');
}

/** The acting identity as the server resolved it from the header. */
export function getMe(): Promise<MeResponse> {
  return apiGet<MeResponse>('/me');
}
