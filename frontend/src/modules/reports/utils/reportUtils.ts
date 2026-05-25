export function monthLabel(year: number, month: number): string {
  return new Date(year, month - 1, 1).toLocaleString('en-GB', { month: 'long', year: 'numeric' });
}
