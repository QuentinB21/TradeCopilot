import type { ReactNode } from "react";

type PanelProps = {
  title: string;
  subtitle?: string;
  children: ReactNode;
};

export function Panel({ title, subtitle, children }: PanelProps) {
  return (
    <section className="panel">
      <div className="panelHeader">
        <h2>{title}</h2>
        {subtitle ? <span>{subtitle}</span> : null}
      </div>
      {children}
    </section>
  );
}
