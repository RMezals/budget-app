import { useEffect, useRef, useState } from 'react';
import './MonthPicker.css';

const MONTHS_SHORT = [
  'Jan',
  'Feb',
  'Mar',
  'Apr',
  'May',
  'Jun',
  'Jul',
  'Aug',
  'Sep',
  'Oct',
  'Nov',
  'Dec',
];

interface MonthPickerProps {
  value: string; // YYYY-MM
  onChange: (v: string) => void;
  min?: string; // YYYY-MM
  max?: string; // YYYY-MM
  placeholder?: string;
}

function formatDisplay(value: string): string {
  if (!value) return '';
  const [year, month] = value.split('-');
  return `${MONTHS_SHORT[Number(month) - 1]} ${year}`;
}

export default function MonthPicker({
  value,
  onChange,
  min,
  max,
  placeholder = 'Select month',
}: MonthPickerProps) {
  const [open, setOpen] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const currentYear = value ? Number(value.split('-')[0]) : new Date().getFullYear();
  const [viewYear, setViewYear] = useState(currentYear);

  useEffect(() => {
    if (value) setViewYear(Number(value.split('-')[0]));
  }, [value]);

  useEffect(() => {
    if (!open) return;
    function handler(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) setOpen(false);
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

  function isDisabled(monthIdx: number): boolean {
    const val = `${viewYear}-${String(monthIdx + 1).padStart(2, '0')}`;
    if (min && val < min) return true;
    if (max && val > max) return true;
    return false;
  }

  function isSelected(monthIdx: number): boolean {
    return value === `${viewYear}-${String(monthIdx + 1).padStart(2, '0')}`;
  }

  function isCurrentMonth(monthIdx: number): boolean {
    const now = new Date();
    return now.getFullYear() === viewYear && now.getMonth() === monthIdx;
  }

  function select(monthIdx: number) {
    if (isDisabled(monthIdx)) return;
    onChange(`${viewYear}-${String(monthIdx + 1).padStart(2, '0')}`);
    setOpen(false);
  }

  const canGoPrev = !min || viewYear > Number(min.split('-')[0]);
  const canGoNext = !max || viewYear < Number(max.split('-')[0]);

  return (
    <div className="mp-root" ref={containerRef}>
      <button
        type="button"
        className={`mp-input ${open ? 'open' : ''}`}
        onClick={() => setOpen((o) => !o)}
      >
        <span className={value ? 'mp-value' : 'mp-placeholder'}>
          {value ? formatDisplay(value) : placeholder}
        </span>
        <span className="mp-icon">
          <svg
            width="14"
            height="14"
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
        <div className="mp-popup">
          <div className="mp-header">
            <button
              type="button"
              className="mp-nav"
              onClick={() => setViewYear((y) => y - 1)}
              disabled={!canGoPrev}
            >
              ‹
            </button>
            <span className="mp-year">{viewYear}</span>
            <button
              type="button"
              className="mp-nav"
              onClick={() => setViewYear((y) => y + 1)}
              disabled={!canGoNext}
            >
              ›
            </button>
          </div>
          <div className="mp-grid">
            {MONTHS_SHORT.map((m, i) => (
              <button
                key={m}
                type="button"
                className={`mp-month ${isSelected(i) ? 'selected' : ''} ${isCurrentMonth(i) && !isSelected(i) ? 'current' : ''} ${isDisabled(i) ? 'disabled' : ''}`}
                onClick={() => select(i)}
                disabled={isDisabled(i)}
              >
                {m}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
