/**
 * The typed client is the only place the browser touches HTTP, so the two
 * things it must never get wrong are tested here: the acting identity travels
 * on every request once one has been chosen, and a refusal from the server
 * arrives as an ApiError carrying the machine-readable code - not as a silent
 * undefined that some component later renders as a blank panel.
 *
 * No network is touched: fetch is stubbed for every test and restored after.
 */

import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import {
  ACTING_EMPLOYEE_HEADER,
  ApiError,
  apiGet,
  getActingEmployeeId,
  getEmployees,
  getJson,
  getMe,
  setActingEmployeeId,
} from '@/lib/api';
import type { EmployeeSummary } from '@/lib/api';

const staff: EmployeeSummary[] = [
  { id: 1, displayName: 'Ada Lovelace', role: 'Manager', managerId: null },
  { id: 2, displayName: 'Grace Hopper', role: 'FinanceOfficer', managerId: 1 },
];

/** A minimal stand-in for a fetch Response, enough for the client to read. */
function stubResponse(status: number, body: unknown): Response {
  const text = JSON.stringify(body);

  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status >= 200 && status < 300 ? 'OK' : 'Error',
    headers: {
      get: (name: string) =>
        name.toLowerCase() === 'content-type' ? 'application/json; charset=utf-8' : null,
    },
    json: async () => body,
    text: async () => text,
  } as unknown as Response;
}

/** Creates the fetch double with a harmless default implementation. */
function createFetchMock() {
  return vi.fn(async (input: unknown, init?: RequestInit): Promise<Response> =>
    stubResponse(200, { url: String(input), method: init?.method ?? 'GET' }),
  );
}

/** Reads a header from a fetch init, whatever shape the client used to send it. */
function headerValue(init: RequestInit | undefined, name: string): string | null {
  const headers = init?.headers;
  if (!headers) {
    return null;
  }

  if (typeof Headers !== 'undefined' && headers instanceof Headers) {
    return headers.get(name);
  }

  if (Array.isArray(headers)) {
    const pair = headers.find(([key]) => key.toLowerCase() === name.toLowerCase());
    return pair ? pair[1] : null;
  }

  const record = headers as Record<string, string>;
  const key = Object.keys(record).find((candidate) => candidate.toLowerCase() === name.toLowerCase());
  return key === undefined ? null : record[key];
}

let fetchMock = createFetchMock();

beforeEach(() => {
  setActingEmployeeId(null);
  fetchMock = createFetchMock();
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  setActingEmployeeId(null);
  vi.unstubAllGlobals();
});

describe('calling the API', () => {
  it('prefixes every path with /api and returns the parsed body', async () => {
    fetchMock.mockResolvedValue(stubResponse(200, staff));

    const employees = await getEmployees();

    expect(employees).toEqual(staff);
    expect(fetchMock).toHaveBeenCalledTimes(1);
    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/employees');
  });

  it('fetches any other /api path through the generic helpers', async () => {
    fetchMock.mockResolvedValue(stubResponse(200, staff));

    const viaApiGet = await apiGet<EmployeeSummary[]>('/employees');
    const viaGetJson = await getJson<EmployeeSummary[]>('/employees');

    expect(viaApiGet).toEqual(staff);
    expect(viaGetJson).toEqual(staff);
    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/employees');
    expect(String(fetchMock.mock.calls[1][0])).toBe('/api/employees');
  });
});

describe('the acting identity header', () => {
  it('is absent until an employee has been chosen', async () => {
    fetchMock.mockResolvedValue(stubResponse(200, staff));

    expect(getActingEmployeeId()).toBeNull();

    await getEmployees();

    expect(headerValue(fetchMock.mock.calls[0][1], ACTING_EMPLOYEE_HEADER)).toBeNull();
  });

  it('is attached to every later call once an employee is chosen', async () => {
    fetchMock.mockResolvedValue(
      stubResponse(200, {
        id: 2,
        displayName: 'Grace Hopper',
        email: 'grace.hopper@example.com',
        role: 'FinanceOfficer',
        managerId: 1,
      }),
    );

    setActingEmployeeId(2);
    expect(getActingEmployeeId()).toBe(2);

    const me = await getMe();

    expect(me.id).toBe(2);
    expect(me.role).toBe('FinanceOfficer');
    expect(String(fetchMock.mock.calls[0][0])).toBe('/api/me');
    expect(headerValue(fetchMock.mock.calls[0][1], ACTING_EMPLOYEE_HEADER)).toBe('2');
  });

  it('stops being sent when the selection is cleared', async () => {
    fetchMock.mockResolvedValue(stubResponse(200, staff));

    setActingEmployeeId(2);
    setActingEmployeeId(null);

    await getEmployees();

    expect(getActingEmployeeId()).toBeNull();
    expect(headerValue(fetchMock.mock.calls[0][1], ACTING_EMPLOYEE_HEADER)).toBeNull();
  });
});

describe('when the server refuses the request', () => {
  it('throws an ApiError carrying the 401 code for a missing identity', async () => {
    fetchMock.mockResolvedValue(
      stubResponse(401, {
        code: 'acting_identity_missing',
        message: 'The request was refused: no acting employee was named.',
      }),
    );

    const thrown = await getMe().catch((error: unknown) => error);

    expect(thrown).toBeInstanceOf(ApiError);

    const error = thrown as ApiError;
    expect(error.status).toBe(401);
    expect(error.code).toBe('acting_identity_missing');
    expect(error.message).toContain('refused');
  });

  it('throws an ApiError carrying the 403 code for an unknown identity', async () => {
    fetchMock.mockResolvedValue(
      stubResponse(403, {
        code: 'acting_identity_unknown',
        message: 'That employee was refused: no such row.',
      }),
    );

    setActingEmployeeId(987654);

    const thrown = await getMe().catch((error: unknown) => error);

    expect(thrown).toBeInstanceOf(ApiError);

    const error = thrown as ApiError;
    expect(error.status).toBe(403);
    expect(error.code).toBe('acting_identity_unknown');
    expect(error.message).toContain('refused');
  });

  it('rejects rather than resolving with nothing', async () => {
    fetchMock.mockResolvedValue(
      stubResponse(500, { code: 'unexpected', message: 'Something went wrong.' }),
    );

    await expect(getEmployees()).rejects.toBeInstanceOf(ApiError);
  });
});
