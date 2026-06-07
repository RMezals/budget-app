import { getApiGoalsResponseItem } from '@/api/generated/schemas';
/**
 * Re-exports auto-generated Zod schemas from src/api/generated/schemas.ts under ergonomic names.
 * Do not define schemas here manually — update swagger.json and run `npm run generate:schemas`.
 */
import type * as zod from 'zod';

export type SavingsGoalProgress = zod.infer<typeof getApiGoalsResponseItem>;

export { getApiGoalsResponseItem as SavingsGoalProgressSchema };

export {
  getApiDashboardResponse as DashboardSummarySchema,
  getApiDashboardSpendingTrendResponse as SpendingTrendSchema,
  postApiAdvisorAnalyseResponse as AdvisorResultSchema,
  getApiGoalsResponse as SavingsGoalProgressListSchema,
  getApiGoalsGoalIdContributionsResponse as GoalContributionListSchema,
  getApiGoalsGoalIdContributionsResponseItem as GoalContributionSchema,
  getApiBudgetsResponse as BudgetListSchema,
  getApiBudgetsResponseItem as BudgetSchema,
  getApiBudgetsUsageResponse as TransactionBudgetUsageListSchema,
  getApiBudgetsUsageResponseItem as TransactionBudgetUsageSchema,
  getApiTransactionsResponse as TransactionListSchema,
  getApiTransactionsResponseItem as TransactionSchema,
  getApiTransactionsCategoriesResponse as TransactionCategoriesSchema,
  getApiAssetsGainLossResponse as PortfolioGainLossSchema,
  getApiAssetsPerformanceResponse as MonthlyPerformanceListSchema,
  getApiAssetsPerformanceResponseItem as MonthlyPerformanceSchema,
  getApiNetworthHistoryResponse as NetWorthHistoryPointListSchema,
  getApiNetworthHistoryResponseItem as NetWorthHistoryPointSchema,
  getApiReportsMonthlyResponse as MonthlyReportSchema,
} from '@/api/generated/schemas';
