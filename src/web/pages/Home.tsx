/**
 * The landing page.
 *
 * It shows who the *server* says the browser is acting as. The value comes
 * from `/api/me` by way of the identity context, never from the selection the
 * switcher holds: the header is only what the browser asserted, and the role
 * that matters is the one the server resolved from the employee row.
 *
 * All four states the panel can be in - nothing selected, resolving, refused
 * and resolved - are rendered from the design-system primitives, so a blank
 * area never has to stand in for any of them.
 */

import type { ReactElement } from 'react';

import { buildInfo } from '@/lib/build-info';
import { formatRole } from '@/lib/format';
import { useIdentity } from '@/lib/identity';
import { Banner, Spinner } from '@/lib/ui';

/** Shown where a value is absent rather than merely unknown. */
const NONE = 'None';

/** Renders the identity panel and the build stamp. */
export default function Home(): ReactElement {
  const { employees, actingId, me, meError } = useIdentity();
  const info = buildInfo();

  const manager =
    me !== null && me.managerId !== null
      ? employees.find((employee) => employee.id === me.managerId)
      : undefined;

  return (
    <section className="page">
      <h1 className="page__title">Expense claim portal</h1>

      {actingId === null ? (
        <Banner title="No acting identity selected">
          <p>
            Choose an employee from the <strong>Acting as</strong> control in the header. Every
            request then carries that identity, and the server resolves the role it is allowed to
            use.
          </p>
        </Banner>
      ) : null}

      {actingId !== null && meError !== null ? (
        <Banner variant="error" title="The server refused this identity">
          <p>{meError}</p>
        </Banner>
      ) : null}

      {actingId !== null && me === null && meError === null ? (
        <Spinner label="Resolving identity" />
      ) : null}

      {me !== null ? (
        <div className="page__panel">
          <h2 className="page__subtitle">Resolved on the server</h2>
          <dl className="page__facts">
            <dt>Name</dt>
            <dd>{me.displayName}</dd>
            <dt>Email</dt>
            <dd>{me.email}</dd>
            <dt>Role</dt>
            <dd>{formatRole(me.role)}</dd>
            <dt>Manager</dt>
            <dd>{manager !== undefined ? manager.displayName : NONE}</dd>
          </dl>
        </div>
      ) : null}

      <footer className="page__footer">
        <dl className="page__facts">
          <dt>Version</dt>
          <dd>{info.version}</dd>
          <dt>Commit</dt>
          <dd>{info.commit}</dd>
        </dl>
      </footer>
    </section>
  );
}
