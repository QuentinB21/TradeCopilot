import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { portfolioTypes } from "../domain/options";
import type { CreatePortfolioPayload, Portfolio } from "../domain/types";
import { formatCurrency } from "../lib/format";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

const emptyPortfolio: CreatePortfolioPayload = {
  name: "",
  type: "Pea",
  broker: "",
  baseCurrency: "EUR",
  cashBalance: 0
};

export function PortfoliosPage() {
  const queryClient = useQueryClient();
  const portfoliosQuery = useQuery({ queryKey: ["portfolios"], queryFn: tradeCopilotApi.getPortfolios });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CreatePortfolioPayload>(emptyPortfolio);

  const savePortfolio = useMutation({
    mutationFn: () => editingId ? tradeCopilotApi.updatePortfolio(editingId, form) : tradeCopilotApi.createPortfolio(form),
    onSuccess: async () => {
      setEditingId(null);
      setForm(emptyPortfolio);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["portfolios"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
    }
  });

  function edit(portfolio: Portfolio) {
    setEditingId(portfolio.id);
    setForm({
      name: portfolio.name,
      type: portfolio.type,
      broker: portfolio.broker,
      baseCurrency: portfolio.baseCurrency,
      cashBalance: portfolio.cashBalance
    });
  }

  return (
    <>
      <PageHeader title="Portefeuilles" description="Gestion des enveloppes d'investissement et soldes especes." />
      <section className="grid">
        <Panel title={editingId ? "Modifier un portefeuille" : "Nouveau portefeuille"}>
          <form className="form" onSubmit={(event) => { event.preventDefault(); savePortfolio.mutate(); }}>
            <label>Nom<input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required /></label>
            <label>Type<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value as CreatePortfolioPayload["type"] })}>{portfolioTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Courtier<input value={form.broker} onChange={(event) => setForm({ ...form, broker: event.target.value })} required /></label>
            <label>Devise<input value={form.baseCurrency} onChange={(event) => setForm({ ...form, baseCurrency: event.target.value })} required maxLength={3} /></label>
            <label>Cash<input type="number" step="0.01" value={form.cashBalance} onChange={(event) => setForm({ ...form, cashBalance: Number(event.target.value) })} /></label>
            <div className="formActions">
              <button type="submit">{editingId ? "Enregistrer" : "Creer"}</button>
              {editingId ? <button className="secondaryButton" type="button" onClick={() => { setEditingId(null); setForm(emptyPortfolio); }}>Annuler</button> : null}
            </div>
          </form>
        </Panel>

        <Panel title="Portefeuilles existants">
          <QueryState isLoading={portfoliosQuery.isLoading} error={portfoliosQuery.error}>
            <div className="compactList">
              {(portfoliosQuery.data ?? []).map((portfolio) => (
                <button className="entityRow" key={portfolio.id} onClick={() => edit(portfolio)} type="button">
                  <div>
                    <strong>{portfolio.name}</strong>
                    <span>{portfolio.broker} - {portfolio.type}</span>
                  </div>
                  <span>{formatCurrency(portfolio.cashBalance)}</span>
                </button>
              ))}
            </div>
          </QueryState>
        </Panel>
      </section>
    </>
  );
}
