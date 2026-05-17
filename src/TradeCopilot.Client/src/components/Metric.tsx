import type { ReactNode } from "react";

type MetricProps = {
  title: string;
  value: string;
  detail?: string;
  icon: ReactNode;
};

export function Metric({ title, value, detail, icon }: MetricProps) {
  return (
    <article className="metric">
      <div className="metricIcon">{icon}</div>
      <span>{title}</span>
      <strong>{value}</strong>
      {detail ? <small>{detail}</small> : null}
    </article>
  );
}
