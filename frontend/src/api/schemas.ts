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

// Advisor Result Schema
export const AdvisorResultSchema = z.object({
  provider: z.string(),
  tips: z.string(),
});

// Error Result Schema
export const ErrorResultSchema = z.object({
  error: z.string(),
});
