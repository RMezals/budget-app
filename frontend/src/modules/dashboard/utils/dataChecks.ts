import type { DashboardSummary } from '@/api/types';

// Type guard — narrows summary so TypeScript knows budgetUsage is a non-empty array
// Used before rendering budget progress bars to avoid mapping over null/undefined
export function hasBudgetData(
  summary: DashboardSummary,
): summary is DashboardSummary & { budgetUsage: NonNullable<DashboardSummary['budgetUsage']> } {
  return (summary.budgetUsage?.length ?? 0) > 0;
}

// Type guard — narrows summary so TypeScript knows activeGoals is a non-empty array
// Used before rendering the savings goal list to avoid mapping over null/undefined
export function hasSavingsGoals(
  summary: DashboardSummary,
): summary is DashboardSummary & { activeGoals: NonNullable<DashboardSummary['activeGoals']> } {
  return (summary.activeGoals?.length ?? 0) > 0;
}
