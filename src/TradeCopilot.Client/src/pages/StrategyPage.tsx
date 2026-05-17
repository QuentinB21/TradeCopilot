import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { allocationRuleStatuses, assetTypes, portfolioTypes, strategicStatuses } from "../domain/options";
import type { AllocationRule, Asset, CreateAllocationRulePayload, CreateAssetPayload, CreatePortfolioPayload, CreateStrategyRulePayload, Portfolio, StrategyRule, UpdateAllocationRulePayload } from "../domain/types";
import { formatPercent } from "../lib/format";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

const emptyPortfolio: CreatePortfolioPayload = {
  name: "",
  type: "Pea",
  broker: "",
  baseCurrency: "EUR",
  cashBalance: 0,
  targetWeight: 0
};

const emptyAsset: CreateAssetPayload = {
  name: "",
  symbol: "",
  isin: null,
  type: "Stock",
  currency: "EUR",
  sector: "",
  country: "",
  priceProvider: "manual",
  strategicStatus: "Conviction"
};

const emptyAllocationRule: CreateAllocationRulePayload = {
  portfolioId: "",
  assetId: "",
  targetWeight: 0,
  minWeight: null,
  maxWeight: null,
  status: "Active"
};

const emptyStrategyRule: CreateStrategyRulePayload = {
  portfolioId: null,
  assetId: null,
  name: "",
  description: "",
  triggerCondition: "",
  recommendedAction: "",
  priority: 100,
  isActive: true
};

function errorText(error: unknown) {
  return error instanceof Error ? error.message : "Operation impossible pour le moment.";
}

