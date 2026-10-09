/**
 * The home page.
 *
 * It shows who the *server* says the caller is - the payload of `/api/me`,
 * resolved from the acting employee header against the database - rather than
 * the row the browser happens to have selected. The two are expected to agree,
 * and the point of showing the server's answer is that a disagreement is
 * visible instead of assumed away.
 *
 * Before an identity has been chosen it says so, because an empty panel reads
 * as a broken page.
 */

import type { ReactElement } from 'react';

import { formatRole } from '@/lib/format';
import { useIdentity } from '@/lib/identity';
import { Banner } from '@/lib/ui';

/** Renders the server-resolved acting identity, or a prompt to choose one. */
export default function Home(): ReactElement {
  const { me, employees, actingId } = useIdentity();

  if (actingId === null || me === null) {
    return (
      <section className="page">
        <h1 className="page__title">Acting identity</h1>
        <Banner variant="info">
          No acting identity selected. Choose an employee in the header and the server will resolve
          the role for every request.
        </Banner>
      </section>
    );
  }

  const manager =
    me.managerId === null
      ? null
      : (employees.find((employee) => employee.id === me.managerId)?.displayName ??
        `Employee ${String(me.managerId)}`);

  return (
    <section className="page">
      <h1 className="page__title">Acting identity</h1>
      <p className="page__lead">This is what the server resolved for the identity you supplied.</p>
      <dl className="identity-panel">
        <div className="identity-panel__row">
          <dt className="identity-panel__term">Name</dt>
          <dd className="identity-panel__value">{me.displayName}</dd>
        </div>
        <div className="identity-panel__row">
          <dt className="identity-panel__term">Email</dt>
          <dd className="identity-panel__value">{me.email}</dd>
        </div>
        <div className="identity-panel__row">
          <dt className="identity-panel__term">Role</dt>
          <dd className="identity-panel__value">{formatRole(me.role)}</dd>
        </div>
        <div className="identity-panel__row">
          <dt className="identity-panel__term">Manager</dt>
          <dd className="identity-panel__value">{manager ?? 'No manager'}</dd>
        </div>
      </dl>
    </section>
  );
}
