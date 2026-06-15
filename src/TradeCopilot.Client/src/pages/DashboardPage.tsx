import { useQuery } from "@tanstack/react-query";
import { ArrowDownRight, ArrowUpRight, CircleDollarSign, RefreshCw, Target, X } from "lucide-react";
import { type CSSProperties, type ReactNode, useEffect, useMemo, useRef, useState } from "react";
import { CartesianGrid, Legend, Line, LineChart, ReferenceLine, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { Metric } from "../components/Metric";
import { MarketBindingPanel } from "../components/MarketBindingPanel";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";
import { useBodyScrollLock } from "../hooks/useBodyScrollLock";
import { readDashboardRefreshInterval } from "../lib/appSettings";
import { formatCurrencyCompact, formatPercent } from "../lib/format";
import { formatStrategicStatus } from "../domain/options";
import type { Dashboard, DashboardHistoryPoint, PortfolioSummary, Position, RuleAlert } from "../domain/types";

const chartColors = ["#0a0a0a", "#155eef", "#0b7a48", "#b86a00", "#c22a2a", "#525252", "#7c3aed", "#0f766e"];
type DashboardPeriod = "1W" | "1M" | "1Y" | "MAX";
type PerformanceDisplayMode = "percent" | "amount";
type DashboardAlertItem = {
  id: string;
  title: string;
  message: string;
  severity: "Info" | "Warning" | "Critical";
  kind: "rule" | "price" | "allocation";
};

const dashboardPeriodOptions: Array<{ value: DashboardPeriod; label: string }> = [
  { value: "1W", label: "1S" },
  { value: "1M", label: "1M" },
  { value: "1Y", label: "1A" },
  { value: "MAX", label: "MAX" }
];

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
        <span>{isRefreshing ? "Mise a jour" : "Actualisation"}</span>
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
  const [period, setPeriod] = useState<DashboardPeriod>("1M");
  const [isAlertsDrawerOpen, setAlertsDrawerOpen] = useState(false);
  const [isAllocationDrawerOpen, setAllocationDrawerOpen] = useState(false);
  const topPositions = useMemo(
    () => [...dashboard.positions].sort((a, b) => b.marketValue - a.marketValue).slice(0, 8),
    [dashboard.positions]
  );
  const missingPricePositions = dashboard.positions.filter((position) => !position.hasMarketPrice);
  const alertItems = useMemo(() => buildDashboardAlerts(dashboard.positions, dashboard.ruleAlerts), [dashboard.positions, dashboard.ruleAlerts]);
  const portfolioDrifts = useMemo(
    () => [...dashboard.portfolios].filter((portfolio) => portfolio.targetWeight > 0).sort((a, b) => Math.abs(b.allocationDrift) - Math.abs(a.allocationDrift)),
    [dashboard.portfolios]
  );
  const lineDrifts = useMemo(
    () => [...dashboard.positions].filter((position) => position.targetWeight !== null).sort((a, b) => Math.abs(b.allocationDrift ?? 0) - Math.abs(a.allocationDrift ?? 0)),
    [dashboard.positions]
  );

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
      </section>

      <section className="dashboardFocusGrid">
        <Panel
          title="Evolution globale"
          subtitle="Historique journalier, sans granularite intraday pour cette version."
          className="chartPanel"
        >
          <TotalValueChart history={dashboard.history} period={period} onPeriodChange={setPeriod} />
        </Panel>
        <Panel title="A traiter" subtitle="Signaux prioritaires" className="watchPanel">
          <DashboardAttentionSummary alerts={alertItems} onOpenDetails={() => setAlertsDrawerOpen(true)} />
        </Panel>
      </section>

      {missingPricePositions.length > 0 ? (
        <Panel title="Cours a lier" subtitle="Associer manuellement les actifs qui ne trouvent pas leur cotation automatiquement." className="bindingPanel">
          <MarketBindingPanel assets={missingPricePositions} />
        </Panel>
      ) : null}

      <section className="grid dashboardGrid">
        <Panel title="Allocation" subtitle="Ecart reel vs cible">
          <AllocationSummary portfolios={portfolioDrifts} positions={lineDrifts} onOpenDetails={() => setAllocationDrawerOpen(true)} />
        </Panel>
        <Panel title="Evolution par portefeuille" subtitle={`${dashboard.portfolios.length} enveloppe(s) suivie(s)`}>
          <PortfolioHistoryChart dashboard={dashboard} period={period} />
        </Panel>
      </section>

      <Panel title="Positions principales" subtitle="Classees par valeur de marche">
        <PositionTable positions={topPositions} />
      </Panel>

      {isAlertsDrawerOpen ? (
        <DashboardDrawer title="Alertes et signaux" subtitle={`${alertItems.length} element(s) a examiner`} onClose={() => setAlertsDrawerOpen(false)}>
          <DashboardAlertList alerts={alertItems} />
        </DashboardDrawer>
      ) : null}

      {isAllocationDrawerOpen ? (
        <DashboardDrawer title="Details d'allocation" subtitle="Portefeuilles et lignes les plus eloignes des objectifs." onClose={() => setAllocationDrawerOpen(false)}>
          <section className="drawerSection">
            <h3>Portefeuilles</h3>
            <PortfolioObjectiveProgress portfolios={dashboard.portfolios} />
          </section>
          <section className="drawerSection">
            <h3>Lignes</h3>
            <LineObjectiveProgress positions={dashboard.positions} />
          </section>
        </DashboardDrawer>
      ) : null}
    </>
  );
}

