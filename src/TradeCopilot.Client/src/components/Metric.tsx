import type { ReactNode } from "react";

type MetricProps = {
  title: string;
  value: string;
  detail?: string;
  icon: ReactNode;
  tone?: "neutral" | "positive" | "warning" | "negative";
};

export function Metric({ title, value, detail, icon, tone = "neutral" }: MetricProps) {
  return (
    <article className={`metric metric-${tone}`}>
      <div className="metricIcon">{icon}</div>
      <div className="metricBody">
        <span>{title}</span>
        <strong>{value}</strong>
      </div>
      {detail ? <small>{detail}</small> : null}
    </article>
  );
}
