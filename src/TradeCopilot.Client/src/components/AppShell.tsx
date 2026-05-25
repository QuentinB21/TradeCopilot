import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

export type NavigationItem<TKey extends string> = {
  key: TKey;
  label: string;
  icon: LucideIcon;
};

type AppShellProps<TKey extends string> = {
  activeView: TKey;
  navigation: NavigationItem<TKey>[];
  children: ReactNode;
  onNavigate: (view: TKey) => void;
};

export function AppShell<TKey extends string>({ activeView, navigation, children, onNavigate }: AppShellProps<TKey>) {
  return (
    <main className="app">
      <aside className="sidebar">
        <div className="brand">
          <div className="brandMark" aria-hidden="true">
            <img src="/icons/app-icon-white.png" alt="" />
          </div>
          <div>
            <strong>TradeCopilot</strong>
            <span>Pilotage patrimonial</span>
          </div>
        </div>
        <p className="sidebarIntro">
          Suivre les positions, cadrer la strategie et garder les decisions lisibles.
        </p>
        <nav className="nav" aria-label="Navigation principale">
          {navigation.map((item) => {
            const Icon = item.icon;
            return (
              <button
                className={activeView === item.key ? "active" : ""}
                key={item.key}
                onClick={() => onNavigate(item.key)}
                type="button"
                aria-current={activeView === item.key ? "page" : undefined}
              >
                <Icon size={18} />
                {item.label}
              </button>
            );
          })}
        </nav>
        <section className="sidebarGuide" aria-label="Flux de demarrage">
          <span>Ordre conseille</span>
          <strong>Configurer</strong>
          <strong>Importer</strong>
          <strong>Piloter</strong>
        </section>
      </aside>
      <section className="workspace">{children}</section>
    </main>
  );
}
