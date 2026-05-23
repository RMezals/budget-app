import { useEffect, useRef, useState } from 'react';
import '@/components/DatePicker.css';

const DAYS = ['Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa', 'Su'];
const MONTHS = [
  'January',
  'February',
  'March',
  'April',
  'May',
  'June',
  'July',
  'August',
  'September',
  'October',
  'November',
  'December',
];

function pad(n: number) {
  return String(n).padStart(2, '0');
}

function toDateStr(year: number, month: number, day: number) {
  return `${year}-${pad(month + 1)}-${pad(day)}`;
}

function parseDate(value: string): Date | null {
  if (!value) return null;
  const d = new Date(`${value}T00:00:00`);
  return Number.isNaN(d.getTime()) ? null : d;
}

function formatDisplay(value: string): string {
  const d = parseDate(value);
  if (!d) return '';
  return d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
}

interface DatePickerProps {
  value: string;
  onChange: (v: string) => void;
  min?: string;
  max?: string;
  placeholder?: string;
  invalid?: boolean;
  dropUp?: boolean;
}

export default function DatePicker({
  value,
  onChange,
  min,
  max,
  placeholder = 'Select date',
  invalid,
  dropUp = false,
}: DatePickerProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const selectedDate = parseDate(value);
  const minDate = min ? parseDate(min) : null;
  const maxDate = max ? parseDate(max) : null;
  const todayStr = new Date().toISOString().split('T')[0];
  const today = parseDate(todayStr) ?? new Date();

  const [view, setView] = useState<{ year: number; month: number }>(() => {
    const d = selectedDate ?? today;
    return { year: d.getFullYear(), month: d.getMonth() };
  });

  useEffect(() => {
    const d = parseDate(value);
    if (d) setView({ year: d.getFullYear(), month: d.getMonth() });
  }, [value]);

  useEffect(() => {
    if (!open) return;
    function handler(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, [open]);

  useEffect(() => {
    if (!open) return;
    function handler(e: KeyboardEvent) {
      if (e.key === 'Escape') setOpen(false);
    }
    document.addEventListener('keydown', handler);
    return () => document.removeEventListener('keydown', handler);
  }, [open]);

  function prevMonth() {
    setView((v) => {
      const m = v.month === 0 ? 11 : v.month - 1;
      const y = v.month === 0 ? v.year - 1 : v.year;
      return { year: y, month: m };
    });
  }

  function nextMonth() {
    setView((v) => {
      const m = v.month === 11 ? 0 : v.month + 1;
      const y = v.month === 11 ? v.year + 1 : v.year;
      return { year: y, month: m };
    });
  }

  function selectDay(day: number) {
    const str = toDateStr(view.year, view.month, day);
    const d = parseDate(str);
    if (!d) return;
    if (maxDate && d > maxDate) return;
    if (minDate && d < minDate) return;
    onChange(str);
    setOpen(false);
  }

  function selectToday() {
    const d = parseDate(todayStr);
    if (d && maxDate && d > maxDate) return;
    if (d && minDate && d < minDate) return;
    onChange(todayStr);
    setOpen(false);
  }

  function isDisabled(day: number) {
    const d = new Date(view.year, view.month, day);
    if (maxDate && d > maxDate) return true;
    if (minDate && d < minDate) return true;
    return false;
  }

  function isSelected(day: number) {
    return value === toDateStr(view.year, view.month, day);
  }

  function isToday(day: number) {
    return todayStr === toDateStr(view.year, view.month, day);
  }

  const daysInMonth = new Date(view.year, view.month + 1, 0).getDate();
  const firstDow = (new Date(view.year, view.month, 1).getDay() + 6) % 7;

  const cells: (number | null)[] = [
    ...Array(firstDow).fill(null),
    ...Array.from({ length: daysInMonth }, (_, i) => i + 1),
  ];
  while (cells.length % 7 !== 0) cells.push(null);

  const canGoPrev =
    !minDate ||
    new Date(view.year, view.month - 1, 1) >=
      new Date(minDate.getFullYear(), minDate.getMonth(), 1);

  const canGoNext =
    !maxDate ||
    new Date(view.year, view.month + 1, 1) <=
      new Date(maxDate.getFullYear(), maxDate.getMonth(), 1);

  return (
    <div className="dp-root" ref={containerRef}>
      <button
        type="button"
        className={`dp-input ${open ? 'open' : ''} ${invalid ? 'invalid' : ''}`}
        onClick={() => setOpen((o) => !o)}
      >
        <span className={value ? 'dp-input-value' : 'dp-input-placeholder'}>
          {value ? formatDisplay(value) : placeholder}
        </span>
        <span className="dp-input-icon">
          <svg
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            aria-hidden="true"
          >
            <rect x="3" y="4" width="18" height="18" rx="2" />
            <line x1="16" y1="2" x2="16" y2="6" />
            <line x1="8" y1="2" x2="8" y2="6" />
            <line x1="3" y1="10" x2="21" y2="10" />
          </svg>
        </span>
      </button>

      {open && (
        <div className={`dp-popup${dropUp ? ' dp-popup-up' : ''}`}>
          <div className="dp-header">
            <button type="button" className="dp-nav" onClick={prevMonth} disabled={!canGoPrev}>
              ‹
            </button>
            <span className="dp-month-label">
              {MONTHS[view.month]} {view.year}
            </span>
            <button type="button" className="dp-nav" onClick={nextMonth} disabled={!canGoNext}>
              ›
            </button>
          </div>

          <div className="dp-grid">
            {DAYS.map((d) => (
              <span key={d} className="dp-dow">
                {d}
              </span>
            ))}
            {cells.map((day, i) =>
              day === null ? (
                // biome-ignore lint/suspicious/noArrayIndexKey: empty calendar cells have no stable id
                <span key={`e-${i}`} />
              ) : (
                <button
                  key={day}
                  type="button"
                  className={`dp-day${isSelected(day) ? ' selected' : ''}${isToday(day) && !isSelected(day) ? ' today' : ''}${isDisabled(day) ? ' disabled' : ''}`}
                  onClick={() => !isDisabled(day) && selectDay(day)}
                  disabled={isDisabled(day)}
                >
                  {day}
                </button>
              ),
            )}
          </div>

          <div className="dp-footer">
            <button type="button" className="dp-today-btn" onClick={selectToday}>
              Today
            </button>
            {value && (
              <button
                type="button"
                className="dp-clear-btn"
                onClick={() => {
                  onChange('');
                  setOpen(false);
                }}
              >
                Clear
              </button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
