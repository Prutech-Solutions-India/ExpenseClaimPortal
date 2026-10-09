/**
 * The header control that chooses which seeded employee the browser is acting
 * as.
 *
 * It renders the four states the staff list can be in - loading, failed, empty
 * and populated - from the design-system primitives, and it never calls fetch
 * itself: the list, the selection and the resolved identity all come from
 * {@link useIdentity}.
 *
 * Choosing here only changes which id is sent. The server decides what that id
 * is allowed to do, so hiding or showing this control is a convenience and
 * never a protection.
 */

import type { ChangeEvent, ReactElement } from 'react';

import { formatRole } from '@/lib/format';
import { useIdentity } from '@/lib/identity';
import { Banner, Button, EmptyState, Field, Select, Spinner } from '@/lib/ui';

/** Id shared by the label and the select, which is what gives the control its accessible name. */
const SELECT_ID = 'acting-employee-select';

/** Visible (and accessible) name of the control. */
const SELECT_LABEL = 'Acting as';

/** Value of the placeholder option, meaning "no identity selected". */
const NO_SELECTION = '';

/** Renders the acting-identity switcher for the application header. */
export function UserSwitcher(): ReactElement {
  const { employees, status, error, actingId, setActingId, reload } = useIdentity();

  function handleChange(event: ChangeEvent<HTMLSelectElement>): void {
    const { value } = event.target;

    if (value === NO_SELECTION) {
      setActingId(null);
      return;
    }

    const parsed = Number.parseInt(value, 10);
    setActingId(Number.isNaN(parsed) ? null : parsed);
  }

  if (status === 'loading') {
    return (
      <div className="app-header__identity">
        <Spinner label="Loading employees" />
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="app-header__identity">
        <Banner variant="error" title="Could not load employees">
          <p>{error ?? 'The staff list could not be loaded.'}</p>
          <Button variant="ghost" onClick={reload}>
            Retry
          </Button>
        </Banner>
      </div>
    );
  }

  if (status === 'empty') {
    return (
      <div className="app-header__identity">
        <EmptyState
          title="No employees to act as"
          description="The staff list is empty, so there is no identity to select."
        >
          <Button variant="ghost" onClick={reload}>
            Refresh
          </Button>
        </EmptyState>
      </div>
    );
  }

  return (
    <div className="app-header__identity">
      <Field id={SELECT_ID} label={SELECT_LABEL}>
        <Select
          id={SELECT_ID}
          name="actingEmployeeId"
          aria-label={SELECT_LABEL}
          value={actingId === null ? NO_SELECTION : String(actingId)}
          onChange={handleChange}
        >
          <option value={NO_SELECTION}>Select an employee</option>
          {employees.map((employee) => (
            <option key={employee.id} value={String(employee.id)}>
              {`${employee.displayName} (${formatRole(employee.role)})`}
            </option>
          ))}
        </Select>
      </Field>
    </div>
  );
}

export default UserSwitcher;
