/**
 * The acting-identity switcher in the header.
 *
 * It renders four states and never a blank box: a spinner while the staff list
 * is in flight, an error banner with a retry when the call failed, an empty
 * state when there is nobody to choose, and otherwise a labelled native select.
 * A header that shows nothing while loading looks exactly like a header that
 * found nobody, and the person in front of it cannot tell which.
 *
 * It reads the identity context and nothing else - no fetch lives here, and
 * choosing a name is only ever a request to the context, which pushes the id
 * into the typed client so the server resolves the role on the next call.
 */

import type { ChangeEvent, ReactElement } from 'react';
import { useId } from 'react';

import { useIdentity } from '@/lib/identity';
import { Banner, Button, EmptyState, Field, Select, Spinner } from '@/lib/ui';

/** The header control for picking which seeded employee to act as. */
export function UserSwitcher(): ReactElement {
  const { employees, status, error, actingId, setActingId, reload } = useIdentity();
  const controlId = useId();

  if (status === 'loading') {
    return (
      <div className="app-header__identity">
        <Spinner label="Loading the staff list" />
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="app-header__identity">
        <Banner
          variant="error"
          actions={
            <Button variant="ghost" onClick={reload}>
              Try again
            </Button>
          }
        >
          {error ?? 'The staff list could not be loaded.'}
        </Banner>
      </div>
    );
  }

  if (status === 'empty' || employees.length === 0) {
    return (
      <div className="app-header__identity">
        <EmptyState
          title="No employees to act as"
          description="The staff list came back empty, so there is nobody to switch to."
        />
      </div>
    );
  }

  const handleChange = (event: ChangeEvent<HTMLSelectElement>): void => {
    const { value } = event.target;
    setActingId(value === '' ? null : Number(value));
  };

  return (
    <div className="app-header__identity">
      <Field controlId={controlId} label="Acting as">
        <Select
          id={controlId}
          aria-label="Acting as"
          value={actingId === null ? '' : String(actingId)}
          onChange={handleChange}
        >
          <option value="">Choose an employee</option>
          {employees.map((employee) => (
            <option key={employee.id} value={String(employee.id)}>
              {employee.displayName}
            </option>
          ))}
        </Select>
      </Field>
    </div>
  );
}

export default UserSwitcher;
