// Converts a numeric year + month into a human-readable label like "January 2024"
// month is 1-based (1 = January), so we subtract 1 for the Date constructor which is 0-based
export function monthLabel(year: number, month: number): string {
  return new Date(year, month - 1, 1).toLocaleString('en-GB', { month: 'long', year: 'numeric' });
}
