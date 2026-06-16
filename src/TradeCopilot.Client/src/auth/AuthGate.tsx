import type { ReactNode } from "react";
import { useAuth } from "./AuthProvider";

const appIconUrl = `${import.meta.env.BASE_URL}icons/app-icon-white.png`;

export function AuthGate({ children }: { children: ReactNode }) {
  const auth = useAuth();

  if (auth.isLoading) {
    return (
      <main className="authShell">
        <section className="authPanel">
          <div className="authSpinner" aria-hidden="true" />
          <strong>TradeCopilot</strong>
          <span>
            {auth.loadingReason === "callback"
              ? "Connexion validee, retour a l'application..."
              : "Ouverture de la session..."}
          </span>
        </section>
      </main>
    );
  }

  if (!auth.isAuthenticated) {
    return (
      <main className="authShell">
        <section className="authPanel">
          <div className="brandMark authBrandMark" aria-hidden="true">
            <img src={appIconUrl} alt="" />
          </div>
          <strong>TradeCopilot</strong>
          <span>Acces securise a vos donnees patrimoniales.</span>
          {auth.error ? <p className="stateError">{auth.error}</p> : null}
          <button type="button" onClick={() => void auth.signIn()}>Se connecter</button>
        </section>
      </main>
    );
  }

  return children;
}
