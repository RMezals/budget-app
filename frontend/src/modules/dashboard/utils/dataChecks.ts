import type { DashboardSummary } from '../../../api/types';

/**
 * Checks if the user has any budget data
 */
export function hasBudgetData(summary: DashboardSummary): boolean {
  return summary.budgetUsage.length > 0;
}

/**
 * Checks if the user has any savings goals
 */
export function hasSavingsGoals(summary: DashboardSummary): boolean {
  return summary.activeGoals.length > 0;
}
