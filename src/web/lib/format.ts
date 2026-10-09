/**
 * Shared presentation formatting.
 *
 * Every date, amount and role label the UI renders is produced here, so the
 * same value never appears two ways on two screens. The locale is pinned to
 * 'en-GB' on purpose: a pilot that formats according to whichever machine the
 * browser happens to run on produces screenshots nobody can compare and tests
 * that pass only on the author's laptop.
 *
 * This module is presentational only - it holds no state and makes no network
 * call.
 */

/** Locale every formatter in this module uses. */
export const LOCALE = 'en-GB';

/** Currency used when a caller does not name one. */
export const DEFAULT_CURRENCY = 'GBP';

/** Rendered in place of a value that cannot be formatted. */
export const EMPTY_PLACEHOLDER = '—';

const dateFormatter = new Intl.DateTimeFormat(LOCALE, {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
});

const currencyFormatters = new Map<string, Intl.NumberFormat>();

/** Returns (and caches) the number formatter for a currency code. */
function currencyFormatter(currency: string): Intl.NumberFormat {
  const key = currency.toUpperCase();
  const existing = currencyFormatters.get(key);
  if (existing) {
    return existing;
  }

  const created = new Intl.NumberFormat(LOCALE, {
    style: 'currency',
    currency: key,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  });
  currencyFormatters.set(key, created);
  return created;
}

/**
 * Formats an ISO-8601 date (or a Date) for display, for example `14 Mar 2025`.
 *
 * @param value An ISO-8601 string, a Date, or a nullish value.
 * @returns The formatted date, or the placeholder when the value is missing or unparseable.
 */
export function formatDate(value: string | Date | null | undefined): string {
  if (value === null || value === undefined || value === '') {
    return EMPTY_PLACEHOLDER;
  }

  const date = value instanceof Date ? value : new Date(value);

  if (Number.isNaN(date.getTime())) {
    return EMPTY_PLACEHOLDER;
  }

  return dateFormatter.format(date);
}

/**
 * Formats a monetary amount, for example `£1,234.50`.
 *
 * @param value The amount, in major currency units.
 * @param currency ISO-4217 currency code. Defaults to {@link DEFAULT_CURRENCY}.
 * @returns The formatted amount, or the placeholder when the value is not a finite number.
 */
export function formatAmount(
  value: number | null | undefined,
  currency: string = DEFAULT_CURRENCY,
): string {
  if (typeof value !== 'number' || !Number.isFinite(value)) {
    return EMPTY_PLACEHOLDER;
  }

  return currencyFormatter(currency).format(value);
}

/** Role codes the server uses, mapped to the words a person reads. */
const ROLE_LABELS: Record<string, string> = {
  Employee: 'Employee',
  Manager: 'Manager',
  FinanceOfficer: 'Finance Officer',
};

/**
 * Turns a server role code into a human label - `FinanceOfficer` reads as
 * `Finance Officer`.
 *
 * An unrecognised code is split on its capitals rather than dropped, so a role
 * added on the server shows up as something readable instead of disappearing
 * from the screen.
 *
 * @param role The role code, for example `FinanceOfficer`.
 * @returns The display label for that role.
 */
export function formatRole(role: string | null | undefined): string {
  if (role === null || role === undefined || role === '') {
    return EMPTY_PLACEHOLDER;
  }

  const known = ROLE_LABELS[role];
  if (known !== undefined) {
    return known;
  }

  return role.replace(/([a-z0-9])([A-Z])/g, '$1 $2');
}
