interface TipNumberedListProps {
  items: string[];
}

/**
 * Displays a numbered list of tips
 * Reuses Bootstrap list-group styles
 */
export default function TipNumberedList({ items }: TipNumberedListProps) {
  return (
    <ol className="list-group list-group-numbered mb-3">
      {items.map((item, index) => (
        <li key={index} className="list-group-item border-0 ps-0 py-2">
          {item}
        </li>
      ))}
    </ol>
  );
}
