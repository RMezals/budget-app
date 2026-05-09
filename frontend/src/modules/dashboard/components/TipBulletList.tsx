interface TipBulletListProps {
  items: string[];
}

/**
 * Displays a bulleted list of tips
 * Reuses Bootstrap list-unstyled with custom bullet styling
 */
export default function TipBulletList({ items }: TipBulletListProps) {
  return (
    <ul className="mb-3 ps-3">
      {items.map((item, index) => (
        <li key={index} className="py-1">
          {item}
        </li>
      ))}
    </ul>
  );
}
