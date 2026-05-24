import type { components } from './openapi.gen';

type S = components['schemas'];

export type DashboardSummary = S['DashboardSummary'];
export type BudgetUsage = S['BudgetUsage'];
export type GoalProgress = S['GoalProgress'];
export type SpendingTrendPoint = S['SpendingTrendPoint'];

export type SavingsGoal = S['SavingsGoal'];
export type SavingsGoalProgress = S['GoalProgressDto'];
export type GoalContribution = S['GoalContribution'];
export type GoalStatus = S['GoalStatus'];
export type ProjectionResult = S['ProjectionResult'];

export type Asset = S['Asset'];
export type PriceEntry = S['PriceEntry'];
export type AssetSummary = S['AssetSummary'];
export type AssetAllocation = S['AssetAllocation'];
export type MonthlyPerformance = S['MonthlyPerformance'];
export type PortfolioGainLoss = S['PortfolioGainLoss'];

export type Liability = S['Liability'];
export type AmountEntry = S['AmountEntry'];
export type NetWorthSnapshot = S['NetWorthSnapshot'];
export type NetWorthHistoryPoint = S['NetWorthHistoryPoint'];

export type Transaction = S['Transaction'];
export type Budget = S['Budget'];
export type TransactionBudgetUsage = S['BudgetUsageResponse'];
export type TransactionCategories = S['TransactionCategoriesResponse'];

export type AdvisorResult = S['AdvisorResult'];
export type AnalyseRequest = S['AnalyseRequest'];
export type ErrorResult = S['ErrorResult'];

export type ProfileResponse = S['ProfileResponse'];

export type CreateAssetRequest = S['CreateAssetRequest'];
export type UpdateAssetRequest = S['UpdateAssetRequest'];
export type AddPriceRequest = S['AddPriceRequest'];
export type CreateLiabilityRequest = S['CreateLiabilityRequest'];
export type UpdateLiabilityRequest = S['UpdateLiabilityRequest'];
export type AddAmountRequest = S['AddAmountRequest'];
export type CreateGoalRequest = S['CreateGoalRequest'];
export type UpdateGoalRequest = S['UpdateGoalRequest'];
export type AbandonGoalRequest = S['AbandonGoalRequest'];
export type UpdateStatusRequest = S['UpdateStatusRequest'];
export type AddContributionRequest = S['AddContributionRequest'];
export type TransactionRequest = S['TransactionRequest'];
export type UpsertBudgetRequest = S['UpsertBudgetRequest'];
export type UpdateProfileRequest = S['UpdateProfileRequest'];
