/**
 * Empty state messages for dashboard sections
 */
export const EMPTY_STATE_MESSAGES = {
  BUDGET_USAGE: {
    icon: '📊',
    title: 'No Budgets Set',
    message: 'Create your first budget to track spending by category.',
  },
  SAVINGS_GOALS: {
    icon: '🎯',
    title: 'No Savings Goals',
    message: 'Set a savings goal to track your progress toward financial targets.',
  },
  SPENDING_TREND: {
    icon: '📈',
    title: 'No Spending Data',
    message: 'Add expense transactions to see your monthly spending trend.',
  },
} as const;