function SegmentedControl<T extends string>({
  ariaLabel,
  onChange,
  options,
  value
}: {
  ariaLabel: string;
  onChange: (value: T) => void;
  options: Array<{ value: T; label: string }>;
  value: T;
}) {
  return (
    <div className="segmentedControl" aria-label={ariaLabel}>
      {options.map((option) => (
        <button
          className={option.value === value ? "active" : ""}
          key={option.value}
          onClick={() => onChange(option.value)}
          type="button"
        >
          {option.label}
        </button>
      ))}
    </div>
  );
}

function PerformanceIndicator({
  amount,
  displayMode,
  label,
  onToggle,
  percent,
  title
}: {
  amount: number | null;
  displayMode: PerformanceDisplayMode;
  label: string;
  onToggle: () => void;
  percent: number | null;
  title: string;
}) {
  const displayedValue = displayMode === "percent" ? percent : amount;
  const tone = displayedValue === null ? "neutral" : displayedValue >= 0 ? "positive" : "negative";
  const nextUnit = displayMode === "percent" ? "euros" : "pourcentage";

  return (
    <button
      className={`chartPerformanceBadge ${tone}`}
      onClick={onToggle}
      title={`${title} Cliquer pour afficher en ${nextUnit}.`}
      type="button"
    >
      <span>{label}</span>
      <strong>{displayMode === "percent" ? formatSignedPercent(percent) : formatSignedCurrency(amount)}</strong>
    </button>
  );
}

