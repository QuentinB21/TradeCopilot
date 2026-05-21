import { PencilLine, Trash2 } from "lucide-react";

type ActionIconButtonProps = {
  action: "edit" | "delete";
  label: string;
  onClick: () => void;
  isActive?: boolean;
  disabled?: boolean;
};

export function ActionIconButton({ action, label, onClick, isActive = false, disabled = false }: ActionIconButtonProps) {
  const Icon = action === "edit" ? PencilLine : Trash2;

  return (
    <button
      aria-label={label}
      aria-pressed={action === "edit" ? isActive : undefined}
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
