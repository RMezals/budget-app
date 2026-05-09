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
        <p key={index} className="mb-2">
          {item}
        </p>
      ))}
    </div>
  );
}
