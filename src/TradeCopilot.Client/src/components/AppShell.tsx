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
          <div className="brandMark">TC</div>
          <div>
            <strong>TradeCopilot</strong>
            <span>Copilote patrimonial</span>
          </div>
        </div>
        <nav className="nav" aria-label="Navigation principale">
          {navigation.map((item) => {
            const Icon = item.icon;
            return (
              <button
                className={activeView === item.key ? "active" : ""}
                key={item.key}
                onClick={() => onNavigate(item.key)}
                type="button"
              >
                <Icon size={18} />
                {item.label}
              </button>
            );
          })}
        </nav>
      </aside>
      <section className="workspace">{children}</section>
    </main>
  );
}
