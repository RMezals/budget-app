// Percentage thresholds that determine progress bar colour (red/yellow/green)
const DANGER_THRESHOLD = 90;
const WARNING_THRESHOLD = 70;

// Returns a Bootstrap background colour class based on how much of a budget has been used
export function getBudgetProgressColor(usagePercent: number): string {
  // At 90% or above the budget is nearly exhausted — show red to alert the user
  if (usagePercent >= DANGER_THRESHOLD) {
    return 'bg-danger';
  }

  // Between 70% and 89% shows a yellow warning so the user can start moderating spending
  if (usagePercent >= WARNING_THRESHOLD) {
    return 'bg-warning';
  }

  // Below 70% the budget is healthy — show green
  return 'bg-success';
}
