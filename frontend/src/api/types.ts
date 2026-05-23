/**
 * API type definitions matching backend models
 *
 * IMPORTANT: These types are manually maintained to stay in sync with backend models.
 *
 * @see backend/BudgetApp.Api/Modules/Dashboard/Models/DashboardSummary.cs
 * @see backend/BudgetApp.Api/Modules/Dashboard/AdvisorController.cs
 *
 * Last synchronized: 2026-05-09
 *
 * TODO: Automate type generation from OpenAPI schema
 * - Option 1: Configure Orval to extract ProducesResponseType properly
 * - Option 2: Use NSwag instead of Orval for better .NET integration
 * - Option 3: Add CI validation tests that verify API responses match these types
 *
 * When updating backend models, remember to update these types!
 */

export interface DashboardSummary {
  netWorth: number;
  totalInvested: number;
  totalSaved: number;
  monthlyIncome: number;
  monthlyExpenses: number;
  budgetUsage: BudgetUsage[];
  activeGoals: GoalProgress[];
}

export interface BudgetUsage {
  category: string;
  limit: number;
  spent: number;
  remaining: number;
  usagePercent: number;
}

export interface GoalProgress {
  goalId: string;
  name: string;
  currentAmount: number;
  targetAmount: number;
  percentReached: number;
  projectedCompletion?: string | null;
}

export interface SavingsGoalProgress {
  id: string;
  userId: string;
  name: string;
  targetAmount: number;
  currentBalance: number;
  percentReached: number;
  amountRemaining: number;
  projectedCompletion?: string | null;
  status: 'Active' | 'Completed' | 'Paused' | 'Abandoned' | number;
  deadline: string;
  description?: string | null;
}

export interface SavingsGoal {
  id: string;
  userId: string;
  name: string;
  targetAmount: number;
  currentAmount: number;
  deadline: string;
  description?: string | null;
  status: 'Active' | 'Completed' | 'Paused' | 'Abandoned' | number;
}

export interface GoalContribution {
  id: string;
  goalId: string;
  userId: string;
  amount: number;
  date: string;
  note?: string | null;
  reason?: string | null;
  description?: string | null;
  balanceAfter: number;
}

export interface AdvisorResult {
  provider: string;
  tips: string;
}

export interface AnalyseRequest {
  provider?: string;
  goals?: string[];
}

export interface ErrorResult {
  error: string;
}

// Portfolio module types — synced with BudgetApp.Api/Modules/Portfolio

export interface Asset {
  id: string;
  userId: string;
  name: string;
  type: string;
  quantity: number;
  purchasePrice: number;
  purchaseDate: string;
  price: PriceEntry[];
}

export interface PriceEntry {
  value: number;
  date: string;
}

export interface AssetSummary {
  id: string;
  name: string;
  type: string;
  quantity: number;
  purchasePrice: number;
  currentPrice: number;
  currentValue: number;
  unrealisedGainLoss: number;
  unrealisedGainLossPercent: number;
}

export interface AssetAllocation {
  type: string;
  totalValue: number;
  allocationPercent: number;
}

export interface Liability {
  id: string;
  userId: string;
  name: string;
  type: string;
  amount: AmountEntry[];
}

export interface AmountEntry {
  value: number;
  date: string;
}

export interface NetWorthSnapshot {
  totalAssets: number;
  totalLiabilities: number;
  netWorth: number;
}

export interface NetWorthHistoryPoint {
  date: string;
  totalAssets: number;
  totalLiabilities: number;
  netWorth: number;
}

export interface MonthlyPerformance {
  month: string;
  startValue: number;
  endValue: number;
  gainLoss: number;
  gainLossPercent: number;
}

export interface PortfolioGainLoss {
  totalInvested: number;
  currentValue: number;
  totalGainLoss: number;
  totalGainLossPercent: number;
}
