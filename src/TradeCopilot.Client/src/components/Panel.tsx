import type { ReactNode } from "react";

type PanelProps = {
  title: string;
  subtitle?: string;
  children: ReactNode;
  className?: string;
  action?: ReactNode;
};

export function Panel({ title, subtitle, children, className, action }: PanelProps) {
  return (
    <section className={className ? `panel ${className}` : "panel"}>
      <div className="panelHeader">
        <div>
          <h2>{title}</h2>
          {subtitle ? <span>{subtitle}</span> : null}
        </div>
        {action}
      </div>
      {children}
    </section>
  );
}
