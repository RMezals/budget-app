import type { SpendingTrendPoint } from '@/api/types';
import { useCurrencyFormatter } from '@/hooks/useCurrencyFormatter';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Legend,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

// One colour per known expense category; unknown categories fall back to the palette
const CATEGORY_COLORS: Record<string, string> = {
  Housing: '#2563eb',
  Utilities: '#0891b2',
  Food: '#16a34a',
  Transport: '#d97706',
  Healthcare: '#dc2626',
  Entertainment: '#7c3aed',
  Clothing: '#db2777',
  Education: '#0d9488',
  Insurance: '#64748b',
  Other: '#94a3b8',
};

const FALLBACK_PALETTE = [
  '#f59e0b',
  '#10b981',
  '#3b82f6',
  '#ef4444',
  '#8b5cf6',
  '#ec4899',
  '#06b6d4',
  '#84cc16',
];

function categoryColor(cat: string, index: number): string {
  return CATEGORY_COLORS[cat] ?? FALLBACK_PALETTE[index % FALLBACK_PALETTE.length];
}

function formatMonthLabel(month: string): string {
  const [year, m] = month.split('-');
  return new Date(Number(year), Number(m) - 1).toLocaleDateString('en-GB', {
    month: 'short',
    year: '2-digit',
  });
}

interface Props {
  data: SpendingTrendPoint[];
}

export default function SpendingTrendChart({ data }: Props) {
  const fmt = useCurrencyFormatter();

  // Collect all categories that appear at least once, preserving insertion order
  const categories = Array.from(new Set(data.flatMap((p) => Object.keys(p.expenses))));

  // Flatten to the shape recharts expects: { month, [category]: value, ... }
  const chartData = data.map((p) => ({
    month: p.month,
    ...p.expenses,
  }));

  return (
    <ResponsiveContainer width="100%" height={280}>
      <BarChart data={chartData} margin={{ top: 4, right: 8, left: 8, bottom: 0 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
        <XAxis
          dataKey="month"
          tickFormatter={formatMonthLabel}
          tick={{ fontSize: 11, fill: '#94a3b8' }}
          axisLine={false}
          tickLine={false}
          interval="preserveStartEnd"
        />
        <YAxis
          tickFormatter={(v: number) => fmt(v)}
          tick={{ fontSize: 11, fill: '#94a3b8' }}
          axisLine={false}
          tickLine={false}
          width={80}
        />
        <Tooltip
          formatter={(value: number, name: string) => [fmt(value), name]}
          labelFormatter={(label: string) => formatMonthLabel(label)}
          contentStyle={{
            background: '#fff',
            border: '1px solid #e2e8f0',
            borderRadius: 8,
            fontSize: 13,
            boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
          }}
        />
        <Legend wrapperStyle={{ fontSize: 12, paddingTop: 12 }} iconType="circle" iconSize={8} />
        {categories.map((cat, i) => (
          <Bar
            key={cat}
            dataKey={cat}
            stackId="expenses"
            fill={categoryColor(cat, i)}
            maxBarSize={40}
          />
        ))}
      </BarChart>
    </ResponsiveContainer>
  );
}
