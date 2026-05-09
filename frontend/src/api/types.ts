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
