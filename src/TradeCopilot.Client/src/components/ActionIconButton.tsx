import { Link2, PencilLine, Trash2 } from "lucide-react";

type ActionIconButtonProps = {
  action: "edit" | "delete" | "link";
  label: string;
  onClick: () => void;
  isActive?: boolean;
  disabled?: boolean;
};

export function ActionIconButton({ action, label, onClick, isActive = false, disabled = false }: ActionIconButtonProps) {
  const Icon = action === "edit" ? PencilLine : action === "link" ? Link2 : Trash2;

  return (
    <button
      aria-label={label}
      aria-pressed={action === "delete" ? undefined : isActive}
      className={`actionIconButton ${action === "delete" ? "danger" : ""} ${isActive ? "active" : ""}`.trim()}
      disabled={disabled}
      onClick={onClick}
      title={label}
      type="button"
    >
      <Icon size={16} />
    </button>
  );
}
