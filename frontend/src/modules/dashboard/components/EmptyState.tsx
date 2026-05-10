interface EmptyStateProps {
  icon: string;
  title: string;
  message: string;
}

/**
 * Generic empty state component for displaying helpful prompts when data is missing
 */
export default function EmptyState({ icon, title, message }: EmptyStateProps) {
  return (
    <div className="text-center py-5">
      <div className="fs-1 mb-3">{icon}</div>
      <h6 className="text-muted mb-2">{title}</h6>
      <p className="text-muted small mb-0">{message}</p>
    </div>
  );
}
