import { formatMarkdown } from '@/modules/dashboard/components/utils/markdownFormatter';

interface TipNumberedListProps {
  items: string[];
}

/**
 * Displays a numbered list of tips
 * Reuses Bootstrap list-group styles
 */
export default function TipNumberedList({ items }: TipNumberedListProps) {
  return (
    <ol className="ps-3 mb-3" style={{ counterReset: 'item' }}>
      {items.map((item, index) => (
        // biome-ignore lint/suspicious/noArrayIndexKey: items are stable within section
        <li key={index} className="mb-3" style={{ lineHeight: '1.6' }}>
          <strong className="d-inline me-1" style={{ color: '#0d6efd' }}>
            Tip {index + 1}:
          </strong>
          <span>{formatMarkdown(item)}</span>
        </li>
      ))}
    </ol>
  );
}
