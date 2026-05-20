import { BarChart3, BrainCircuit, BriefcaseBusiness, LineChart, ReceiptText, ShieldCheck } from "lucide-react";
import { useState } from "react";
import { AppShell, type NavigationItem } from "./components/AppShell";
import { AssetsPage } from "./pages/AssetsPage";
import { AssistantPage } from "./pages/AssistantPage";
import { DashboardPage } from "./pages/DashboardPage";
import { PortfoliosPage } from "./pages/PortfoliosPage";
import { StrategyPage } from "./pages/StrategyPage";
import { TransactionsPage } from "./pages/TransactionsPage";

type ViewKey = "dashboard" | "portfolios" | "assets" | "transactions" | "assistant" | "strategy";

const navigation: NavigationItem<ViewKey>[] = [
  { key: "dashboard", label: "Dashboard", icon: BarChart3 },
  { key: "portfolios", label: "Portefeuilles", icon: BriefcaseBusiness },
  { key: "assets", label: "Actifs", icon: LineChart },
  { key: "transactions", label: "Transactions", icon: ReceiptText },
  { key: "assistant", label: "Assistant", icon: BrainCircuit },
  { key: "strategy", label: "Strategie", icon: ShieldCheck }
];

export function App() {
  const [activeView, setActiveView] = useState<ViewKey>("dashboard");

  return (
    <AppShell activeView={activeView} navigation={navigation} onNavigate={setActiveView}>
      {activeView === "dashboard" ? <DashboardPage /> : null}
      {activeView === "portfolios" ? <PortfoliosPage /> : null}
      {activeView === "assets" ? <AssetsPage /> : null}
      {activeView === "transactions" ? <TransactionsPage /> : null}
      {activeView === "assistant" ? <AssistantPage /> : null}
      {activeView === "strategy" ? <StrategyPage /> : null}
    </AppShell>
  );
}
