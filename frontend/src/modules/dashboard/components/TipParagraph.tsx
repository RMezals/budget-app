import { formatMarkdown } from '@/modules/dashboard/components/utils/markdownFormatter';

interface TipParagraphProps {
  items: string[];
}

/**
 * Displays paragraph content from AI tips
 * Each item is rendered as a separate paragraph
 */
export default function TipParagraph({ items }: TipParagraphProps) {
  return (
    <div className="mb-3">
      {items.map((item, index) => (
        // biome-ignore lint/suspicious/noArrayIndexKey: items are stable within section
        <p key={index} className="mb-3" style={{ lineHeight: '1.6', fontSize: '0.95rem' }}>
          {formatMarkdown(item)}
        </p>
      ))}
    </div>
  );
}
