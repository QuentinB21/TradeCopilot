import { useQuery } from "@tanstack/react-query";
import { ArrowDownRight, ArrowUpRight, CircleDollarSign, Target } from "lucide-react";
import { useMemo } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { Metric } from "../components/Metric";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";
import { formatCurrencyCompact, formatPercent } from "../lib/format";
import type { Dashboard, Position } from "../domain/types";

export function DashboardPage() {
  const dashboardQuery = useQuery({ queryKey: ["dashboard"], queryFn: tradeCopilotApi.getDashboard });

  return (
    <>
      <PageHeader title="Vue patrimoniale" description="Suivi consolide des enveloppes, positions et ecarts d'allocation." />
      <QueryState isLoading={dashboardQuery.isLoading} error={dashboardQuery.error}>
        {dashboardQuery.data ? <DashboardContent dashboard={dashboardQuery.data} /> : null}
      </QueryState>
    </>
  );
}

function DashboardContent({ dashboard }: { dashboard: Dashboard }) {
  const topPositions = useMemo(
    () => [...dashboard.positions].sort((a, b) => b.marketValue - a.marketValue).slice(0, 8),
    [dashboard.positions]
  );

  if (dashboard.portfolios.length === 0) {
    return (
      <Panel title="Demarrage">
        <div className="setupSteps">
          <div><strong>1. Strategie</strong><span>Creer les portefeuilles, actifs, cles et regles.</span></div>
          <div><strong>2. Positions</strong><span>Saisir les lignes deja detenues avec quantite et PRU.</span></div>
          <div><strong>3. Prix</strong><span>Ajouter un dernier cours pour valoriser chaque ligne.</span></div>
        </div>
      </Panel>
    );
  }

  return (
    <>
      <section className="metrics">
        <Metric title="Valeur totale" value={formatCurrencyCompact(dashboard.totalMarketValue)} icon={<CircleDollarSign size={20} />} />
        <Metric title="Montant investi" value={formatCurrencyCompact(dashboard.totalInvested)} icon={<Target size={20} />} />
        <Metric
          title="Gain latent"
          value={formatCurrencyCompact(dashboard.totalUnrealizedGain)}
          detail={formatPercent(dashboard.totalUnrealizedGainPercent)}
          icon={dashboard.totalUnrealizedGain >= 0 ? <ArrowUpRight size={20} /> : <ArrowDownRight size={20} />}
        />
      </section>

      <section className="grid">
        <Panel title="Repartition par enveloppe" subtitle={`${dashboard.portfolios.length} portefeuilles`}>
          <PortfolioBars dashboard={dashboard} />
        </Panel>
        <Panel title="Ecarts d'allocation" subtitle="Poids reel vs cible">
          <AllocationDrift positions={dashboard.positions} />
        </Panel>
      </section>

      <Panel title="Positions principales" subtitle="Classees par valeur de marche">
        <PositionTable positions={topPositions} />
      </Panel>
    </>
  );
}

function PortfolioBars({ dashboard }: { dashboard: Dashboard }) {
  const max = Math.max(...dashboard.portfolios.map((portfolio) => portfolio.marketValue), 1);

  return (
    <div className="bars">
      {dashboard.portfolios.length === 0 ? <p className="emptyState">Aucun portefeuille configure.</p> : null}
      {dashboard.portfolios.map((portfolio) => (
        <div className="barRow" key={portfolio.portfolioId}>
          <span>{portfolio.name}</span>
          <div className="barTrack">
            <div className="barFill" style={{ width: `${(portfolio.marketValue / max) * 100}%` }} />
          </div>
          <strong>{formatCurrencyCompact(portfolio.marketValue)}</strong>
        </div>
      ))}
    </div>
  );
}

function AllocationDrift({ positions }: { positions: Position[] }) {
  const tracked = positions.filter((position) => position.targetWeight !== null);

  return (
    <div className="compactList">
      {tracked.length === 0 ? <p className="emptyState">Aucune cible d'allocation configuree.</p> : null}
      {tracked.map((position) => (
        <div className="compactRow" key={`${position.portfolioId}-${position.assetId}`}>
          <div>
            <strong>{position.symbol}</strong>
            <span>{position.portfolioName}</span>
          </div>
          <span className={(position.allocationDrift ?? 0) >= 0 ? "positive" : "negative"}>
            {formatPercent(position.allocationDrift ?? 0)}
          </span>
        </div>
      ))}
    </div>
  );
}

export function PositionTable({ positions }: { positions: Position[] }) {
  return (
    <div className="tableWrap">
      {positions.length === 0 ? <p className="emptyState">Aucune position valorisee.</p> : null}
      <table>
        <thead>
          <tr>
            <th>Ligne</th>
            <th>Portefeuille</th>
            <th>Valeur</th>
            <th>Gain latent</th>
            <th>Poids</th>
            <th>Cible</th>
            <th>Statut</th>
          </tr>
        </thead>
        <tbody>
          {positions.map((position) => (
            <tr key={`${position.portfolioId}-${position.assetId}`}>
              <td>
                <strong>{position.symbol}</strong>
                <span>{position.assetName}</span>
              </td>
              <td>{position.portfolioName}</td>
              <td>{formatCurrencyCompact(position.marketValue)}</td>
              <td className={position.unrealizedGain >= 0 ? "positive" : "negative"}>
                {formatCurrencyCompact(position.unrealizedGain)}
              </td>
              <td>{formatPercent(position.weight)}</td>
              <td>{position.targetWeight == null ? "-" : formatPercent(position.targetWeight)}</td>
              <td><span className="status">{position.strategicStatus}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
