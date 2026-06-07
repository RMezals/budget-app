import type { MonthlyReport } from '@/api/types';
import { jsPDF } from 'jspdf';
import { monthLabel } from '@/modules/reports/utils/reportUtils';

// Formats a raw number as a localised currency string for use in the PDF
function formatAmount(value: number, currency: string): string {
  return new Intl.NumberFormat('en-GB', { style: 'currency', currency }).format(value);
}

// Generates and triggers a PDF download of a monthly financial report using jsPDF
export function exportReportPdf(report: MonthlyReport, currency: string): void {
  const doc = new jsPDF();
  const label = monthLabel(report.year ?? 0, report.month ?? 0);
  const pageWidth = doc.internal.pageSize.getWidth();
  // y tracks the current vertical drawing position in mm; incremented after each element
  let y = 20;
  const leftMargin = 14;
  // colRight is where right-aligned values (amounts) are anchored
  const colRight = 130;

  // Draws a bold section heading followed by a horizontal divider line
  const section = (title: string) => {
    y += 4;
    doc.setFontSize(12);
    doc.setFont('helvetica', 'bold');
    doc.text(title, leftMargin, y);
    y += 2;
    doc.setDrawColor(200); // light grey divider
    doc.line(leftMargin, y, pageWidth - leftMargin, y);
    y += 6;
    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
  };

  // Draws a label on the left and a value right-aligned at colRight, then advances y
  const row = (label: string, value: string, indent = 0) => {
    doc.text(label, leftMargin + indent, y);
    doc.text(value, colRight, y, { align: 'right' });
    y += 6;
  };

  // Inserts a new page when content is near the bottom (270mm of an A4 page)
  const checkPageBreak = () => {
    if (y > 270) {
      doc.addPage();
      y = 20;
    }
  };

  // Page title
  doc.setFontSize(16);
  doc.setFont('helvetica', 'bold');
  doc.text(`Monthly Report — ${label}`, leftMargin, y);
  y += 14;

  // Summary section — income, expenses, and net savings
  section('Summary');
  row('Total Income', formatAmount(report.totalIncome ?? 0, currency));
  row('Total Expenses', formatAmount(report.totalExpenses ?? 0, currency));
  // Net savings is displayed in bold to emphasise it
  doc.setFont('helvetica', 'bold');
  row('Net Savings', formatAmount(report.netSavings ?? 0, currency));
  doc.setFont('helvetica', 'normal');

  // Expenses by category — sorted largest first so the biggest costs are prominent
  checkPageBreak();
  section('Expenses by Category');
  const expEntries = Object.entries(report.expensesByCategory ?? {}).sort((a, b) => b[1] - a[1]);
  if (expEntries.length === 0) {
    doc.setTextColor(150); // grey placeholder text
    doc.text('No expenses this month.', leftMargin, y);
    doc.setTextColor(0);
    y += 6;
  } else {
    for (const [cat, amt] of expEntries) {
      checkPageBreak();
      row(cat, formatAmount(amt, currency), 4);
    }
  }

  // Income by category — also sorted largest first
  checkPageBreak();
  section('Income by Category');
  const incEntries = Object.entries(report.incomeByCategory ?? {}).sort((a, b) => b[1] - a[1]);
  if (incEntries.length === 0) {
    doc.setTextColor(150);
    doc.text('No income this month.', leftMargin, y);
    doc.setTextColor(0);
    y += 6;
  } else {
    for (const [cat, amt] of incEntries) {
      checkPageBreak();
      row(cat, formatAmount(amt, currency), 4);
    }
  }

  // Savings contributions — each goal gets its own sub-block
  checkPageBreak();
  section('Savings Goal Contributions');
  const contribs = report.savingsContributions ?? [];
  if (contribs.length === 0) {
    doc.setTextColor(150);
    doc.text('No savings contributions this month.', leftMargin, y);
    doc.setTextColor(0);
    y += 6;
  } else {
    for (const c of contribs) {
      checkPageBreak();
      // Goal name as a sub-heading in bold
      doc.setFont('helvetica', 'bold');
      doc.text(c.goalName ?? '', leftMargin + 4, y);
      y += 5;
      doc.setFont('helvetica', 'normal');
      row('  Deposited', formatAmount(c.totalDeposited ?? 0, currency), 4);
      // Only show withdrawn row when there were actual withdrawals to avoid clutter
      if ((c.totalWithdrawn ?? 0) > 0) {
        row('  Withdrawn', formatAmount(c.totalWithdrawn ?? 0, currency), 4);
      }
      row('  Net Contribution', formatAmount(c.netContribution ?? 0, currency), 4);
      y += 2;
    }
  }

  // Portfolio change section — colour the change value green/red depending on direction
  checkPageBreak();
  section('Portfolio Change');
  const pc = report.portfolioChange;
  row('Start of Month Value', formatAmount(pc?.startValue ?? 0, currency));
  row('End of Month Value', formatAmount(pc?.endValue ?? 0, currency));
  const change = pc?.change ?? 0;
  // RGB: green (0, 120, 0) for gains, dark red (180, 0, 0) for losses
  doc.setTextColor(change >= 0 ? 0 : 180, change >= 0 ? 120 : 0, 0);
  row('Change', `${formatAmount(change, currency)} (${pc?.changePercent ?? 0}%)`);
  doc.setTextColor(0); // reset to black for anything after

  // Zero-pad month so files sort chronologically (e.g. 2024-01.pdf before 2024-10.pdf)
  doc.save(`report-${report.year}-${String(report.month).padStart(2, '0')}.pdf`);
}
