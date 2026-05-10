const DANGER_THRESHOLD = 90;
const WARNING_THRESHOLD = 70;

/**
 * Determines the Bootstrap color class for a budget progress bar based on usage percentage
 *
 * @param usagePercent - Budget usage percentage (0-100+)
 * @returns Bootstrap CSS class for the progress bar color
 */
export function getBudgetProgressColor(usagePercent: number): string {
  if (usagePercent >= DANGER_THRESHOLD) {
    return 'bg-danger';
  }

  if (usagePercent >= WARNING_THRESHOLD) {
    return 'bg-warning';
  }

  return 'bg-success';
}
