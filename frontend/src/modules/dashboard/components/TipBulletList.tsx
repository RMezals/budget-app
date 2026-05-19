import { formatMarkdown } from '@/modules/dashboard/components/utils/markdownFormatter';

interface TipBulletListProps {
  items: string[];
}

/**
 * Displays a bulleted list of tips
 * Reuses Bootstrap list-unstyled with custom bullet styling
 */
export default function TipBulletList({ items }: TipBulletListProps) {
  return (
    <ul className="mb-3 ps-3" style={{ listStyleType: 'disc' }}>
      {items.map((item, index) => (
        // biome-ignore lint/suspicious/noArrayIndexKey: items are stable within section
        <li key={index} className="mb-2" style={{ lineHeight: '1.6', fontSize: '0.95rem' }}>
          {formatMarkdown(item)}
        </li>
      ))}
    </ul>
  );
}
