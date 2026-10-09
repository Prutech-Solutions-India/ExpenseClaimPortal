/**
 * The shell, wired together.
 *
 * This is the one frontend test that runs the real identity provider and the
 * real typed client over a stubbed fetch, so it proves the pieces meet: the
 * switcher loads the staff list, a selection is pushed into the client, and
 * every later request carries the acting employee to the server, which is what
 * `/api/me` answers from. The browser asserts an identity; the server resolves
 * the role. Nothing here trusts the UI to hide anything.
 *
 * No network is touched: fetch is stubbed for every test and restored after.
 */

import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import App from '@/App';
import { ACTING_EMPLOYEE_HEADER, setActingEmployeeId } from '@/lib/api';
import type { EmployeeSummary, MeResponse } from '@/lib/api';
import { IdentityProvider } from '@/lib/identity';

const staff: EmployeeSummary[] = [
  { id: 1, displayName: 'Ada Lovelace', role: 'Manager', managerId: null },
  { id: 2, displayName: 'Grace Hopper', role: 'FinanceOfficer', managerId: 1 },
  { id: 3, displayName: 'Alan Turing', role: 'Employee', managerId: 1 },
];

const graceHopper: MeResponse = {
  id: 2,
  displayName: 'Grace Hopper',
  email: 'grace.hopper@example.com',
  role: 'FinanceOfficer',
  managerId: 1,
};

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

/** Routes the stubbed fetch the way the server does: by path, under /api. */
function createFetchMock(employees: EmployeeSummary[], me: MeResponse) {
  return vi.fn(async (input: unknown, init?: RequestInit): Promise<Response> => {
    const url = String(input);
    void init;

    if (url.endsWith('/api/employees')) {
      return stubResponse(200, employees);
    }

    if (url.endsWith('/api/me')) {
      return stubResponse(200, me);
    }

    return stubResponse(404, { code: 'not_found', message: `No endpoint at ${url}.` });
  });
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

let fetchMock = createFetchMock(staff, graceHopper);

/** Mounts the shell at a path inside one router and one identity provider. */
function renderAt(path: string) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <IdentityProvider>
        <App />
      </IdentityProvider>
    </MemoryRouter>,
  );
}

beforeEach(() => {
  setActingEmployeeId(null);
  fetchMock = createFetchMock(staff, graceHopper);
  vi.stubGlobal('fetch', fetchMock);
});

afterEach(() => {
  setActingEmployeeId(null);
  vi.unstubAllGlobals();
});

describe('the application shell', () => {
  it('shows the home page and the staff switcher at the root', async () => {
    renderAt('/');

    expect(
      await screen.findByRole('combobox', { name: /acting as/i }),
    ).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Expense claim portal' })).toBeInTheDocument();
  });

  it('shows a not-found page for an unknown address', async () => {
    renderAt('/nothing-here');

    expect(await screen.findByRole('heading', { name: 'Page not found' })).toBeInTheDocument();
  });

  it('asks for an identity before one has been chosen', async () => {
    renderAt('/');

    expect(await screen.findByText(/no acting identity selected/i)).toBeInTheDocument();
  });
});

describe('choosing who to act as', () => {
  it('sends the chosen employee to the server and shows what the server resolved', async () => {
    const user = userEvent.setup();
    renderAt('/');

    const select = await screen.findByRole('combobox', { name: /acting as/i });
    await user.selectOptions(select, '2');

    const main = screen.getByRole('main');
    expect(await within(main).findByText('Grace Hopper')).toBeInTheDocument();
    expect(within(main).getByText('grace.hopper@example.com')).toBeInTheDocument();
    expect(within(main).getByText('Finance Officer')).toBeInTheDocument();

    const meCall = fetchMock.mock.calls.find(([input]) => String(input).endsWith('/api/me'));
    expect(meCall).toBeDefined();
    expect(headerValue(meCall?.[1], ACTING_EMPLOYEE_HEADER)).toBe('2');
  });

  it('names the manager the server reported for the acting employee', async () => {
    const user = userEvent.setup();
    renderAt('/');

    const select = await screen.findByRole('combobox', { name: /acting as/i });
    await user.selectOptions(select, '2');

    const main = screen.getByRole('main');
    expect(await within(main).findByText('Ada Lovelace')).toBeInTheDocument();
  });
});
