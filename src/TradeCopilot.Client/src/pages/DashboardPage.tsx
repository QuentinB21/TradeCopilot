import { useQuery } from "@tanstack/react-query";
import { AlertTriangle, ArrowDownRight, ArrowUpRight, CircleDollarSign, RefreshCw, Target } from "lucide-react";
import { type CSSProperties, useEffect, useMemo, useState } from "react";
import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { Metric } from "../components/Metric";
import { MarketBindingPanel } from "../components/MarketBindingPanel";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";
import { readDashboardRefreshInterval } from "../lib/appSettings";
import { formatCurrencyCompact, formatPercent } from "../lib/format";
import { formatStrategicStatus } from "../domain/options";
import type { Dashboard, DashboardHistoryPoint, PortfolioSummary, Position } from "../domain/types";

const chartColors = ["#0a0a0a", "#155eef", "#0b7a48", "#b86a00", "#c22a2a", "#525252", "#7c3aed", "#0f766e"];

export function DashboardPage() {
  const [refreshIntervalMs] = useState(readDashboardRefreshInterval);
  const [now, setNow] = useState(() => Date.now());
  const [nextRefreshAt, setNextRefreshAt] = useState(() => Date.now() + refreshIntervalMs);
  const dashboardQuery = useQuery({
    queryKey: ["dashboard"],
    queryFn: tradeCopilotApi.getDashboard,
    refetchInterval: refreshIntervalMs,
    refetchIntervalInBackground: true,
    staleTime: 60 * 1000
  });

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    if (dashboardQuery.dataUpdatedAt > 0) {
      setNextRefreshAt(dashboardQuery.dataUpdatedAt + refreshIntervalMs);
    }
  }, [dashboardQuery.dataUpdatedAt, refreshIntervalMs]);

  return (
    <>
      <PageHeader
        title="Vue patrimoniale"
        description="Suivi consolide des performances, objectifs d'allocation et valorisations disponibles."
        action={(
          <DashboardRefreshControl
            intervalMs={refreshIntervalMs}
            isRefreshing={dashboardQuery.isFetching}
            nextRefreshAt={nextRefreshAt}
            now={now}
          />
        )}
      />
      <QueryState isLoading={dashboardQuery.isLoading} error={dashboardQuery.error}>
        {dashboardQuery.data ? <DashboardContent dashboard={dashboardQuery.data} /> : null}
      </QueryState>
    </>
  );
}

function DashboardRefreshControl({
  intervalMs,
  isRefreshing,
  nextRefreshAt,
  now
}: {
  intervalMs: number;
  isRefreshing: boolean;
  nextRefreshAt: number;
  now: number;
}) {
  const remainingMs = Math.min(Math.max(nextRefreshAt - now, 0), intervalMs);
  const progress = Math.min(Math.max(1 - remainingMs / intervalMs, 0), 1);

  return (
    <div className="dashboardRefresh">
      <div
        aria-live="polite"
        className={isRefreshing ? "refreshTimer refreshing" : "refreshTimer"}
        style={{ "--refresh-progress": `${progress * 360}deg` } as CSSProperties}
      >
        <RefreshCw size={14} />
      </div>
      <div className="refreshTimerText">
        <span>{isRefreshing ? "Mise a jour" : "Actualisation dans"}</span>
        <strong>{isRefreshing ? "..." : formatCountdown(remainingMs)}</strong>
      </div>
    </div>
  );
}

