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
  note: z.string().nullable().optional(),
  reason: z.string().nullable().optional(),
  description: z.string().nullable().optional(),
  balanceAfter: z.number(),
});

// Advisor Result Schema
export const AdvisorResultSchema = z.object({
  provider: z.string(),
  tips: z.string(),
});

// Error Result Schema
export const ErrorResultSchema = z.object({
  error: z.string(),
});
