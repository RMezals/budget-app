import { useEffect, useState } from 'react'
import { apiFetch } from '../../api/client'

interface DashboardSummary {
  netWorth: number
  totalInvested: number
  totalSaved: number
  monthlyIncome: number
  monthlyExpenses: number
  budgetUsage: { category: string; limit: number; spent: number; remaining: number; usagePercent: number }[]
  activeGoals: { goalId: string; name: string; currentAmount: number; targetAmount: number; percentReached: number }[]
}

export default function DashboardPage() {
  const [summary, setSummary] = useState<DashboardSummary | null>(null)

  useEffect(() => {
    apiFetch<DashboardSummary>('/api/dashboard').then(setSummary).catch(console.error)
  }, [])

  if (!summary) return <p>Loading...</p>

  return (
    <div>
      <h1>Dashboard</h1>
      <p>Net Worth: {summary.netWorth}</p>
      <p>Monthly Income: {summary.monthlyIncome} / Expenses: {summary.monthlyExpenses}</p>
      {/* TODO: charts and full UI */}
    </div>
  )
}
