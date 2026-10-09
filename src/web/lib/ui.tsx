/**
 * The design-system primitives.
 *
 * Every screen builds from these, so loading, empty and error look the same
 * wherever they appear and an accessibility fix lands once rather than in each
 * copy. They are presentational only: no fetching, no routing, no state beyond
 * what the DOM already holds. Styling lives in `src/web/styles.css` behind the
 * `ds-` prefix - there is no CSS framework and no CSS-in-JS here.
 */

import type {
  ButtonHTMLAttributes,
  ReactElement,
  ReactNode,
  SelectHTMLAttributes,
} from 'react';

/** Joins class names, dropping anything absent. */
function classNames(...values: Array<string | false | null | undefined>): string {
  return values.filter((value): value is string => Boolean(value)).join(' ');
}

/** Visual weight of a {@link Button}. */
export type ButtonVariant = 'primary' | 'ghost';

/** Props accepted by {@link Button}. */
export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  /** Visual weight. Defaults to `primary`. */
  variant?: ButtonVariant;
}

/**
 * A button.
 *
 * `type` defaults to `button`: inside a form the browser's default of `submit`
 * turns an ordinary action into a page reload.
 */
export function Button({
  variant = 'primary',
  type = 'button',
  className,
  children,
  ...rest
}: ButtonProps): ReactElement {
  return (
    <button
      // eslint-disable-next-line react/button-has-type
      type={type}
      className={classNames('ds-button', variant === 'ghost' && 'ds-button--ghost', className)}
      {...rest}
    >
      {children}
    </button>
  );
}

/** Props accepted by {@link Field}. */
export interface FieldProps {
  /** Id of the control this field labels. The control must carry the same id. */
  id: string;
  /** Visible label text. */
  label: string;
  /** Optional hint rendered beneath the control. */
  hint?: string;
  /** The control itself. */
  children: ReactNode;
}

/**
 * A labelled form control.
 *
 * The label is associated with the control through `htmlFor`/`id` rather than
 * by wrapping, so clicking the label focuses the control and assistive
 * technology reads a name for it.
 */
export function Field({ id, label, hint, children }: FieldProps): ReactElement {
  return (
    <div className="ds-field">
      <label className="ds-label" htmlFor={id}>
        {label}
      </label>
      {children}
      {hint !== undefined && hint !== '' ? (
        <p className="ds-field__hint" id={`${id}-hint`}>
          {hint}
        </p>
      ) : null}
    </div>
  );
}

/** Props accepted by {@link Select}. */
export type SelectProps = SelectHTMLAttributes<HTMLSelectElement>;

/**
 * A native `<select>`.
 *
 * Deliberately native: a div-based menu has to re-implement focus, typeahead
 * and arrow-key movement, and usually re-implements only some of it.
 */
export function Select({ className, children, ...rest }: SelectProps): ReactElement {
  return (
    <select className={classNames('ds-select', className)} {...rest}>
      {children}
    </select>
  );
}

/** Tone of a {@link Banner}. */
export type BannerVariant = 'info' | 'error';

/** Props accepted by {@link Banner}. */
export interface BannerProps {
  /** Tone. Defaults to `info`. */
  variant?: BannerVariant;
  /** Optional heading line. */
  title?: string;
  /** Body content, including any action buttons. */
  children?: ReactNode;
}

/**
 * A message about the page as a whole.
 *
 * The error variant is announced (`role="alert"`); the informational variant is
 * not, so a prompt that is simply part of the page does not interrupt whatever
 * a screen-reader user is doing.
 */
export function Banner({ variant = 'info', title, children }: BannerProps): ReactElement {
  return (
    <div
      className={classNames('ds-banner', variant === 'error' && 'ds-banner--error')}
      role={variant === 'error' ? 'alert' : undefined}
    >
      {title !== undefined && title !== '' ? <p className="ds-banner__title">{title}</p> : null}
      {children !== undefined && children !== null ? (
        <div className="ds-banner__body">{children}</div>
      ) : null}
    </div>
  );
}

/** Props accepted by {@link Spinner}. */
export interface SpinnerProps {
  /** Text announced while the spinner is shown. Defaults to `Loading`. */
  label?: string;
}

/**
 * A busy indicator.
 *
 * Carries `role="status"` with visually hidden text, because a spinning shape
 * with no accessible name tells a screen-reader user nothing at all.
 */
export function Spinner({ label = 'Loading' }: SpinnerProps): ReactElement {
  return (
    <span className="ds-spinner" role="status">
      <span className="ds-spinner__indicator" aria-hidden="true" />
      <span className="ds-visually-hidden">{label}</span>
    </span>
  );
}

/** Props accepted by {@link EmptyState}. */
export interface EmptyStateProps {
  /** Short statement of what is missing. */
  title: string;
  /** Optional explanation of why, or what to do next. */
  description?: string;
  /** Optional actions. */
  children?: ReactNode;
}

/**
 * The "there is nothing here" state.
 *
 * Distinct from loading and from failure on purpose: an empty list rendered as
 * a blank area is indistinguishable from a broken one.
 */
export function EmptyState({ title, description, children }: EmptyStateProps): ReactElement {
  return (
    <div className="ds-empty">
      <p className="ds-empty__title">{title}</p>
      {description !== undefined && description !== '' ? (
        <p className="ds-empty__description">{description}</p>
      ) : null}
      {children !== undefined && children !== null ? (
        <div className="ds-empty__actions">{children}</div>
      ) : null}
    </div>
  );
}
