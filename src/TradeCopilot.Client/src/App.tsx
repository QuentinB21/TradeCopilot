import { BarChart3, BrainCircuit, BriefcaseBusiness, LineChart, ReceiptText, Settings2, ShieldCheck } from "lucide-react";
import { lazy, Suspense, useState } from "react";
import { useAuth } from "./auth/AuthProvider";
import { AppShell, type NavigationItem } from "./components/AppShell";

type ViewKey = "dashboard" | "portfolios" | "assets" | "transactions" | "assistant" | "strategy" | "settings";

const DashboardPage = lazy(() => import("./pages/DashboardPage").then((module) => ({ default: module.DashboardPage })));
const PortfoliosPage = lazy(() => import("./pages/PortfoliosPage").then((module) => ({ default: module.PortfoliosPage })));
const AssetsPage = lazy(() => import("./pages/AssetsPage").then((module) => ({ default: module.AssetsPage })));
const TransactionsPage = lazy(() => import("./pages/TransactionsPage").then((module) => ({ default: module.TransactionsPage })));
const AssistantPage = lazy(() => import("./pages/AssistantPage").then((module) => ({ default: module.AssistantPage })));
const StrategyPage = lazy(() => import("./pages/StrategyPage").then((module) => ({ default: module.StrategyPage })));
const SettingsPage = lazy(() => import("./pages/SettingsPage").then((module) => ({ default: module.SettingsPage })));

const navigation: NavigationItem<ViewKey>[] = [
  { key: "dashboard", label: "Dashboard", icon: BarChart3 },
  { key: "portfolios", label: "Portefeuilles", icon: BriefcaseBusiness },
  { key: "assets", label: "Actifs", icon: LineChart },
  { key: "transactions", label: "Transactions", icon: ReceiptText },
  { key: "assistant", label: "Assistant", icon: BrainCircuit },
  { key: "strategy", label: "Strategie", icon: ShieldCheck },
  { key: "settings", label: "Parametres", icon: Settings2 }
];

export function App() {
  const [activeView, setActiveView] = useState<ViewKey>("dashboard");
  const auth = useAuth();
  const userName = auth.user?.profile.name ?? auth.user?.profile.preferred_username ?? auth.user?.profile.email ?? null;

  return (
    <AppShell
      activeView={activeView}
      navigation={navigation}
      userName={userName}
      onNavigate={setActiveView}
      onSignOut={auth.user ? () => void auth.signOut() : undefined}
    >
      <Suspense fallback={<section className="stateBlock">Chargement de la vue...</section>}>
        {activeView === "dashboard" ? <DashboardPage /> : null}
        {activeView === "portfolios" ? <PortfoliosPage /> : null}
        {activeView === "assets" ? <AssetsPage /> : null}
        {activeView === "transactions" ? <TransactionsPage /> : null}
        {activeView === "assistant" ? <AssistantPage /> : null}
        {activeView === "strategy" ? <StrategyPage /> : null}
        {activeView === "settings" ? <SettingsPage /> : null}
      </Suspense>
    </AppShell>
  );
}
