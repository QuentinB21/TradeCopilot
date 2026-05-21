import type { ReactNode } from "react";

type PageHeaderProps = {
  title: string;
  description: string;
  action?: ReactNode;
};

export function PageHeader({ title, description, action }: PageHeaderProps) {
  return (
    <header className="topbar">
      <div className="pageLead">
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action ? <div className="pageActions">{action}</div> : null}
    </header>
  );
}
