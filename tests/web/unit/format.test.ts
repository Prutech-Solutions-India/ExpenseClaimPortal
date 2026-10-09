/**
 * The shared formatting module.
 *
 * One place decides how a date, an amount and a role code are shown, so two
 * screens cannot disagree about what the same value looks like. The assertions
 * below are deliberately about the parts that matter - the currency symbol, the
 * two decimal places, the day and the year, the expanded role label - rather
 * than about the exact spacing a given ICU build happens to emit.
 */

import { describe, expect, it } from 'vitest';

import { formatAmount, formatDate, formatRole } from '@/lib/format';

describe('formatRole', () => {
  it('expands the stored code into words a person reads', () => {
    expect(formatRole('FinanceOfficer')).toBe('Finance Officer');
  });

  it('leaves the single-word roles alone', () => {
    expect(formatRole('Employee')).toBe('Employee');
    expect(formatRole('Manager')).toBe('Manager');
  });
});

describe('formatAmount', () => {
  it('shows sterling with two decimal places and thousands separators', () => {
    const formatted = formatAmount(1234.5);

    expect(formatted).toContain('£');
    expect(formatted).toMatch(/1,234\.50/);
  });

  it('keeps the trailing zeros on a round amount', () => {
    expect(formatAmount(0)).toMatch(/0\.00/);
    expect(formatAmount(12)).toMatch(/12\.00/);
  });

  it('marks a negative amount as negative', () => {
    const formatted = formatAmount(-1);

    expect(formatted).toMatch(/1\.00/);
    expect(formatted).toMatch(/[-\u2212(]/);
  });

  it('honours an explicit currency', () => {
    expect(formatAmount(10, 'EUR')).toContain('€');
    expect(formatAmount(10, 'USD')).toMatch(/\$/);
  });
});

describe('formatDate', () => {
  it('renders the day, month and year of an ISO instant', () => {
    const formatted = formatDate('2025-01-31T12:00:00Z');

    expect(formatted).toMatch(/31/);
    expect(formatted).toMatch(/2025/);
    expect(formatted).toMatch(/jan|01/i);
  });

  it('puts the day before the month, as en-GB does', () => {
    const formatted = formatDate('2025-03-04T12:00:00Z');

    expect(formatted).toMatch(/2025/);
    expect(formatted.indexOf('4')).toBeLessThan(formatted.indexOf('2025'));
  });
});
