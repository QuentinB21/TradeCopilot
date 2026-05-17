import type { ReactNode } from "react";

type QueryStateProps = {
  isLoading: boolean;
  error: unknown;
  children: ReactNode;
};

export function QueryState({ isLoading, error, children }: QueryStateProps) {
  if (isLoading) {
    return <section className="stateBlock">Chargement des donnees...</section>;
  }

  if (error) {
    return <section className="stateBlock stateError">Impossible de charger les donnees.</section>;
  }

  return <>{children}</>;
}
