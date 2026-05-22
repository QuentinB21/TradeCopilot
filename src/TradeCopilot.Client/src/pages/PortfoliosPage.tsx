import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { ActionIconButton } from "../components/ActionIconButton";
import { DecimalInput } from "../components/DecimalInput";
import { portfolioTypes } from "../domain/options";
import type { CreatePortfolioPayload, Portfolio } from "../domain/types";
import { formatCurrency } from "../lib/format";
import { parseDecimalInput, toNumberInput } from "../lib/numberInput";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { Pagination } from "../components/Pagination";
import { QueryState } from "../components/QueryState";
import { usePagination } from "../hooks/usePagination";

type PortfolioForm = Omit<CreatePortfolioPayload, "cashBalance" | "targetWeight"> & {
  cashBalance: string;
  targetWeight: string;
};

const emptyPortfolio: PortfolioForm = {
  name: "",
  type: "Pea",
  broker: "",
  baseCurrency: "EUR",
  cashBalance: "",
  targetWeight: ""
};

export function PortfoliosPage() {
  const queryClient = useQueryClient();
  const portfoliosQuery = useQuery({ queryKey: ["portfolios"], queryFn: tradeCopilotApi.getPortfolios });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<PortfolioForm>(emptyPortfolio);
  const portfolios = portfoliosQuery.data ?? [];
  const portfolioPagination = usePagination(portfolios);
  const formTargetWeight = parseDecimalInput(form.targetWeight);
  const isTargetWeightInvalid = formTargetWeight > 1;
  const projectedTargetWeight = portfolios
    .filter((portfolio) => portfolio.id !== editingId)
    .reduce((total, portfolio) => total + portfolio.targetWeight, formTargetWeight);
  const isProjectedTargetWeightInvalid = projectedTargetWeight > 1.000001;

  const toPayload = (): CreatePortfolioPayload => ({
    ...form,
    cashBalance: parseDecimalInput(form.cashBalance),
    targetWeight: parseDecimalInput(form.targetWeight)
  });

  const savePortfolio = useMutation({
    mutationFn: () => editingId ? tradeCopilotApi.updatePortfolio(editingId, toPayload()) : tradeCopilotApi.createPortfolio(toPayload()),
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
    if (editingId === portfolio.id) {
      setEditingId(null);
      setForm(emptyPortfolio);
      return;
    }

    setEditingId(portfolio.id);
    setForm({
      name: portfolio.name,
      type: portfolio.type,
      broker: portfolio.broker,
      baseCurrency: portfolio.baseCurrency,
      cashBalance: toNumberInput(portfolio.cashBalance),
      targetWeight: toNumberInput(portfolio.targetWeight)
    });
  }

  return (
    <>
      <PageHeader title="Portefeuilles" description="Creer les enveloppes d'investissement, suivre leur solde especes et cadrer leur poids cible." />
      <section className="portfolioWorkspace">
        <Panel className="portfolioEditorPanel" title={editingId ? "Modifier un portefeuille" : "Nouveau portefeuille"} subtitle="Une enveloppe correspond a un compte ou une plateforme suivie.">
          <form className="form" onSubmit={(event) => { event.preventDefault(); savePortfolio.mutate(); }}>
            <label>Nom<input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required /></label>
            <label>Type<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value as CreatePortfolioPayload["type"] })}>{portfolioTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Courtier<input value={form.broker} onChange={(event) => setForm({ ...form, broker: event.target.value })} required /></label>
            <label>Devise<input value={form.baseCurrency} onChange={(event) => setForm({ ...form, baseCurrency: event.target.value })} required maxLength={3} /></label>
            <label>Solde especes<DecimalInput step="0.01" value={form.cashBalance} onChange={(value) => setForm({ ...form, cashBalance: value })} /><small>Liquidites disponibles dans ce portefeuille, hors titres detenus.</small></label>
            <label>Cle globale<DecimalInput step="0.01" min={0} max={1} value={form.targetWeight} onChange={(value) => setForm({ ...form, targetWeight: value })} /></label>
            <div className="formActions">
              <button type="submit" disabled={isTargetWeightInvalid || isProjectedTargetWeightInvalid}>{editingId ? "Enregistrer" : "Creer"}</button>
              {editingId ? <button className="secondaryButton" type="button" onClick={() => { setEditingId(null); setForm(emptyPortfolio); }}>Annuler</button> : null}
            </div>
          </form>
          {isTargetWeightInvalid ? <p className="stateError">Une cle globale ne peut pas depasser 100 %.</p> : null}
          {!isTargetWeightInvalid && isProjectedTargetWeightInvalid ? <p className="stateError">Cette cle ferait depasser 100 % pour les portefeuilles.</p> : null}
          {savePortfolio.error ? <p className="stateError">{savePortfolio.error instanceof Error ? savePortfolio.error.message : "Operation impossible pour le moment."}</p> : null}
        </Panel>

        <Panel className="portfolioListPanel" title="Portefeuilles existants" subtitle="Selectionner une ligne pour la corriger.">
          <QueryState isLoading={portfoliosQuery.isLoading} error={portfoliosQuery.error}>
            <div className="compactList">
              {portfolioPagination.pageItems.map((portfolio) => (
                <div className={editingId === portfolio.id ? "compactRow editingEntity" : "compactRow"} key={portfolio.id}>
                  <div>
                    <strong>{portfolio.name}</strong>
                    <span>{portfolio.broker} - {portfolio.type} - cle {Math.round(portfolio.targetWeight * 100)}%</span>
                  </div>
                  <div className="rowEnd">
                    <span>{formatCurrency(portfolio.cashBalance)}</span>
                    {editingId === portfolio.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingId === portfolio.id} label={`Modifier ${portfolio.name}`} onClick={() => edit(portfolio)} />
                  </div>
                </div>
              ))}
            </div>
            <Pagination {...portfolioPagination} itemLabel="portefeuilles" onPageChange={portfolioPagination.setPage} />
          </QueryState>
        </Panel>
      </section>
    </>
  );
}