function TotalValueChart({
  history,
  onPeriodChange,
  period
}: {
  history: DashboardHistoryPoint[];
  onPeriodChange: (period: DashboardPeriod) => void;
  period: DashboardPeriod;
}) {
  const [activePoint, setActivePoint] = useState<DashboardHistoryPoint | null>(null);
  const [cursorRatio, setCursorRatio] = useState(1);
  const [periodPerformanceDisplay, setPeriodPerformanceDisplay] = useState<PerformanceDisplayMode>("percent");
  const [patrimonyPerformanceDisplay, setPatrimonyPerformanceDisplay] = useState<PerformanceDisplayMode>("percent");
  const chartRef = useRef<HTMLDivElement | null>(null);
  const points = filterHistoryByPeriod(normalizeHistory(history), period);
  useEffect(() => {
    const handleDocumentMouseMove = (event: MouseEvent) => {
      const chart = chartRef.current;
      if (!chart) {
        return;
      }

      const bounds = chart.getBoundingClientRect();
      const isInsideChart =
        event.clientX >= bounds.left &&
        event.clientX <= bounds.right &&
        event.clientY >= bounds.top &&
        event.clientY <= bounds.bottom;

      if (!isInsideChart) {
        setActivePoint(null);
      }
    };

    document.addEventListener("mousemove", handleDocumentMouseMove);
    return () => document.removeEventListener("mousemove", handleDocumentMouseMove);
  }, []);

  if (points.length < 2) {
    return <p className="emptyState">Ajoutez plusieurs operations ou cours dates pour afficher une evolution.</p>;
  }

  const latestPoint = points.at(-1) ?? points[0];
  const displayedPoint = activePoint && points.some((point) => point.date === activePoint.date) ? activePoint : latestPoint;
  const performanceBaselinePoint = findPerformanceBaselinePoint(points, displayedPoint) ?? displayedPoint;
  const displayedRatio = getValueInvestedRatio(displayedPoint);
  const baselineRatio = getValueInvestedRatio(performanceBaselinePoint);
  const periodPerformance = displayedRatio !== null && baselineRatio !== null ? displayedRatio / baselineRatio - 1 : null;
  const periodPerformanceAmount = baselineRatio !== null
    ? displayedPoint.totalMarketValue - displayedPoint.totalInvested * baselineRatio
    : null;
  const patrimonyPerformance = displayedRatio !== null ? displayedRatio - 1 : null;
  const patrimonyPerformanceAmount = displayedRatio !== null ? displayedPoint.totalMarketValue - displayedPoint.totalInvested : null;
  const chartValues = points.flatMap((point) => [point.totalMarketValue, point.totalInvested]);
  const rawMinValue = Math.min(...chartValues);
  const rawMaxValue = Math.max(...chartValues);
  const domainPadding = Math.max((rawMaxValue - rawMinValue) * 0.08, 1);
  const minValue = Math.max(rawMinValue - domainPadding, 0);
  const maxValue = rawMaxValue + domainPadding;
  const updateActivePointFromMouse = (clientX: number, bounds: DOMRect) => {
    const plotLeft = 92;
    const plotRight = 18;
    const plotWidth = Math.max(bounds.width - plotLeft - plotRight, 1);
    const ratio = Math.min(Math.max((clientX - bounds.left - plotLeft) / plotWidth, 0), 1);
    const index = Math.min(Math.max(Math.round(ratio * (points.length - 1)), 0), points.length - 1);
    setCursorRatio(ratio);
    setActivePoint(points[index]);
  };

  return (
    <div className="chartBlock">
      <div className="chartValueHeader">
        <div className="chartLiveValue">
          <strong>{formatCurrencyCompact(displayedPoint.totalMarketValue)}</strong>
          <div className="chartIndicatorList">
            <PerformanceIndicator
              amount={periodPerformanceAmount}
              displayMode={periodPerformanceDisplay}
              label="Perf. periode"
              onToggle={() => setPeriodPerformanceDisplay(periodPerformanceDisplay === "percent" ? "amount" : "percent")}
              percent={periodPerformance}
              title="Performance periodique: (Valeur / Investi) comparee au debut de periode."
            />
            <PerformanceIndicator
              amount={patrimonyPerformanceAmount}
              displayMode={patrimonyPerformanceDisplay}
              label="Perf. patrimoine"
              onToggle={() => setPatrimonyPerformanceDisplay(patrimonyPerformanceDisplay === "percent" ? "amount" : "percent")}
              percent={patrimonyPerformance}
              title="Performance reelle du patrimoine: Valeur / Investi - 1."
            />
          </div>
        </div>
        <SegmentedControl options={dashboardPeriodOptions} value={period} onChange={onPeriodChange} ariaLabel="Periode du graphique" />
      </div>
      <div
        className="chartCanvas"
        ref={chartRef}
        role="img"
        aria-label="Evolution globale du patrimoine"
        onMouseLeave={() => setActivePoint(null)}
        onMouseMove={(event) => updateActivePointFromMouse(event.clientX, event.currentTarget.getBoundingClientRect())}
      >
        {activePoint ? (
          <div className="chartCursorLabel" style={{ left: `${8 + cursorRatio * 88}%` }}>
            {formatDate(activePoint.date)}
          </div>
        ) : null}
        <ResponsiveContainer width="100%" height={280}>
          <LineChart data={points} margin={{ top: 18, right: 18, bottom: 8, left: 4 }}>
            <CartesianGrid stroke="#deded8" strokeDasharray="4 8" vertical={false} />
            <XAxis dataKey="date" tickFormatter={formatShortDate} axisLine={false} tickLine={false} minTickGap={24} />
            <YAxis
              axisLine={false}
              domain={[minValue, maxValue]}
              tickFormatter={(value) => formatCurrencyCompact(Number(value))}
              tickLine={false}
              width={88}
            />
            <Legend />
            {activePoint ? <ReferenceLine x={activePoint.date} stroke="#0a0a0a" strokeDasharray="3 6" strokeWidth={1} /> : null}
            <Line dataKey="totalInvested" dot={false} activeDot={false} name="Investi" stroke="#b86a00" strokeWidth={2.2} type="monotone" />
            <Line dataKey="totalMarketValue" dot={false} activeDot={false} name="Valeur" stroke="#0a0a0a" strokeWidth={3} type="monotone" />
          </LineChart>
        </ResponsiveContainer>
      </div>
      <div className="chartRange">
        <span>{formatDate(points[0].date)}</span>
        <span>{formatDate(points.at(-1)?.date ?? points[0].date)}</span>
      </div>
    </div>
  );
}