export function StrategyPage() {
  const queryClient = useQueryClient();
  const portfoliosQuery = useQuery({ queryKey: ["portfolios"], queryFn: tradeCopilotApi.getPortfolios });
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const allocationRulesQuery = useQuery({ queryKey: ["allocation-rules"], queryFn: tradeCopilotApi.getAllocationRules });
  const strategyRulesQuery = useQuery({ queryKey: ["strategy-rules"], queryFn: tradeCopilotApi.getStrategyRules });
  const [portfolioForm, setPortfolioForm] = useState<CreatePortfolioPayload>(emptyPortfolio);
  const [assetForm, setAssetForm] = useState<CreateAssetPayload>(emptyAsset);
  const [allocationForm, setAllocationForm] = useState<CreateAllocationRulePayload>(emptyAllocationRule);
  const [strategyRuleForm, setStrategyRuleForm] = useState<CreateStrategyRulePayload>(emptyStrategyRule);
  const [editingPortfolioId, setEditingPortfolioId] = useState<string | null>(null);
  const [editingAssetId, setEditingAssetId] = useState<string | null>(null);
  const [editingAllocationRuleId, setEditingAllocationRuleId] = useState<string | null>(null);
  const [editingStrategyRuleId, setEditingStrategyRuleId] = useState<string | null>(null);

  const portfolios = portfoliosQuery.data ?? [];
  const assets = assetsQuery.data ?? [];
  const portfolioById = useMemo(() => new Map(portfolios.map((portfolio) => [portfolio.id, portfolio])), [portfolios]);
  const assetById = useMemo(() => new Map(assets.map((asset) => [asset.id, asset])), [assets]);

  const invalidateConfiguration = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["portfolios"] }),
      queryClient.invalidateQueries({ queryKey: ["assets"] }),
      queryClient.invalidateQueries({ queryKey: ["allocation-rules"] }),
      queryClient.invalidateQueries({ queryKey: ["strategy-rules"] }),
      queryClient.invalidateQueries({ queryKey: ["strategy"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] })
    ]);
  };

  const resetPortfolioForm = () => {
    setPortfolioForm(emptyPortfolio);
    setEditingPortfolioId(null);
  };

  const editPortfolio = (portfolio: Portfolio) => {
    setEditingPortfolioId(portfolio.id);
    setPortfolioForm({
      name: portfolio.name,
      type: portfolio.type,
      broker: portfolio.broker,
      baseCurrency: portfolio.baseCurrency,
      cashBalance: portfolio.cashBalance,
      targetWeight: portfolio.targetWeight
    });
  };

  const resetAssetForm = () => {
    setAssetForm(emptyAsset);
    setEditingAssetId(null);
  };

  const editAsset = (asset: Asset) => {
    setEditingAssetId(asset.id);
    setAssetForm({
      name: asset.name,
      symbol: asset.symbol,
      isin: asset.isin,
      type: asset.type,
      currency: asset.currency,
      sector: asset.sector,
      country: null,
      priceProvider: null,
      strategicStatus: asset.strategicStatus
    });
  };

  const resetAllocationForm = () => {
    setAllocationForm(emptyAllocationRule);
    setEditingAllocationRuleId(null);
  };

  const editAllocationRule = (rule: AllocationRule) => {
    setEditingAllocationRuleId(rule.id);
    setAllocationForm({
      portfolioId: rule.portfolioId,
      assetId: rule.assetId,
      targetWeight: rule.targetWeight,
      minWeight: rule.minWeight,
      maxWeight: rule.maxWeight,
      status: rule.status
    });
  };

  const resetStrategyRuleForm = () => {
    setStrategyRuleForm(emptyStrategyRule);
    setEditingStrategyRuleId(null);
  };

  const editStrategyRule = (rule: StrategyRule) => {
    setEditingStrategyRuleId(rule.id);
    setStrategyRuleForm({
      portfolioId: rule.portfolioId,
      assetId: rule.assetId,
      name: rule.name,
      description: rule.description,
      triggerCondition: rule.triggerCondition,
      recommendedAction: rule.recommendedAction,
      priority: rule.priority,
      isActive: rule.isActive
    });
  };

  const savePortfolio = useMutation({
    mutationFn: () => editingPortfolioId
      ? tradeCopilotApi.updatePortfolio(editingPortfolioId, portfolioForm)
      : tradeCopilotApi.createPortfolio(portfolioForm),
    onSuccess: async () => {
      resetPortfolioForm();
      await invalidateConfiguration();
    }
  });
  const deletePortfolio = useMutation({ mutationFn: tradeCopilotApi.deletePortfolio, onSuccess: invalidateConfiguration });
  const saveAsset = useMutation({
    mutationFn: () => editingAssetId
      ? tradeCopilotApi.updateAsset(editingAssetId, assetForm)
      : tradeCopilotApi.createAsset(assetForm),
    onSuccess: async () => {
      resetAssetForm();
      await invalidateConfiguration();
    }
  });
  const deleteAsset = useMutation({ mutationFn: tradeCopilotApi.deleteAsset, onSuccess: invalidateConfiguration });
  const saveAllocationRule = useMutation({
    mutationFn: () => {
      if (!editingAllocationRuleId) {
        return tradeCopilotApi.createAllocationRule(allocationForm);
      }

      const payload: UpdateAllocationRulePayload = {
        targetWeight: allocationForm.targetWeight,
        minWeight: allocationForm.minWeight,
        maxWeight: allocationForm.maxWeight,
        status: allocationForm.status
      };
      return tradeCopilotApi.updateAllocationRule(editingAllocationRuleId, payload);
    },
    onSuccess: async () => {
      resetAllocationForm();
      await invalidateConfiguration();
    }
  });
  const deleteAllocationRule = useMutation({ mutationFn: tradeCopilotApi.deleteAllocationRule, onSuccess: invalidateConfiguration });
  const saveStrategyRule = useMutation({
    mutationFn: () => editingStrategyRuleId
      ? tradeCopilotApi.updateStrategyRule(editingStrategyRuleId, strategyRuleForm)
      : tradeCopilotApi.createStrategyRule(strategyRuleForm),
    onSuccess: async () => {
      resetStrategyRuleForm();
      await invalidateConfiguration();
    }
  });
  const deleteStrategyRule = useMutation({ mutationFn: tradeCopilotApi.deleteStrategyRule, onSuccess: invalidateConfiguration });

  return (
    <>
      <PageHeader
        title="Configuration strategie"
        description="Parametrage initial : enveloppes, actifs, cles de repartition et regles de decision."
      />

      <section className="configurationGrid">
        <Panel title="Portefeuilles et cles globales" subtitle="Ex: 0.80 pour 80%">
          <form className="form" onSubmit={(event) => { event.preventDefault(); savePortfolio.mutate(); }}>
            <label>Nom<input value={portfolioForm.name} onChange={(event) => setPortfolioForm({ ...portfolioForm, name: event.target.value })} required /></label>
            <label>Type<select value={portfolioForm.type} onChange={(event) => setPortfolioForm({ ...portfolioForm, type: event.target.value as CreatePortfolioPayload["type"] })}>{portfolioTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Courtier<input value={portfolioForm.broker} onChange={(event) => setPortfolioForm({ ...portfolioForm, broker: event.target.value })} required /></label>
            <label>Cle<input type="number" min={0} max={1} step="0.01" value={portfolioForm.targetWeight} onChange={(event) => setPortfolioForm({ ...portfolioForm, targetWeight: Number(event.target.value) })} /></label>
            <div className="formActions">
              <button type="submit">{editingPortfolioId ? "Mettre a jour" : "Ajouter le portefeuille"}</button>
              {editingPortfolioId && <button className="secondaryButton" type="button" onClick={resetPortfolioForm}>Annuler</button>}
            </div>
          </form>
          {(savePortfolio.error || deletePortfolio.error) && <p className="stateError">{errorText(savePortfolio.error ?? deletePortfolio.error)}</p>}
          <QueryState isLoading={portfoliosQuery.isLoading} error={portfoliosQuery.error}>
            <div className="compactList">
              {portfolios.map((portfolio) => (
                <div className="compactRow" key={portfolio.id}>
                  <div><strong>{portfolio.name}</strong><span>{portfolio.broker} - {formatPercent(portfolio.targetWeight)}</span></div>
                  <div className="rowActions">
                    <button className="linkButton" type="button" onClick={() => editPortfolio(portfolio)}>Modifier</button>
                    <button className="linkButton dangerText" type="button" onClick={() => deletePortfolio.mutate(portfolio.id)}>Supprimer</button>
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>

        <Panel title="Actifs configurables">
          <form className="form" onSubmit={(event) => { event.preventDefault(); saveAsset.mutate(); }}>
            <label>Nom<input value={assetForm.name} onChange={(event) => setAssetForm({ ...assetForm, name: event.target.value })} required /></label>
            <label>Symbole<input value={assetForm.symbol} onChange={(event) => setAssetForm({ ...assetForm, symbol: event.target.value })} required /></label>
            <label>Type<select value={assetForm.type} onChange={(event) => setAssetForm({ ...assetForm, type: event.target.value as CreateAssetPayload["type"] })}>{assetTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Statut<select value={assetForm.strategicStatus} onChange={(event) => setAssetForm({ ...assetForm, strategicStatus: event.target.value as CreateAssetPayload["strategicStatus"] })}>{strategicStatuses.map((status) => <option key={status}>{status}</option>)}</select></label>
            <div className="formActions">
              <button type="submit">{editingAssetId ? "Mettre a jour" : "Ajouter l'actif"}</button>
              {editingAssetId && <button className="secondaryButton" type="button" onClick={resetAssetForm}>Annuler</button>}
            </div>
          </form>
          {(saveAsset.error || deleteAsset.error) && <p className="stateError">{errorText(saveAsset.error ?? deleteAsset.error)}</p>}
          <QueryState isLoading={assetsQuery.isLoading} error={assetsQuery.error}>
            <div className="compactList">
              {assets.map((asset) => (
                <div className="compactRow" key={asset.id}>
                  <div><strong>{asset.symbol}</strong><span>{asset.name} - {asset.strategicStatus}</span></div>
                  <div className="rowActions">
                    <button className="linkButton" type="button" onClick={() => editAsset(asset)}>Modifier</button>
                    <button className="linkButton dangerText" type="button" onClick={() => deleteAsset.mutate(asset.id)}>Supprimer</button>
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>

        <Panel title="Cles par ligne" subtitle="Ponderation dans chaque portefeuille">
          <form className="form" onSubmit={(event) => { event.preventDefault(); saveAllocationRule.mutate(); }}>
            <label>Portefeuille<select value={allocationForm.portfolioId} onChange={(event) => setAllocationForm({ ...allocationForm, portfolioId: event.target.value })} required disabled={Boolean(editingAllocationRuleId)}><option value="">Selectionner</option>{portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
            <label>Actif<select value={allocationForm.assetId} onChange={(event) => setAllocationForm({ ...allocationForm, assetId: event.target.value })} required disabled={Boolean(editingAllocationRuleId)}><option value="">Selectionner</option>{assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.symbol} - {asset.name}</option>)}</select></label>
            <label>Cle<input type="number" min={0} max={1} step="0.01" value={allocationForm.targetWeight} onChange={(event) => setAllocationForm({ ...allocationForm, targetWeight: Number(event.target.value) })} /></label>
            <label>Statut<select value={allocationForm.status} onChange={(event) => setAllocationForm({ ...allocationForm, status: event.target.value as CreateAllocationRulePayload["status"] })}>{allocationRuleStatuses.map((status) => <option key={status}>{status}</option>)}</select></label>
            <div className="formActions">
              <button type="submit" disabled={!allocationForm.portfolioId || !allocationForm.assetId}>{editingAllocationRuleId ? "Mettre a jour" : "Ajouter la cle"}</button>
              {editingAllocationRuleId && <button className="secondaryButton" type="button" onClick={resetAllocationForm}>Annuler</button>}
            </div>
          </form>
          {(saveAllocationRule.error || deleteAllocationRule.error) && <p className="stateError">{errorText(saveAllocationRule.error ?? deleteAllocationRule.error)}</p>}
          <QueryState isLoading={allocationRulesQuery.isLoading} error={allocationRulesQuery.error}>
            <div className="compactList">
              {(allocationRulesQuery.data ?? []).map((rule) => (
                <div className="compactRow" key={rule.id}>
                  <div>
                    <strong>{assetById.get(rule.assetId)?.symbol ?? "Actif"}</strong>
                    <span>{portfolioById.get(rule.portfolioId)?.name ?? "Portefeuille"} - {formatPercent(rule.targetWeight)} - {rule.status}</span>
                  </div>
                  <div className="rowActions">
                    <button className="linkButton" type="button" onClick={() => editAllocationRule(rule)}>Modifier</button>
                    <button className="linkButton dangerText" type="button" onClick={() => deleteAllocationRule.mutate(rule.id)}>Supprimer</button>
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>

        <Panel title="Regles de decision">
          <form className="form" onSubmit={(event) => { event.preventDefault(); saveStrategyRule.mutate(); }}>
            <label>Nom<input value={strategyRuleForm.name} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, name: event.target.value })} required /></label>
            <label>Description<input value={strategyRuleForm.description} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, description: event.target.value })} required /></label>
            <label>Portefeuille<select value={strategyRuleForm.portfolioId ?? ""} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, portfolioId: event.target.value || null })}><option value="">Tous</option>{portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
            <label>Actif<select value={strategyRuleForm.assetId ?? ""} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, assetId: event.target.value || null })}><option value="">Tous</option>{assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.symbol} - {asset.name}</option>)}</select></label>
            <label>Condition<input value={strategyRuleForm.triggerCondition ?? ""} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, triggerCondition: event.target.value || null })} /></label>
            <label>Action recommandee<input value={strategyRuleForm.recommendedAction} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, recommendedAction: event.target.value })} required /></label>
            <label>Priorite<input type="number" value={strategyRuleForm.priority} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, priority: Number(event.target.value) })} /></label>
            <div className="formActions">
              <button type="submit">{editingStrategyRuleId ? "Mettre a jour" : "Ajouter la regle"}</button>
              {editingStrategyRuleId && <button className="secondaryButton" type="button" onClick={resetStrategyRuleForm}>Annuler</button>}
            </div>
          </form>
          {(saveStrategyRule.error || deleteStrategyRule.error) && <p className="stateError">{errorText(saveStrategyRule.error ?? deleteStrategyRule.error)}</p>}
          <QueryState isLoading={strategyRulesQuery.isLoading} error={strategyRulesQuery.error}>
            <div className="compactList">
              {(strategyRulesQuery.data ?? []).map((rule) => (
                <div className="compactRow" key={rule.id}>
                  <div>
                    <strong>{rule.name}</strong>
                    <span>{rule.recommendedAction}</span>
                    <span>{rule.portfolioId ? portfolioById.get(rule.portfolioId)?.name : "Tous portefeuilles"} - {rule.assetId ? assetById.get(rule.assetId)?.symbol : "Tous actifs"}</span>
                  </div>
                  <div className="rowActions">
                    <button className="linkButton" type="button" onClick={() => editStrategyRule(rule)}>Modifier</button>
                    <button className="linkButton dangerText" type="button" onClick={() => deleteStrategyRule.mutate(rule.id)}>Supprimer</button>
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>
      </section>
    </>
  );
}
