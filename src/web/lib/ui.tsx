/**
 * The design-system primitives.
 *
 * Presentational only: nothing in this module fetches, stores or decides
 * anything. One place owns what a button, a field, a banner, a spinner and an
 * empty state look like, so two screens cannot quietly disagree about how the
 * same state is shown - and a state that has no primitive is a state nobody
 * styled.
 *
 * Every element here carries its semantics itself (a `<button>` with an
 * explicit `type`, a `<label>` bound to its control, `role="status"` on the
 * spinner) rather than relying on a lint rule or a reviewer to notice.
 */

import type {
  ButtonHTMLAttributes,
  ReactElement,
  ReactNode,
  SelectHTMLAttributes,
} from 'react';

/** Joins class names, dropping the ones that are absent. */
function classNames(...values: Array<string | false | null | undefined>): string {
  return values.filter((value): value is string => Boolean(value)).join(' ');
}

export type ButtonVariant = 'primary' | 'ghost';

export interface ButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'type'> {
  /** Visual weight. 'ghost' is the quiet one used inside banners. */
  variant?: ButtonVariant;
  /**
   * Always spelled out. A `<button>` with no type submits the form it happens
   * to sit in, which is never what a retry control means.
   */
  type?: 'button' | 'submit' | 'reset';
}

/** The one button. */
export function Button({
  variant = 'primary',
  type = 'button',
  className,
  children,
  ...rest
}: ButtonProps): ReactElement {
  return (
    <button
      type={type}
      className={classNames('ds-button', variant === 'ghost' && 'ds-button--ghost', className)}
      {...rest}
    >
      {children}
    </button>
  );
}

export interface FieldProps {
  /** The id of the control this field labels; the label points at it. */
  controlId: string;
  label: string;
  hint?: string;
  children: ReactNode;
}

/** A label bound to its control by id, plus an optional hint. */
export function Field({ controlId, label, hint, children }: FieldProps): ReactElement {
  return (
    <div className="ds-field">
      <label className="ds-label" htmlFor={controlId}>
        {label}
      </label>
      {children}
      {hint ? (
        <p className="ds-field__hint" id={`${controlId}-hint`}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}

export type SelectProps = SelectHTMLAttributes<HTMLSelectElement>;

/**
 * A native select, deliberately. The browser already makes it reachable by
 * keyboard and announced by a screen reader; a div dressed as a dropdown would
 * have to re-earn both.
 */
export function Select({ className, children, ...rest }: SelectProps): ReactElement {
  return (
    <select className={classNames('ds-select', className)} {...rest}>
      {children}
    </select>
  );
}

export type BannerVariant = 'info' | 'error';

export interface BannerProps {
  variant?: BannerVariant;
  title?: string;
  /** Controls shown beside the message, such as a retry button. */
  actions?: ReactNode;
  children: ReactNode;
}

/** A short message about the state of the page, optionally with actions. */
export function Banner({
  variant = 'info',
  title,
  actions,
  children,
}: BannerProps): ReactElement {
  return (
    <div
      className={classNames('ds-banner', variant === 'error' ? 'ds-banner--error' : 'ds-banner--info')}
      role={variant === 'error' ? 'alert' : 'status'}
    >
      <div className="ds-banner__body">
        {title ? <p className="ds-banner__title">{title}</p> : null}
        <p className="ds-banner__message">{children}</p>
      </div>
      {actions ? <div className="ds-banner__actions">{actions}</div> : null}
    </div>
  );
}

export interface SpinnerProps {
  /** Announced to assistive technology; visually hidden. */
  label?: string;
}

/** The in-flight indicator. Silence is indistinguishable from emptiness. */
export function Spinner({ label = 'Loading' }: SpinnerProps): ReactElement {
  return (
    <span className="ds-spinner" role="status">
      <span className="ds-spinner__dot" aria-hidden="true" />
      <span className="ds-visually-hidden">{label}</span>
    </span>
  );
}

export interface EmptyStateProps {
  title: string;
  description?: string;
  actions?: ReactNode;
}

/** Says that there is nothing, which is not the same as saying nothing. */
export function EmptyState({ title, description, actions }: EmptyStateProps): ReactElement {
  return (
    <div className="ds-empty">
      <p className="ds-empty__title">{title}</p>
      {description ? <p className="ds-empty__body">{description}</p> : null}
      {actions ? <div className="ds-empty__actions">{actions}</div> : null}
    </div>
  );
}
