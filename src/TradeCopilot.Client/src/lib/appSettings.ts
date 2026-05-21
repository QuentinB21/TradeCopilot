export const dashboardRefreshIntervals = [
  { label: "5 min", value: 5 * 60 * 1000 },
  { label: "10 min", value: 10 * 60 * 1000 },
  { label: "30 min", value: 30 * 60 * 1000 },
  { label: "1 h", value: 60 * 60 * 1000 }
] as const;

export const defaultDashboardRefreshInterval = dashboardRefreshIntervals[0].value;

const dashboardRefreshIntervalStorageKey = "tradecopilot.settings.dashboardRefreshInterval";
const legacyDashboardRefreshIntervalStorageKey = "tradecopilot.dashboard.refreshInterval";

export function readDashboardRefreshInterval() {
  const storedInterval = Number(
    window.localStorage.getItem(dashboardRefreshIntervalStorageKey)
      ?? window.localStorage.getItem(legacyDashboardRefreshIntervalStorageKey)
  );

  return dashboardRefreshIntervals.some((interval) => interval.value === storedInterval)
    ? storedInterval
    : defaultDashboardRefreshInterval;
}

export function saveDashboardRefreshInterval(intervalMs: number) {
  if (!dashboardRefreshIntervals.some((interval) => interval.value === intervalMs)) {
    return;
  }

  window.localStorage.setItem(dashboardRefreshIntervalStorageKey, String(intervalMs));
}
