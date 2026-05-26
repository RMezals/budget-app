import type { MonthlyReport } from '@/api/types';
import { monthLabel } from './reportUtils';

function toCsvRows(rows: string[][]): string {
  return rows.map((r) => r.map((cell) => `"${cell.replace(/"/g, '""')}"`).join(',')).join('\n');
}

export function exportReportCsv(report: MonthlyReport): void {
  const label = monthLabel(report.year ?? 0, report.month ?? 0);
  const sections: string[] = [];

  sections.push(`Monthly Financial Report — ${label}`);
  sections.push('');

  sections.push('Summary');
  sections.push(
    toCsvRows([
      ['Total Income', String(report.totalIncome ?? 0)],
      ['Total Expenses', String(report.totalExpenses ?? 0)],
      ['Net Savings', String(report.netSavings ?? 0)],
    ]),
  );
  sections.push('');

  sections.push('Expenses by Category');
  sections.push(toCsvRows([['Category', 'Amount']]));
  sections.push(
    toCsvRows(
      Object.entries(report.expensesByCategory ?? {}).map(([cat, amt]) => [cat, String(amt)]),
    ),
  );
  sections.push('');

  sections.push('Income by Category');
  sections.push(toCsvRows([['Category', 'Amount']]));
  sections.push(
    toCsvRows(
      Object.entries(report.incomeByCategory ?? {}).map(([cat, amt]) => [cat, String(amt)]),
    ),
  );
  sections.push('');

  sections.push('Savings Contributions');
  sections.push(toCsvRows([['Goal', 'Deposited', 'Withdrawn', 'Net', 'Transactions']]));
  sections.push(
    toCsvRows(
      (report.savingsContributions ?? []).map((c) => [
        c.goalName ?? '',
        String(c.totalDeposited ?? 0),
        String(c.totalWithdrawn ?? 0),
        String(c.netContribution ?? 0),
        String(c.contributionCount ?? 0),
      ]),
    ),
  );
  sections.push('');

  const pc = report.portfolioChange;
  sections.push('Portfolio Change');
  sections.push(
    toCsvRows([
      ['Start of Month Value', String(pc?.startValue ?? 0)],
      ['End of Month Value', String(pc?.endValue ?? 0)],
      ['Change', String(pc?.change ?? 0)],
      ['Change %', String(pc?.changePercent ?? 0)],
    ]),
  );

  const csv = sections.join('\n');
  const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `report-${report.year}-${String(report.month).padStart(2, '0')}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}
