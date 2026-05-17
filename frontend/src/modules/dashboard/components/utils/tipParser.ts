export interface TipSection {
  id: string;
  type: 'numbered' | 'bullet' | 'paragraph';
  items: string[];
}

// Regex patterns defined once for performance
const NUMBERED_PATTERN = /^(\d+)[.)]\s+(.+)$/;
const BULLET_PATTERN = /^[-*•▪]\s+(.+)$/;

// Security limits
const MAX_TIP_LENGTH = 50000; // Prevent DoS with extremely large inputs
const MAX_SECTIONS = 100; // Reasonable limit for sections

/**
 * Parses AI-generated tips into structured sections
 * Detects numbered lists (1. 2. 3.), bullet points (- * •), and paragraphs
 *
 * @param rawTips - Raw text from AI provider
 * @returns Array of parsed sections with unique IDs
 * @throws {Error} If input exceeds safety limits
 */
export function parseTips(rawTips: string): TipSection[] {
  // Input validation
  if (!rawTips || typeof rawTips !== 'string') {
    return [
      {
        id: 'empty-0',
        type: 'paragraph',
        items: ['No tips available.'],
      },
    ];
  }

  // Security: Prevent DoS with extremely large inputs
  let processedTips = rawTips;
  if (rawTips.length > MAX_TIP_LENGTH) {
    console.warn(`Tips truncated from ${rawTips.length} to ${MAX_TIP_LENGTH} characters`);
    processedTips = rawTips.substring(0, MAX_TIP_LENGTH);
  }

  const sections: TipSection[] = [];
  const lines = processedTips
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean);

  let currentSection: Omit<TipSection, 'id'> | null = null;
  let sectionCounter = 0;

  for (const line of lines) {
    // Safety check: prevent infinite sections
    if (sectionCounter >= MAX_SECTIONS) {
      console.warn(`Section limit of ${MAX_SECTIONS} reached, stopping parse`);
      break;
    }

    // Skip markdown headers (##, ###, etc.)
    if (line.startsWith('#')) {
      continue;
    }

    // Detect numbered list items (1. 2. 3. or 1) 2) 3))
    const numberedMatch = line.match(NUMBERED_PATTERN);
    if (numberedMatch) {
      if (currentSection?.type !== 'numbered') {
        if (currentSection) {
          sections.push({ ...currentSection, id: `section-${sectionCounter++}` });
        }
        currentSection = { type: 'numbered', items: [] };
      }
      currentSection.items.push(numberedMatch[2]);
      continue;
    }

    // Detect bullet points (-, *, •, ▪)
    const bulletMatch = line.match(BULLET_PATTERN);
    if (bulletMatch) {
      if (currentSection?.type !== 'bullet') {
        if (currentSection) {
          sections.push({ ...currentSection, id: `section-${sectionCounter++}` });
        }
        currentSection = { type: 'bullet', items: [] };
      }
      currentSection.items.push(bulletMatch[1]);
      continue;
    }

    // Regular paragraph
    if (currentSection?.type !== 'paragraph') {
      if (currentSection) {
        sections.push({ ...currentSection, id: `section-${sectionCounter++}` });
      }
      currentSection = { type: 'paragraph', items: [] };
    }
    currentSection.items.push(line);
  }

  if (currentSection) {
    sections.push({ ...currentSection, id: `section-${sectionCounter}` });
  }

  return sections;
}