function PortfolioHistoryChart({ dashboard, period }: { dashboard: Dashboard; period: DashboardPeriod }) {
  const points = filterHistoryByPeriod(normalizeHistory(dashboard.history), period);
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

function getValueInvestedRatio(point: DashboardHistoryPoint) {
  if (point.totalInvested <= 0) {
    return null;
  }

  return point.totalMarketValue / point.totalInvested;
}

function formatSignedPercent(value: number | null) {
  if (value === null || !Number.isFinite(value)) {
    return "-";
  }

  return `${value >= 0 ? "+" : ""}${formatPercent(value)}`;
}

function formatSignedCurrency(value: number | null) {
  if (value === null || !Number.isFinite(value)) {
    return "-";
  }

  return `${value >= 0 ? "+" : ""}${formatCurrencyCompact(value)}`;
}

function findPerformanceBaselinePoint(points: DashboardHistoryPoint[], displayedPoint: DashboardHistoryPoint) {
  return points.find(
    (point) =>
      point.date <= displayedPoint.date &&
      point.totalMarketValue > 0 &&
      point.totalInvested > 0
  );
}

function filterHistoryByPeriod(points: DashboardHistoryPoint[], period: DashboardPeriod) {
  if (period === "MAX" || points.length === 0) {
    return points;
  }

  const latestDate = new Date(`${points.at(-1)?.date}T00:00:00`);
  const days = period === "1W" ? 7 : period === "1M" ? 30 : 365;
  const startDate = new Date(latestDate);
  startDate.setDate(latestDate.getDate() - days);
  const filtered = points.filter((point) => new Date(`${point.date}T00:00:00`) >= startDate);
  return filtered.length >= 2 ? filtered : points.slice(-2);
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

function buildDashboardAlerts(positions: Position[], ruleAlerts: RuleAlert[]): DashboardAlertItem[] {
  const driftedPositions = positions
    .filter((position) => position.targetWeight !== null)
    .sort((left, right) => Math.abs(right.allocationDrift ?? 0) - Math.abs(left.allocationDrift ?? 0))
    .slice(0, 5)
    .map((position) => ({
      id: `allocation-${position.portfolioId}-${position.assetId}`,
      title: position.assetName,
      message: `${position.portfolioName} - ecart ${formatPercent(position.allocationDrift ?? 0)} vs cible.`,
      severity: Math.abs(position.allocationDrift ?? 0) >= 0.08 ? "Warning" as const : "Info" as const,
      kind: "allocation" as const
    }));
  const missingPriceAlerts = positions
    .filter((position) => !position.hasMarketPrice)
    .map((position) => ({
      id: `price-${position.portfolioId}-${position.assetId}`,
      title: position.assetName,
      message: "Cours manquant, ligne estimee au PRU.",
      severity: "Warning" as const,
      kind: "price" as const
    }));
  const ruleItems = ruleAlerts.map((alert) => ({
    id: `rule-${alert.ruleId}-${alert.portfolioId ?? "all"}-${alert.assetId ?? "all"}`,
    title: alert.assetName ?? alert.portfolioName ?? alert.ruleName,
    message: alert.message,
    severity: alert.severity,
    kind: "rule" as const
  }));

  return [...ruleItems, ...missingPriceAlerts, ...driftedPositions].sort((left, right) => severityRank(right.severity) - severityRank(left.severity));
}

function severityRank(severity: DashboardAlertItem["severity"]) {
  return severity === "Critical" ? 3 : severity === "Warning" ? 2 : 1;
}

function DashboardAttentionSummary({ alerts, onOpenDetails }: { alerts: DashboardAlertItem[]; onOpenDetails: () => void }) {
  const criticalCount = alerts.filter((alert) => alert.severity === "Critical").length;
  const warningCount = alerts.filter((alert) => alert.severity === "Warning").length;
  const firstAlerts = alerts.slice(0, 3);

  return (
    <div className="attentionSummary">
      <div className="attentionHero">
        <strong>{alerts.length}</strong>
        <span>{alerts.length > 1 ? "signaux actifs" : "signal actif"}</span>
      </div>
      {alerts.length > 0 ? (
        <div className="attentionBadges">
          {criticalCount > 0 ? <span className="attentionBadge critical">{criticalCount} critique(s)</span> : null}
          {warningCount > 0 ? <span className="attentionBadge warning">{warningCount} attention</span> : null}
          <span className="attentionBadge">{alerts.length - criticalCount - warningCount} info</span>
        </div>
      ) : (
        <p className="emptyState">Aucun signal prioritaire pour le moment.</p>
      )}
      {firstAlerts.length > 0 ? (
        <div className="attentionPreview">
          {firstAlerts.map((alert) => (
            <article className={`attentionLine attentionLine-${alert.severity.toLowerCase()}`} key={alert.id}>
              <strong>{alert.title}</strong>
              <span>{alert.message}</span>
            </article>
          ))}
        </div>
      ) : null}
      <button className="ghostButton secondaryButton" disabled={alerts.length === 0} onClick={onOpenDetails} type="button">
        Voir tout
      </button>
    </div>
  );
}

function DashboardAlertList({ alerts }: { alerts: DashboardAlertItem[] }) {
  if (alerts.length === 0) {
    return <p className="emptyState">Aucune alerte active.</p>;
  }

  return (
    <div className="drawerList">
      {alerts.map((alert) => (
        <article className={`watchItem watchItem-${alert.severity.toLowerCase()}`} key={alert.id}>
          <strong>{alert.title}</strong>
          <span>{alert.message}</span>
        </article>
      ))}
    </div>
  );
}

function AllocationSummary({ portfolios, positions, onOpenDetails }: { portfolios: PortfolioSummary[]; positions: Position[]; onOpenDetails: () => void }) {
  const mainPortfolioDrift = portfolios[0];
  const mainLineDrift = positions[0];
  const configuredLineCount = positions.filter((position) => position.targetWeight !== null).length;

  return (
    <div className="allocationSummary">
      <div className="allocationSummaryHero">
        <span>Ecart principal</span>
        <strong>{mainPortfolioDrift ? formatPercent(mainPortfolioDrift.allocationDrift) : "-"}</strong>
        <small>{mainPortfolioDrift?.name ?? "Aucune cle globale"}</small>
      </div>
      <div className="allocationQuickList">
        {mainLineDrift ? (
          <article>
            <strong>{mainLineDrift.assetName}</strong>
            <span>{formatPercent(mainLineDrift.allocationDrift ?? 0)} vs cible</span>
          </article>
        ) : null}
        <article>
          <strong>{configuredLineCount}</strong>
          <span>ligne(s) avec cle cible</span>
        </article>
      </div>
      <button className="ghostButton secondaryButton" onClick={onOpenDetails} type="button">Voir le detail</button>
    </div>
  );
}

function DashboardDrawer({ children, onClose, subtitle, title }: { children: ReactNode; onClose: () => void; subtitle: string; title: string }) {
  useBodyScrollLock(true);

  return (
    <aside className="dashboardDrawerLayer" role="dialog" aria-modal="true" aria-label={title}>
      <button className="dashboardDrawerBackdrop" onClick={onClose} type="button" aria-label="Fermer" />
      <div className="dashboardDrawer">
        <header>
          <div>
            <strong>{title}</strong>
            <span>{subtitle}</span>
          </div>
          <button className="actionIconButton" onClick={onClose} type="button" aria-label="Fermer">
            <X size={18} />
          </button>
        </header>
        <div className="dashboardDrawerContent">{children}</div>
      </div>
    </aside>
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
