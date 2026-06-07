import type {
  getApiAssetsAllocationResponseItem,
  getApiAssetsGainLossResponse,
  getApiAssetsPerformanceResponseItem,
  getApiAssetsResponseItem,
  getApiAssetsSummaryResponseItem,
  getApiAuthProfileResponse,
  getApiBudgetsResponseItem,
  getApiBudgetsUsageResponseItem,
  getApiDashboardResponse,
  getApiDashboardSpendingTrendResponseItem,
  getApiGoalsGoalIdContributionsResponseItem,
  getApiGoalsIdProjectionResponse,
  getApiGoalsResponseItem,
  getApiLiabilitiesIdResponse,
  getApiNetworthHistoryResponseItem,
  getApiNetworthResponse,
  getApiReportsMonthlyResponse,
  getApiTransactionsCategoriesResponse,
  getApiTransactionsResponseItem,
  postApiAdvisorAnalyseBody,
  postApiAdvisorAnalyseResponse,
  postApiAssetsBody,
  postApiAssetsIdPricesBody,
  postApiGoalsBody,
  postApiGoalsGoalIdContributionsBody,
  postApiGoalsIdAbandonBody,
  postApiLiabilitiesBody,
  postApiLiabilitiesIdAmountsBody,
  postApiTransactionsBody,
  putApiAssetsIdBody,
  putApiAuthProfileBody,
  putApiBudgetsBody,
  putApiGoalsGoalIdContributionsIdBody,
  putApiGoalsIdBody,
  putApiGoalsIdStatusBody,
  putApiLiabilitiesIdBody,
} from '@/api/generated/schemas';
/**
 * TypeScript types derived from the auto-generated Zod schemas in src/api/generated/schemas.ts.
 * Do not edit manually — update swagger.json and run `npm run generate:schemas`.
 */
import type * as zod from 'zod';

export type { SavingsGoalProgress } from '@/api/schemas';

export type DashboardSummary = zod.infer<typeof getApiDashboardResponse>;
export type BudgetUsage = NonNullable<DashboardSummary['budgetUsage']>[number];
export type GoalProgress = NonNullable<DashboardSummary['activeGoals']>[number];
export type SpendingTrendPoint = zod.infer<typeof getApiDashboardSpendingTrendResponseItem>;

export type AdvisorResult = zod.infer<typeof postApiAdvisorAnalyseResponse>;
export type AnalyseRequest = zod.infer<typeof postApiAdvisorAnalyseBody>;
export type ErrorResult = { error?: string | null };

export type GoalStatus = 'Active' | 'Completed' | 'Paused' | 'Abandoned';
export type SavingsGoal = zod.infer<typeof getApiGoalsResponseItem>;
export type GoalContribution = zod.infer<typeof getApiGoalsGoalIdContributionsResponseItem>;
export type ProjectionResult = zod.infer<typeof getApiGoalsIdProjectionResponse>;
export type CreateGoalRequest = zod.infer<typeof postApiGoalsBody>;
export type UpdateGoalRequest = zod.infer<typeof putApiGoalsIdBody>;
export type AbandonGoalRequest = zod.infer<typeof postApiGoalsIdAbandonBody>;
export type UpdateStatusRequest = zod.infer<typeof putApiGoalsIdStatusBody>;
export type AddContributionRequest = zod.infer<typeof postApiGoalsGoalIdContributionsBody>;
export type UpdateContributionRequest = zod.infer<typeof putApiGoalsGoalIdContributionsIdBody>;

export type Asset = zod.infer<typeof getApiAssetsResponseItem>;
export type PriceEntry = NonNullable<Asset['price']>[number];
export type AssetSummary = zod.infer<typeof getApiAssetsSummaryResponseItem>;
export type AssetAllocation = zod.infer<typeof getApiAssetsAllocationResponseItem>;
export type MonthlyPerformance = zod.infer<typeof getApiAssetsPerformanceResponseItem>;
export type PortfolioGainLoss = zod.infer<typeof getApiAssetsGainLossResponse>;
export type CreateAssetRequest = zod.infer<typeof postApiAssetsBody>;
export type UpdateAssetRequest = zod.infer<typeof putApiAssetsIdBody>;
export type AddPriceRequest = zod.infer<typeof postApiAssetsIdPricesBody>;

export type Liability = zod.infer<typeof getApiLiabilitiesIdResponse>;
export type AmountEntry = NonNullable<Liability['amount']>[number];
export type NetWorthSnapshot = zod.infer<typeof getApiNetworthResponse>;
export type NetWorthHistoryPoint = zod.infer<typeof getApiNetworthHistoryResponseItem>;
export type CreateLiabilityRequest = zod.infer<typeof postApiLiabilitiesBody>;
export type UpdateLiabilityRequest = zod.infer<typeof putApiLiabilitiesIdBody>;
export type AddAmountRequest = zod.infer<typeof postApiLiabilitiesIdAmountsBody>;

export type Transaction = zod.infer<typeof getApiTransactionsResponseItem>;
export type Budget = zod.infer<typeof getApiBudgetsResponseItem>;
export type TransactionBudgetUsage = zod.infer<typeof getApiBudgetsUsageResponseItem>;
export type TransactionCategories = zod.infer<typeof getApiTransactionsCategoriesResponse>;
export type TransactionRequest = zod.infer<typeof postApiTransactionsBody>;
export type UpsertBudgetRequest = zod.infer<typeof putApiBudgetsBody>;

export type UpdateProfileRequest = zod.infer<typeof putApiAuthProfileBody>;
export type ProfileResponse = zod.infer<typeof getApiAuthProfileResponse>;

export type MonthlyReport = zod.infer<typeof getApiReportsMonthlyResponse>;
export type GoalContributionSummary = NonNullable<MonthlyReport['savingsContributions']>[number];
export type PortfolioChangeSummary = NonNullable<MonthlyReport['portfolioChange']>;
