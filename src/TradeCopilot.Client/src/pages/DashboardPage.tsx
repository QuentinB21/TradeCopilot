import { useQuery } from "@tanstack/react-query";
import { AlertTriangle, ArrowDownRight, ArrowUpRight, CircleDollarSign, Target } from "lucide-react";
import { useMemo } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { Metric } from "../components/Metric";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";
import { formatCurrencyCompact, formatPercent } from "../lib/format";
import type { Dashboard, DashboardHistoryPoint, PortfolioSummary, Position } from "../domain/types";

const chartColors = ["#123c35", "#2f6fed", "#b06618", "#7a4cc2", "#c24155", "#0f766e", "#475569", "#a16207"];

export function DashboardPage() {
  const dashboardQuery = useQuery({
    queryKey: ["dashboard"],
    queryFn: tradeCopilotApi.getDashboard,
    refetchInterval: 5 * 60 * 1000,
    staleTime: 60 * 1000
  });

  return (
    <>
      <PageHeader title="Vue patrimoniale" description="Suivi consolide des performances, objectifs d'allocation et valorisations disponibles, rafraichi toutes les 5 minutes." />
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
  const missingPriceCount = dashboard.positions.filter((position) => !position.hasMarketPrice).length;

  if (dashboard.portfolios.length === 0) {
    return (
      <Panel title="Demarrage">
        <div className="setupSteps">
          <div><strong>1. Strategie</strong><span>Creer les portefeuilles, actifs, cles et regles.</span></div>
          <div><strong>2. Transactions</strong><span>Importer ou saisir les lignes deja detenues avec quantite et PRU.</span></div>
          <div><strong>3. Dashboard</strong><span>Suivre la valorisation, les ecarts aux objectifs et les lignes non valorisees.</span></div>
        </div>
      </Panel>
    );
  }

  return (
    <>
      <section className="metrics dashboardMetrics">
        <Metric title="Valeur totale" value={formatCurrencyCompact(dashboard.totalMarketValue)} icon={<CircleDollarSign size={20} />} />
        <Metric title="Montant investi" value={formatCurrencyCompact(dashboard.totalInvested)} icon={<Target size={20} />} />
        <Metric
          title="Gain latent"
          value={formatCurrencyCompact(dashboard.totalUnrealizedGain)}
          detail={formatPercent(dashboard.totalUnrealizedGainPercent)}
          icon={dashboard.totalUnrealizedGain >= 0 ? <ArrowUpRight size={20} /> : <ArrowDownRight size={20} />}
        />
        <Metric title="Cours manquants" value={String(missingPriceCount)} detail="valorisees au PRU" icon={<AlertTriangle size={20} />} />
      </section>

      <section className="grid dashboardGrid">
        <Panel title="Evolution globale" subtitle="Valeur de marche et capital investi">
          <TotalValueChart history={dashboard.history} />
        </Panel>
        <Panel title="Objectifs par portefeuille" subtitle="Poids reel vs cle cible">
          <PortfolioObjectiveProgress portfolios={dashboard.portfolios} />
        </Panel>
      </section>

      <section className="grid dashboardGrid">
        <Panel title="Evolution par portefeuille" subtitle={`${dashboard.portfolios.length} enveloppes suivies`}>
          <PortfolioHistoryChart dashboard={dashboard} />
        </Panel>
        <Panel title="Objectifs par ligne" subtitle="Ecarts les plus importants">
          <LineObjectiveProgress positions={dashboard.positions} />
        </Panel>
      </section>

      <Panel title="Positions principales" subtitle="Classees par valeur de marche">
        <PositionTable positions={topPositions} />
      </Panel>
    </>
  );
}

function TotalValueChart({ history }: { history: DashboardHistoryPoint[] }) {
  const points = normalizeHistory(history);
  if (points.length < 2) {
    return <p className="emptyState">Ajoutez plusieurs operations ou cours dates pour afficher une evolution.</p>;
  }

  const maxValue = Math.max(...points.flatMap((point) => [point.totalMarketValue, point.totalInvested]), 1);
  const marketPath = buildLinePath(points, (point) => point.totalMarketValue, maxValue);
  const investedPath = buildLinePath(points, (point) => point.totalInvested, maxValue);

  return (
    <div className="chartBlock">
      <svg className="lineChart" viewBox="0 0 720 260" role="img" aria-label="Evolution globale du patrimoine">
        <ChartGrid />
        <path d={investedPath} fill="none" stroke="#b06618" strokeWidth="3" strokeLinecap="round" />
        <path d={marketPath} fill="none" stroke="#123c35" strokeWidth="3.5" strokeLinecap="round" />
      </svg>
      <div className="chartLegend">
        <span><i style={{ background: "#123c35" }} /> Valeur</span>
        <span><i style={{ background: "#b06618" }} /> Investi</span>
      </div>
      <div className="chartRange">
        <span>{formatDate(points[0].date)}</span>
        <strong>{formatCurrencyCompact(points.at(-1)?.totalMarketValue ?? 0)}</strong>
        <span>{formatDate(points.at(-1)?.date ?? points[0].date)}</span>
      </div>
    </div>
  );
}

function PortfolioHistoryChart({ dashboard }: { dashboard: Dashboard }) {
  const points = normalizeHistory(dashboard.history);
  const activePortfolios = dashboard.portfolios.filter((portfolio) => portfolio.marketValue > 0 || portfolio.investedAmount > 0);
  if (points.length < 2 || activePortfolios.length === 0) {
    return <p className="emptyState">L'historique apparaitra apres plusieurs operations valorisees.</p>;
  }

  const maxValue = Math.max(
    ...points.flatMap((point) => point.portfolios.map((portfolio) => portfolio.marketValue)),
    1
  );

  return (
    <div className="chartBlock">
      <svg className="lineChart" viewBox="0 0 720 260" role="img" aria-label="Evolution par portefeuille">
        <ChartGrid />
        {activePortfolios.map((portfolio, index) => (
          <path
            key={portfolio.portfolioId}
            d={buildLinePath(points, (point) => point.portfolios.find((item) => item.portfolioId === portfolio.portfolioId)?.marketValue ?? 0, maxValue)}
            fill="none"
            stroke={chartColors[index % chartColors.length]}
            strokeWidth="3"
            strokeLinecap="round"
          />
        ))}
      </svg>
      <div className="chartLegend">
        {activePortfolios.slice(0, chartColors.length).map((portfolio, index) => (
          <span key={portfolio.portfolioId}><i style={{ background: chartColors[index % chartColors.length] }} /> {portfolio.name}</span>
        ))}
      </div>
    </div>
  );
}

function PortfolioObjectiveProgress({ portfolios }: { portfolios: PortfolioSummary[] }) {
  const configured = portfolios.filter((portfolio) => portfolio.targetWeight > 0);
  if (configured.length === 0) {
    return <p className="emptyState">Aucune cle de repartition globale configuree.</p>;
  }

  return (
    <div className="objectiveList">
      {configured.map((portfolio) => (
        <ObjectiveRow
          key={portfolio.portfolioId}
          label={portfolio.name}
          detail={formatCurrencyCompact(portfolio.marketValue)}
          actual={portfolio.actualWeight}
          target={portfolio.targetWeight}
          drift={portfolio.allocationDrift}
        />
      ))}
    </div>
  );
}

function LineObjectiveProgress({ positions }: { positions: Position[] }) {
  const tracked = positions
    .filter((position) => position.targetWeight !== null)
    .sort((left, right) => Math.abs(right.allocationDrift ?? 0) - Math.abs(left.allocationDrift ?? 0))
    .slice(0, 8);

  if (tracked.length === 0) {
    return <p className="emptyState">Aucune cle par ligne configuree.</p>;
  }

  return (
    <div className="objectiveList">
      {tracked.map((position) => (
        <ObjectiveRow
          key={`${position.portfolioId}-${position.assetId}`}
          label={position.symbol}
          detail={position.portfolioName}
          actual={position.weight}
          target={position.targetWeight ?? 0}
          drift={position.allocationDrift ?? 0}
        />
      ))}
    </div>
  );
}

function ObjectiveRow({ label, detail, actual, target, drift }: { label: string; detail: string; actual: number; target: number; drift: number }) {
  const actualWidth = Math.min(Math.max(actual, 0), 1) * 100;
  const targetLeft = Math.min(Math.max(target, 0), 1) * 100;

  return (
    <div className="objectiveRow">
      <div className="objectiveHeader">
        <div><strong>{label}</strong><span>{detail}</span></div>
        <div className="rightText">
          <strong>{formatPercent(actual)}</strong>
          <span className={drift >= 0 ? "positive" : "negative"}>{formatPercent(drift)}</span>
        </div>
      </div>
      <div className="objectiveTrack">
        <div className="objectiveFill" style={{ width: `${actualWidth}%` }} />
        <div className="objectiveTarget" style={{ left: `${targetLeft}%` }} />
      </div>
      <div className="objectiveFooter">
        <span>Actuel</span>
        <span>Cible {formatPercent(target)}</span>
      </div>
    </div>
  );
}

function ChartGrid() {
  return (
    <g className="chartGridLines">
      {[40, 85, 130, 175, 220].map((y) => <line key={y} x1="44" x2="696" y1={y} y2={y} />)}
    </g>
  );
}

function buildLinePath(points: DashboardHistoryPoint[], getValue: (point: DashboardHistoryPoint) => number, maxValue: number) {
  const width = 652;
  const height = 184;
  const left = 44;
  const top = 36;
  const denominator = Math.max(points.length - 1, 1);

  return points
    .map((point, index) => {
      const x = left + (index / denominator) * width;
      const y = top + height - (Math.max(getValue(point), 0) / maxValue) * height;
      return `${index === 0 ? "M" : "L"} ${x.toFixed(2)} ${y.toFixed(2)}`;
    })
    .join(" ");
}

function normalizeHistory(history: DashboardHistoryPoint[]) {
  return [...history].sort((left, right) => left.date.localeCompare(right.date));
}

function formatDate(date: string) {
  return new Intl.DateTimeFormat("fr-FR", { day: "2-digit", month: "short", year: "2-digit" }).format(new Date(`${date}T00:00:00`));
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
                <strong>{position.assetName}</strong>
                <span>{position.symbol}</span>
              </td>
              <td>{position.portfolioName}</td>
              <td>
                <strong>{formatCurrencyCompact(position.marketValue)}</strong>
                {!position.hasMarketPrice ? <span>Estimee au PRU</span> : null}
              </td>
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
