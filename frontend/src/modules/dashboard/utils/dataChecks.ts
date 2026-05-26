import type { DashboardSummary } from '@/api/types';

export function hasBudgetData(
  summary: DashboardSummary,
): summary is DashboardSummary & { budgetUsage: NonNullable<DashboardSummary['budgetUsage']> } {
  return (summary.budgetUsage?.length ?? 0) > 0;
}

export function hasSavingsGoals(
  summary: DashboardSummary,
): summary is DashboardSummary & { activeGoals: NonNullable<DashboardSummary['activeGoals']> } {
  return (summary.activeGoals?.length ?? 0) > 0;
}
