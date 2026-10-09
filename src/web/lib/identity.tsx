/**
 * The acting-identity context.
 *
 * It holds the seeded staff list, which employee the user picked, and the
 * identity the *server* resolved for that pick. The distinction matters: the
 * selection is only a header value the browser offers, and `/api/me` is the
 * server's answer about who that header turned out to be. The UI renders the
 * answer, never the offer.
 *
 * The selection lives in React state for the lifetime of the page and nowhere
 * else - no localStorage, no module cache, no cookie. Persisting it would be
 * storing an authorisation decision in the one place the user controls.
 */

import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import type { ReactElement, ReactNode } from 'react';

import {
  ApiError,
  getActingEmployeeId,
  getEmployees,
  getMe,
  setActingEmployeeId,
} from '@/lib/api';
import type { EmployeeSummary, MeResponse } from '@/lib/api';

/** Where the staff list has got to. */
export type IdentityStatus = 'loading' | 'ready' | 'empty' | 'error';

/** Everything {@link useIdentity} exposes to a component. */
export interface IdentityContextValue {
  /** The seeded staff list, ordered as the server returned it. Empty until loaded. */
  employees: EmployeeSummary[];
  /** Load state of {@link employees}. */
  status: IdentityStatus;
  /** Why the staff list could not be loaded, when `status` is `error`. */
  error: string | null;
  /** The employee id currently being sent with every API call, or null. */
  actingId: number | null;
  /** Selects (or clears) the acting employee and refreshes {@link me}. */
  setActingId: (id: number | null) => void;
  /** The identity the server resolved for {@link actingId}, or null. */
  me: MeResponse | null;
  /** Why the server refused to resolve the identity, when it did. */
  meError: string | null;
  /** Retries the staff list after a failure. */
  reload: () => void;
}

const IdentityContext = createContext<IdentityContextValue | null>(null);

/** Props accepted by {@link IdentityProvider}. */
export interface IdentityProviderProps {
  children: ReactNode;
}

/** Turns a thrown value into something worth putting on the screen. */
function describeError(cause: unknown, fallback: string): string {
  if (cause instanceof ApiError) {
    return cause.message;
  }
  if (cause instanceof Error && cause.message !== '') {
    return cause.message;
  }
  return fallback;
}

/**
 * Loads the staff list once on mount and owns the acting selection beneath it.
 *
 * Mounted once, at the root, in `main.tsx`. Two providers would mean two
 * selections and two answers to "who am I".
 */
export function IdentityProvider({ children }: IdentityProviderProps): ReactElement {
  const [employees, setEmployees] = useState<EmployeeSummary[]>([]);
  const [status, setStatus] = useState<IdentityStatus>('loading');
  const [error, setError] = useState<string | null>(null);
  const [actingId, setActingIdState] = useState<number | null>(() => getActingEmployeeId());
  const [me, setMe] = useState<MeResponse | null>(null);
  const [meError, setMeError] = useState<string | null>(null);

  // Request tokens, so a slow reply from a superseded request cannot overwrite
  // the newer one it lost the race to.
  const listRequestRef = useRef(0);
  const meRequestRef = useRef(0);

  const loadEmployees = useCallback(async (): Promise<void> => {
    const token = listRequestRef.current + 1;
    listRequestRef.current = token;

    setStatus('loading');
    setError(null);

    try {
      const list = await getEmployees();
      if (listRequestRef.current !== token) {
        return;
      }
      setEmployees(list);
      setStatus(list.length === 0 ? 'empty' : 'ready');
    } catch (cause) {
      if (listRequestRef.current !== token) {
        return;
      }
      setEmployees([]);
      setError(describeError(cause, 'The staff list could not be loaded.'));
      setStatus('error');
    }
  }, []);

  const loadMe = useCallback(async (): Promise<void> => {
    const token = meRequestRef.current + 1;
    meRequestRef.current = token;

    try {
      const identity = await getMe();
      if (meRequestRef.current !== token) {
        return;
      }
      setMe(identity);
      setMeError(null);
    } catch (cause) {
      if (meRequestRef.current !== token) {
        return;
      }
      setMe(null);
      setMeError(describeError(cause, 'The acting identity could not be resolved.'));
    }
  }, []);

  const setActingId = useCallback(
    (id: number | null): void => {
      // The header is what the server reads, so it is set first and in the same
      // place as the state: a selection the UI shows but does not send is a
      // role the user thinks they have.
      setActingEmployeeId(id);
      setActingIdState(id);
      setMe(null);
      setMeError(null);

      if (id === null) {
        meRequestRef.current += 1;
        return;
      }

      void loadMe();
    },
    [loadMe],
  );

  const reload = useCallback((): void => {
    void loadEmployees();
  }, [loadEmployees]);

  useEffect(() => {
    void loadEmployees();

    if (getActingEmployeeId() !== null) {
      void loadMe();
    }
  }, [loadEmployees, loadMe]);

  const value = useMemo<IdentityContextValue>(
    () => ({
      employees,
      status,
      error,
      actingId,
      setActingId,
      me,
      meError,
      reload,
    }),
    [employees, status, error, actingId, setActingId, me, meError, reload],
  );

  return <IdentityContext.Provider value={value}>{children}</IdentityContext.Provider>;
}

/**
 * Reads the acting-identity context.
 *
 * @throws Error when called outside {@link IdentityProvider}, which is a wiring
 * mistake rather than a runtime condition and should fail loudly at the first
 * render instead of silently rendering an empty switcher.
 */
export function useIdentity(): IdentityContextValue {
  const context = useContext(IdentityContext);

  if (context === null) {
    throw new Error('useIdentity must be used inside an IdentityProvider.');
  }

  return context;
}
