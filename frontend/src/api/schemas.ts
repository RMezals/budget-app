import { z } from 'zod';

// Budget Usage Schema
export const BudgetUsageSchema = z.object({
  category: z.string(),
  limit: z.number(),
  spent: z.number(),
  remaining: z.number(),
  usagePercent: z.number(),
});

// Goal Progress Schema
export const GoalProgressSchema = z.object({
  goalId: z.string(),
  name: z.string(),
  currentAmount: z.number(),
  targetAmount: z.number(),
  percentReached: z.number(),
  projectedCompletion: z.string().nullable().optional(),
});

// Dashboard Summary Schema
export const DashboardSummarySchema = z.object({
  netWorth: z.number(),
  totalInvested: z.number(),
  totalSaved: z.number(),
  monthlyIncome: z.number(),
  monthlyExpenses: z.number(),
  budgetUsage: z.array(BudgetUsageSchema),
  activeGoals: z.array(GoalProgressSchema),
});

export const SavingsGoalProgressSchema = z.object({
  id: z.string(),
  userId: z.string(),
  name: z.string(),
  targetAmount: z.number(),
  currentBalance: z.number(),
  percentReached: z.number(),
  amountRemaining: z.number(),
  projectedCompletion: z.string().nullable().optional(),
  status: z.union([z.enum(['Active', 'Completed', 'Paused', 'Abandoned']), z.number()]),
  deadline: z.string(),
  description: z.string().nullable().optional(),
});

export const SavingsGoalProgressListSchema = z.array(SavingsGoalProgressSchema);

export type SavingsGoalProgress = z.infer<typeof SavingsGoalProgressSchema>;

export const SavingsGoalSchema = z.object({
  id: z.string(),
  userId: z.string(),
  name: z.string(),
  targetAmount: z.number(),
  currentAmount: z.number(),
  deadline: z.string(),
  description: z.string().nullable().optional(),
  status: z.union([z.enum(['Active', 'Completed', 'Paused', 'Abandoned']), z.number()]),
});

export const GoalContributionSchema = z.object({
  id: z.string(),
  goalId: z.string(),
  userId: z.string(),
  amount: z.number(),
  date: z.string(),
  // note: z.string().nullable().optional(),
  reason: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  balanceAfter: z.number(),
});

export const GoalContributionListSchema = z.array(GoalContributionSchema);

// Spending Trend
export const SpendingTrendPointSchema = z.object({
  month: z.string(),
  expenses: z.record(z.string(), z.number()),
});

export const SpendingTrendSchema = z.array(SpendingTrendPointSchema);

// Advisor Result Schema
export const AdvisorResultSchema = z.object({
  provider: z.string(),
  tips: z.string(),
});

// Error Result Schema
export const ErrorResultSchema = z.object({
  error: z.string(),
});

// Transactions module schemas

export const TransactionSchema = z.object({
  id: z.string(),
  userId: z.string(),
  amount: z.number(),
  date: z.string(),
  category: z.string(),
  description: z.string().nullable().optional(),
});

export const TransactionListSchema = z.array(TransactionSchema);

export const BudgetSchema = z.object({
  id: z.string(),
  userId: z.string(),
  category: z.string(),
  date: z.string(),
  limitAmount: z.number(),
});

export const BudgetListSchema = z.array(BudgetSchema);

export const TransactionBudgetUsageSchema = z.object({
  category: z.string(),
  limit: z.number(),
  spent: z.number(),
  remaining: z.number(),
  usagePercent: z.number(),
});

export const TransactionBudgetUsageListSchema = z.array(TransactionBudgetUsageSchema);

export const TransactionCategoriesSchema = z.object({
  expense: z.array(z.string()),
  income: z.array(z.string()),
});

// Portfolio schemas
export const PortfolioGainLossSchema = z.object({
  totalInvested: z.number().optional(),
  currentValue: z.number().optional(),
  totalGainLoss: z.number().optional(),
  totalGainLossPercent: z.number().optional(),
});

export const MonthlyPerformanceSchema = z.object({
  month: z.string().nullable().optional(),
  startValue: z.number().optional(),
  endValue: z.number().optional(),
  gainLoss: z.number().optional(),
  gainLossPercent: z.number().optional(),
});

export const MonthlyPerformanceListSchema = z.array(MonthlyPerformanceSchema);

export const NetWorthHistoryPointSchema = z.object({
  date: z.string().optional(),
  totalAssets: z.number().optional(),
  totalLiabilities: z.number().optional(),
  netWorth: z.number().optional(),
});

export const NetWorthHistoryPointListSchema = z.array(NetWorthHistoryPointSchema);

// Monthly Report schemas
export const GoalContributionSummarySchema = z.object({
  goalId: z.string(),
  goalName: z.string(),
  totalDeposited: z.number(),
  totalWithdrawn: z.number(),
  netContribution: z.number(),
  contributionCount: z.number(),
});

export const PortfolioChangeSummarySchema = z.object({
  startValue: z.number(),
  endValue: z.number(),
  change: z.number(),
  changePercent: z.number(),
});

export const MonthlyReportSchema = z.object({
  year: z.number(),
  month: z.number(),
  totalIncome: z.number(),
  totalExpenses: z.number(),
  netSavings: z.number(),
  expensesByCategory: z.record(z.string(), z.number()),
  incomeByCategory: z.record(z.string(), z.number()),
  savingsContributions: z.array(GoalContributionSummarySchema),
  portfolioChange: PortfolioChangeSummarySchema,
});
