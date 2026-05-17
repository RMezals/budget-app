/**
 * Simple Markdown formatter for AI tips
 * Converts basic Markdown syntax to React elements
 */

/**
 * Formats a text string with Markdown syntax into React elements
 * Supports: **bold**, *italic*, `code`
 */
export function formatMarkdown(text: string): React.ReactNode[] {
  const parts: React.ReactNode[] = [];
  let currentIndex = 0;
  let keyCounter = 0;

  // Regex pattern to match **bold**, *italic*, or `code`
  const pattern = /(\*\*([^*]+)\*\*)|(\*([^*]+)\*)|(`([^`]+)`)/g;
  const matches = Array.from(text.matchAll(pattern));

  for (const match of matches) {
    // Add text before the match
    if (match.index > currentIndex) {
      parts.push(text.substring(currentIndex, match.index));
    }

    // Add the formatted match
    if (match[1]) {
      // **bold**
      parts.push(
        <strong key={`bold-${keyCounter++}`} style={{ fontWeight: 600 }}>
          {match[2]}
        </strong>,
      );
    } else if (match[3]) {
      // *italic*
      parts.push(
        <em key={`italic-${keyCounter++}`} style={{ fontStyle: 'italic' }}>
          {match[4]}
        </em>,
      );
    } else if (match[5]) {
      // `code`
      parts.push(
        <code
          key={`code-${keyCounter++}`}
          style={{
            backgroundColor: '#f8f9fa',
            padding: '0.125rem 0.375rem',
            borderRadius: '0.25rem',
            fontSize: '0.875em',
            fontFamily: 'monospace',
          }}
        >
          {match[6]}
        </code>,
      );
    }

    currentIndex = match.index + match[0].length;
  }

  // Add any remaining text
  if (currentIndex < text.length) {
    parts.push(text.substring(currentIndex));
  }

  return parts.length > 0 ? parts : [text];
}
