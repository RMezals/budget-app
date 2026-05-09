import { useMemo } from 'react';
import type { AdvisorResult } from '../../../api/types';
import TipBulletList from './TipBulletList';
import TipNumberedList from './TipNumberedList';
import TipParagraph from './TipParagraph';
import { parseTips } from './utils/tipParser';

interface FormattedTipsProps {
  rawTips: AdvisorResult['tips'];
  provider: AdvisorResult['provider'];
}

/**
 * Main component for displaying AI-generated tips
 * Parses raw tips and renders them in a formatted, readable way
 */
export default function FormattedTips({ rawTips, provider }: FormattedTipsProps) {
  // Memoize parsing to avoid re-parsing on every render
  const sections = useMemo(() => {
    try {
      return parseTips(rawTips);
    } catch (error) {
      console.error('Failed to parse AI tips:', error);
      // Fallback to display raw tips if parsing fails
      return [
        {
          id: 'error-fallback',
          type: 'paragraph' as const,
          items: ['Unable to format tips. Please try again.'],
        },
      ];
    }
  }, [rawTips]);

  return (
    <div className="alert alert-light border mb-0">
      <p className="text-muted small mb-3">via {provider}</p>
      <div>
        {sections.map((section) => {
          // Use section.id for stable, unique keys
          switch (section.type) {
            case 'numbered':
              return <TipNumberedList key={section.id} items={section.items} />;
            case 'bullet':
              return <TipBulletList key={section.id} items={section.items} />;
            case 'paragraph':
              return <TipParagraph key={section.id} items={section.items} />;
            default:
              return null;
          }
        })}
      </div>
    </div>
  );
}
