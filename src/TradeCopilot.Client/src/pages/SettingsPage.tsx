import { useState } from "react";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import {
  dashboardRefreshIntervals,
  readDashboardRefreshInterval,
  saveDashboardRefreshInterval
} from "../lib/appSettings";

export function SettingsPage() {
  const [dashboardRefreshInterval, setDashboardRefreshInterval] = useState(readDashboardRefreshInterval);

  function changeDashboardRefreshInterval(intervalMs: number) {
    setDashboardRefreshInterval(intervalMs);
    saveDashboardRefreshInterval(intervalMs);
  }

  return (
    <>
      <PageHeader
        title="Parametres"
        description="Regler les preferences d'affichage et d'actualisation de l'application."
      />
      <section className="settingsGrid">
        <Panel title="Dashboard" subtitle="Rythme de valorisation">
          <div className="settingsForm">
            <label className="settingsField">
              Delai d'actualisation
              <select
                value={dashboardRefreshInterval}
                onChange={(event) => changeDashboardRefreshInterval(Number(event.target.value))}
              >
                {dashboardRefreshIntervals.map((interval) => (
                  <option key={interval.value} value={interval.value}>{interval.label}</option>
                ))}
              </select>
            </label>
            <p className="settingHint">
              Le dashboard applique ce delai pour rafraichir les valorisations et le point de cours du jour.
            </p>
          </div>
        </Panel>
      </section>
    </>
  );
}
