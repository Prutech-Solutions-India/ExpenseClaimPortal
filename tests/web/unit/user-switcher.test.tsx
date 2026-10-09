/**
 * AC-6: the switcher has to say what it is doing.
 *
 * Four states, four renderings: loading, empty, failed and populated. A blank
 * header while the list is in flight, or a silent one when the call failed, is
 * the same screen as "there are no employees" - and the person in front of it
 * has no way to tell which. Each state is asserted here against the
 * design-system primitive that is supposed to carry it.
 *
 * The identity context is replaced with a stub so this file exercises the
 * switcher and nothing else; the real provider is covered end to end in
 * tests/web/integration/App.test.tsx.
 */

import type { ReactNode } from 'react';

import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { UserSwitcher } from '@/components/UserSwitcher';
import type { EmployeeSummary, MeResponse } from '@/lib/api';

const { useIdentityMock } = vi.hoisted(() => ({ useIdentityMock: vi.fn() }));

vi.mock('@/lib/identity', () => ({
  useIdentity: () => useIdentityMock(),
  IdentityProvider: ({ children }: { children: ReactNode }) => <>{children}</>,
}));

/** The slice of the identity context the switcher consumes. */
interface IdentityValue {
  employees: EmployeeSummary[];
  status: 'loading' | 'ready' | 'empty' | 'error';
  error: string | null;
  actingId: number | null;
  setActingId: (id: number | null) => void;
  me: MeResponse | null;
  meError: string | null;
  reload: () => void;
}

const setActingId = vi.fn();
const reload = vi.fn();

const staff: EmployeeSummary[] = [
  { id: 1, displayName: 'Ada Lovelace', role: 'Manager', managerId: null },
  { id: 2, displayName: 'Grace Hopper', role: 'FinanceOfficer', managerId: 1 },
  { id: 3, displayName: 'Alan Turing', role: 'Employee', managerId: 1 },
];

/** Builds a context value, defaulting to the state before anything has loaded. */
function identity(overrides: Partial<IdentityValue> = {}): IdentityValue {
  return {
    employees: [],
    status: 'loading',
    error: null,
    actingId: null,
    setActingId,
    me: null,
    meError: null,
    reload,
    ...overrides,
  };
}

/** Renders the switcher over the supplied context state. */
function renderSwitcher(value: IdentityValue) {
  useIdentityMock.mockReturnValue(value);
  return render(<UserSwitcher />);
}

beforeEach(() => {
  vi.clearAllMocks();
});

describe('the user switcher while the staff list is loading', () => {
  it('shows the design-system spinner rather than an empty header', () => {
    const { container } = renderSwitcher(identity({ status: 'loading' }));

    expect(screen.getByRole('status')).toBeInTheDocument();
    expect(container.querySelector('.ds-spinner')).not.toBeNull();
    expect(screen.queryByRole('combobox')).toBeNull();
  });
});

describe('the user switcher when there is nobody to choose', () => {
  it('shows the empty state instead of an empty dropdown', () => {
    const { container } = renderSwitcher(identity({ status: 'empty', employees: [] }));

    expect(container.querySelector('.ds-empty')).not.toBeNull();
    expect(container.textContent?.trim()).not.toBe('');
    expect(screen.queryByRole('combobox')).toBeNull();
  });
});

describe('the user switcher when the staff list cannot be loaded', () => {
  it('explains the failure in an error banner', () => {
    const { container } = renderSwitcher(
      identity({ status: 'error', error: 'The staff list could not be loaded.' }),
    );

    expect(container.querySelector('.ds-banner--error')).not.toBeNull();
    expect(screen.getByText(/could not be loaded/i)).toBeInTheDocument();
    expect(screen.queryByRole('combobox')).toBeNull();
  });

  it('offers a labelled retry that asks the context to load again', async () => {
    const user = userEvent.setup();
    renderSwitcher(identity({ status: 'error', error: 'The staff list could not be loaded.' }));

    const retry = screen.getByRole('button');
    expect(retry).toHaveAccessibleName();

    await user.click(retry);

    expect(reload).toHaveBeenCalledTimes(1);
  });
});

describe('the user switcher with the seeded staff loaded', () => {
  it('lists every employee and shows the current selection', () => {
    renderSwitcher(identity({ status: 'ready', employees: staff, actingId: 2 }));

    const select = screen.getByRole('combobox', { name: /acting as/i }) as HTMLSelectElement;

    const optionLabels = within(select)
      .getAllByRole('option')
      .map((option) => option.textContent);

    expect(optionLabels).toEqual(
      expect.arrayContaining(['Ada Lovelace', 'Grace Hopper', 'Alan Turing']),
    );
    expect(select.value).toBe('2');
  });

  it('carries an accessible label so the control is not an unnamed box', () => {
    renderSwitcher(identity({ status: 'ready', employees: staff, actingId: 1 }));

    expect(screen.getByRole('combobox', { name: /acting as/i })).toHaveAccessibleName(/acting as/i);
  });

  it('is reachable and operable from the keyboard alone', async () => {
    const user = userEvent.setup();
    renderSwitcher(identity({ status: 'ready', employees: staff, actingId: 1 }));

    const select = screen.getByRole('combobox', { name: /acting as/i }) as HTMLSelectElement;

    await user.tab();
    expect(select).toHaveFocus();

    await user.selectOptions(select, '3');

    expect(setActingId).toHaveBeenCalledTimes(1);
    expect(Number(setActingId.mock.calls[0][0])).toBe(3);
  });
});
