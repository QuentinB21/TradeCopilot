import { useQuery } from "@tanstack/react-query";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { formatPercent } from "../lib/format";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

export function StrategyPage() {
  const strategyQuery = useQuery({ queryKey: ["strategy"], queryFn: tradeCopilotApi.getStrategy });

  return (
    <>
      <PageHeader title="Strategie" description="Regles patrimoniales parametrees pour guider les decisions mensuelles." />
      <QueryState isLoading={strategyQuery.isLoading} error={strategyQuery.error}>
        {strategyQuery.data ? (
          <section className="grid">
            <Panel title="Allocation globale">
              <div className="compactList">
                {strategyQuery.data.globalAllocation.map((target) => (
                  <div className="compactRow" key={target.envelope}>
                    <strong>{target.envelope}</strong>
                    <span>{formatPercent(target.targetWeight)}</span>
                  </div>
                ))}
              </div>
            </Panel>
            <Panel title="Regles PEA">
              <ul className="notes">{strategyQuery.data.peaRules.map((rule) => <li key={rule}>{rule}</li>)}</ul>
            </Panel>
            <Panel title="Regles Trade Republic">
              <ul className="notes">{strategyQuery.data.tradeRepublicRules.map((rule) => <li key={rule}>{rule}</li>)}</ul>
            </Panel>
          </section>
        ) : null}
      </QueryState>
    </>
  );
}