function formatCountdown(milliseconds: number) {
  const seconds = Math.ceil(milliseconds / 1000);
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}:${String(remainingSeconds).padStart(2, "0")}`;
}

function DashboardContent({ dashboard }: { dashboard: Dashboard }) {
  const topPositions = useMemo(
    () => [...dashboard.positions].sort((a, b) => b.marketValue - a.marketValue).slice(0, 8),
    [dashboard.positions]
  );
  const missingPricePositions = dashboard.positions.filter((position) => !position.hasMarketPrice);
  const missingPriceCount = missingPricePositions.length;

  if (dashboard.portfolios.length === 0) {
    return (
      <Panel title="Demarrage" subtitle="Le dashboard devient utile des que la configuration et les donnees existent." className="setupPanel">
        <div className="setupSteps">
          <div><strong>1. Strategie</strong><span>Creer les portefeuilles, actifs, cles et regles.</span></div>
          <div><strong>2. Transactions</strong><span>Importer ou saisir les lignes deja detenues avec quantite et PRU.</span></div>
          <div><strong>3. Pilotage</strong><span>Suivre la valorisation, les ecarts aux objectifs et les lignes non valorisees.</span></div>
        </div>
      </Panel>
    );
  }

  return (
    <>
      <section className="metrics dashboardMetrics">
        <Metric title="Valeur totale" value={formatCurrencyCompact(dashboard.totalMarketValue)} icon={<CircleDollarSign size={20} />} tone="positive" />
        <Metric title="Montant investi" value={formatCurrencyCompact(dashboard.totalInvested)} icon={<Target size={20} />} />
        <Metric
          title="Gain latent"
          value={formatCurrencyCompact(dashboard.totalUnrealizedGain)}
          detail={formatPercent(dashboard.totalUnrealizedGainPercent)}
          icon={dashboard.totalUnrealizedGain >= 0 ? <ArrowUpRight size={20} /> : <ArrowDownRight size={20} />}
          tone={dashboard.totalUnrealizedGain >= 0 ? "positive" : "negative"}
        />
        <Metric title="Cours manquants" value={String(missingPriceCount)} detail="valorisees au PRU" icon={<AlertTriangle size={20} />} tone="warning" />
      </section>

      <section className="dashboardFocusGrid">
        <Panel title="Evolution globale" subtitle="Valeur de marche et capital investi" className="chartPanel">
          <TotalValueChart history={dashboard.history} />
        </Panel>
        <Panel title="A surveiller" subtitle="Ecarts qui meritent une action" className="watchPanel">
          <DashboardWatchlist positions={dashboard.positions} missingPriceCount={missingPriceCount} />
        </Panel>
      </section>

      {missingPricePositions.length > 0 ? (
        <Panel title="Cours a lier" subtitle="Associer manuellement les actifs qui ne trouvent pas leur cotation automatiquement." className="bindingPanel">
          <MarketBindingPanel assets={missingPricePositions} />
        </Panel>
      ) : null}

      <section className="grid dashboardGrid">
        <Panel title="Objectifs par portefeuille" subtitle="Poids reel vs cle cible">
          <PortfolioObjectiveProgress portfolios={dashboard.portfolios} />
        </Panel>
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

  return (
    <div className="chartBlock">
      <div className="chartCanvas" role="img" aria-label="Evolution globale du patrimoine">
        <ResponsiveContainer width="100%" height={280}>
          <LineChart data={points} margin={{ top: 18, right: 18, bottom: 8, left: 4 }}>
            <CartesianGrid stroke="#deded8" strokeDasharray="4 8" vertical={false} />
            <XAxis dataKey="date" tickFormatter={formatShortDate} axisLine={false} tickLine={false} minTickGap={24} />
            <YAxis
              axisLine={false}
              domain={[0, maxValue]}
              tickFormatter={(value) => formatCurrencyCompact(Number(value))}
              tickLine={false}
              width={88}
            />
            <Tooltip content={<PortfolioTooltip />} />
            <Legend />
            <Line dataKey="totalInvested" dot={false} name="Investi" stroke="#b86a00" strokeWidth={2.5} type="monotone" />
            <Line dataKey="totalMarketValue" dot={false} name="Valeur" stroke="#0a0a0a" strokeWidth={3} type="monotone" />
          </LineChart>
        </ResponsiveContainer>
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
      <div className="chartCanvas" role="img" aria-label="Evolution par portefeuille">
        <ResponsiveContainer width="100%" height={280}>
          <LineChart
            data={points.map((point) => ({
              date: point.date,
              ...Object.fromEntries(point.portfolios.map((portfolio) => [portfolio.portfolioId, portfolio.marketValue]))
            }))}
            margin={{ top: 18, right: 18, bottom: 8, left: 4 }}
          >
            <CartesianGrid stroke="#deded8" strokeDasharray="4 8" vertical={false} />
            <XAxis dataKey="date" tickFormatter={formatShortDate} axisLine={false} tickLine={false} minTickGap={24} />
            <YAxis
              axisLine={false}
              domain={[0, maxValue]}
              tickFormatter={(value) => formatCurrencyCompact(Number(value))}
              tickLine={false}
              width={88}
            />
            <Tooltip content={<PortfolioTooltip />} />
            <Legend />
            {activePortfolios.map((portfolio, index) => (
              <Line
                dataKey={portfolio.portfolioId}
                dot={false}
                key={portfolio.portfolioId}
                name={portfolio.name}
                stroke={chartColors[index % chartColors.length]}
                strokeWidth={2.5}
                type="monotone"
              />
            ))}
          </LineChart>
        </ResponsiveContainer>
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
          label={position.assetName}
          detail={`${position.portfolioName} - ${position.symbol}`}
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

function normalizeHistory(history: DashboardHistoryPoint[]) {
  return [...history].sort((left, right) => left.date.localeCompare(right.date));
}

function formatDate(date: string) {
  return new Intl.DateTimeFormat("fr-FR", { day: "2-digit", month: "short", year: "2-digit" }).format(new Date(`${date}T00:00:00`));
}

function formatShortDate(date: string) {
  return new Intl.DateTimeFormat("fr-FR", { day: "2-digit", month: "short" }).format(new Date(`${date}T00:00:00`));
}

function PortfolioTooltip({ active, label, payload }: { active?: boolean; label?: string; payload?: Array<{ name?: string; value?: number | string; color?: string }> }) {
  if (!active || !payload?.length) {
    return null;
  }

  return (
    <div className="chartTooltip">
      <strong>{label ? formatDate(label) : "Valeur"}</strong>
      {payload.map((entry) => (
        <span key={entry.name} style={{ color: entry.color }}>
          {entry.name}: {formatCurrencyCompact(Number(entry.value ?? 0))}
        </span>
      ))}
    </div>
  );
}

function DashboardWatchlist({ positions, missingPriceCount }: { positions: Position[]; missingPriceCount: number }) {
  const driftedPositions = positions
    .filter((position) => position.targetWeight !== null)
    .sort((left, right) => Math.abs(right.allocationDrift ?? 0) - Math.abs(left.allocationDrift ?? 0))
    .slice(0, 3);

  return (
    <div className="watchList">
      <div className={missingPriceCount > 0 ? "watchItem watchItem-warning" : "watchItem watchItem-positive"}>
        <strong>{missingPriceCount > 0 ? "Valorisation incomplete" : "Valorisation disponible"}</strong>
        <span>{missingPriceCount > 0 ? `${missingPriceCount} ligne(s) utilisent encore le PRU.` : "Toutes les lignes suivies ont un cours exploitable."}</span>
      </div>
      {driftedPositions.map((position) => (
        <div className="watchItem" key={`${position.portfolioId}-${position.assetId}`}>
          <strong>{position.assetName}</strong>
          <span>{position.symbol} - {position.portfolioName} - ecart {formatPercent(position.allocationDrift ?? 0)} vs cible.</span>
        </div>
      ))}
      {driftedPositions.length === 0 ? <p className="emptyState">Les ecarts apparaitront apres configuration des cles par ligne.</p> : null}
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
              <td><span className="status">{formatStrategicStatus(position.strategicStatus)}</span></td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
