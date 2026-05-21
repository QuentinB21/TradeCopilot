import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { ActionIconButton } from "../components/ActionIconButton";
import { DecimalInput } from "../components/DecimalInput";
import { allocationRuleStatuses, assetTypes, portfolioTypes, strategicStatuses } from "../domain/options";
import type { AllocationRule, Asset, CreateAllocationRulePayload, CreateAssetPayload, CreatePortfolioPayload, CreateStrategyRulePayload, Portfolio, StrategyRule, UpdateAllocationRulePayload } from "../domain/types";
import { formatPercent } from "../lib/format";
import { parseDecimalInput, parseNullableDecimalInput, toNumberInput } from "../lib/numberInput";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

type PortfolioForm = Omit<CreatePortfolioPayload, "cashBalance" | "targetWeight"> & {
  cashBalance: string;
  targetWeight: string;
};

type AllocationRuleForm = Omit<CreateAllocationRulePayload, "targetWeight" | "minWeight" | "maxWeight"> & {
  targetWeight: string;
  minWeight: string;
  maxWeight: string;
};

type StrategyRuleForm = Omit<CreateStrategyRulePayload, "priority"> & {
  priority: string;
};

type StrategySection = "overview" | "portfolios" | "assets" | "allocation" | "rules";
type StrategyEditor = "portfolio" | "asset" | "allocation" | "rule" | null;

const strategySections: Array<{ key: StrategySection; label: string; detail: string }> = [
  { key: "overview", label: "Vue d'ensemble", detail: "Etat de configuration" },
  { key: "portfolios", label: "Portefeuilles", detail: "Enveloppes et poids" },
  { key: "assets", label: "Actifs", detail: "Univers investissable" },
  { key: "allocation", label: "Repartition", detail: "Cles par portefeuille" },
  { key: "rules", label: "Regles", detail: "Cadre de decision" }
];

const emptyPortfolio: PortfolioForm = {
  name: "",
  type: "Pea",
  broker: "",
  baseCurrency: "EUR",
  cashBalance: "",
  targetWeight: ""
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
  marketSymbol: null,
  strategicStatus: "Conviction"
};

const emptyAllocationRule: AllocationRuleForm = {
  portfolioId: "",
  assetId: "",
  targetWeight: "",
  minWeight: "",
  maxWeight: "",
  status: "Active"
};

const emptyStrategyRule: StrategyRuleForm = {
  portfolioId: null,
  assetId: null,
  name: "",
  description: "",
  triggerCondition: "",
  recommendedAction: "",
  priority: "100",
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
  const [portfolioForm, setPortfolioForm] = useState<PortfolioForm>(emptyPortfolio);
  const [assetForm, setAssetForm] = useState<CreateAssetPayload>(emptyAsset);
  const [allocationForm, setAllocationForm] = useState<AllocationRuleForm>(emptyAllocationRule);
  const [strategyRuleForm, setStrategyRuleForm] = useState<StrategyRuleForm>(emptyStrategyRule);
  const [editingPortfolioId, setEditingPortfolioId] = useState<string | null>(null);
  const [editingAssetId, setEditingAssetId] = useState<string | null>(null);
  const [editingAllocationRuleId, setEditingAllocationRuleId] = useState<string | null>(null);
  const [editingStrategyRuleId, setEditingStrategyRuleId] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<StrategySection>("overview");
  const [activeEditor, setActiveEditor] = useState<StrategyEditor>(null);
  const [allocationPortfolioId, setAllocationPortfolioId] = useState("");

  const portfolios = portfoliosQuery.data ?? [];
  const assets = assetsQuery.data ?? [];
  const portfolioById = useMemo(() => new Map(portfolios.map((portfolio) => [portfolio.id, portfolio])), [portfolios]);
  const assetById = useMemo(() => new Map(assets.map((asset) => [asset.id, asset])), [assets]);
  const allocationRules = allocationRulesQuery.data ?? [];
  const strategyRules = strategyRulesQuery.data ?? [];
  const selectedAllocationPortfolioId = portfolioById.has(allocationPortfolioId)
    ? allocationPortfolioId
    : portfolios[0]?.id ?? "";
  const selectedAllocationPortfolio = portfolioById.get(selectedAllocationPortfolioId);
  const visibleAllocationRules = allocationRules.filter((rule) => rule.portfolioId === selectedAllocationPortfolioId);
  const visibleAllocationWeight = visibleAllocationRules.reduce((total, rule) => total + rule.targetWeight, 0);
  const totalPortfolioWeight = portfolios.reduce((total, portfolio) => total + portfolio.targetWeight, 0);
  const activeRulesCount = strategyRules.filter((rule) => rule.isActive).length;

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
    activateEditor("portfolio");
    setActiveSection("portfolios");
    setEditingPortfolioId(portfolio.id);
    setPortfolioForm({
      name: portfolio.name,
      type: portfolio.type,
      broker: portfolio.broker,
      baseCurrency: portfolio.baseCurrency,
      cashBalance: toNumberInput(portfolio.cashBalance),
      targetWeight: toNumberInput(portfolio.targetWeight)
    });
  };

  const resetAssetForm = () => {
    setAssetForm(emptyAsset);
    setEditingAssetId(null);
  };

  const editAsset = (asset: Asset) => {
    activateEditor("asset");
    setActiveSection("assets");
    setEditingAssetId(asset.id);
    setAssetForm({
      name: asset.name,
      symbol: asset.symbol,
      isin: asset.isin,
      type: asset.type,
      currency: asset.currency,
      sector: asset.sector,
      country: null,
      priceProvider: asset.priceProvider,
      marketSymbol: asset.marketSymbol,
      strategicStatus: asset.strategicStatus
    });
  };

  const resetAllocationForm = () => {
    setAllocationForm(emptyAllocationRule);
    setEditingAllocationRuleId(null);
  };

  const editAllocationRule = (rule: AllocationRule) => {
    activateEditor("allocation");
    setActiveSection("allocation");
    setAllocationPortfolioId(rule.portfolioId);
    setEditingAllocationRuleId(rule.id);
    setAllocationForm({
      portfolioId: rule.portfolioId,
      assetId: rule.assetId,
      targetWeight: toNumberInput(rule.targetWeight),
      minWeight: toNumberInput(rule.minWeight),
      maxWeight: toNumberInput(rule.maxWeight),
      status: rule.status
    });
  };

  const resetStrategyRuleForm = () => {
    setStrategyRuleForm(emptyStrategyRule);
    setEditingStrategyRuleId(null);
  };

  function activateEditor(editor: StrategyEditor) {
    if (editor !== "portfolio") {
      resetPortfolioForm();
    }

    if (editor !== "asset") {
      resetAssetForm();
    }

    if (editor !== "allocation") {
      resetAllocationForm();
    }

    if (editor !== "rule") {
      resetStrategyRuleForm();
    }

    setActiveEditor(editor);
  }

  const openSection = (section: StrategySection) => {
    activateEditor(null);
    setActiveSection(section);
  };

  const createPortfolio = () => {
    resetPortfolioForm();
    activateEditor("portfolio");
    setActiveSection("portfolios");
  };

  const createAsset = () => {
    resetAssetForm();
    activateEditor("asset");
    setActiveSection("assets");
  };

  const createAllocationRule = () => {
    resetAllocationForm();
    activateEditor("allocation");
    setActiveSection("allocation");
    setAllocationForm({ ...emptyAllocationRule, portfolioId: selectedAllocationPortfolioId });
  };

  const createStrategyRule = () => {
    resetStrategyRuleForm();
    activateEditor("rule");
    setActiveSection("rules");
  };

  const editStrategyRule = (rule: StrategyRule) => {
    activateEditor("rule");
    setActiveSection("rules");
    setEditingStrategyRuleId(rule.id);
    setStrategyRuleForm({
      portfolioId: rule.portfolioId,
      assetId: rule.assetId,
      name: rule.name,
      description: rule.description,
      triggerCondition: rule.triggerCondition,
      recommendedAction: rule.recommendedAction,
      priority: toNumberInput(rule.priority),
      isActive: rule.isActive
    });
  };

  const savePortfolio = useMutation({
    mutationFn: () => {
      const payload: CreatePortfolioPayload = {
        ...portfolioForm,
        cashBalance: parseDecimalInput(portfolioForm.cashBalance),
        targetWeight: parseDecimalInput(portfolioForm.targetWeight)
      };
      return editingPortfolioId
        ? tradeCopilotApi.updatePortfolio(editingPortfolioId, payload)
        : tradeCopilotApi.createPortfolio(payload);
    },
    onSuccess: async () => {
      resetPortfolioForm();
      setActiveEditor(null);
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
      setActiveEditor(null);
      await invalidateConfiguration();
    }
  });
  const deleteAsset = useMutation({ mutationFn: tradeCopilotApi.deleteAsset, onSuccess: invalidateConfiguration });
  const saveAllocationRule = useMutation({
    mutationFn: () => {
      const createPayload: CreateAllocationRulePayload = {
        ...allocationForm,
        targetWeight: parseDecimalInput(allocationForm.targetWeight),
        minWeight: parseNullableDecimalInput(allocationForm.minWeight),
        maxWeight: parseNullableDecimalInput(allocationForm.maxWeight)
      };
      if (!editingAllocationRuleId) {
        return tradeCopilotApi.createAllocationRule(createPayload);
      }

      const updatePayload: UpdateAllocationRulePayload = {
        targetWeight: parseDecimalInput(allocationForm.targetWeight),
        minWeight: parseNullableDecimalInput(allocationForm.minWeight),
        maxWeight: parseNullableDecimalInput(allocationForm.maxWeight),
        status: allocationForm.status
      };
      return tradeCopilotApi.updateAllocationRule(editingAllocationRuleId, updatePayload);
    },
    onSuccess: async () => {
      resetAllocationForm();
      setActiveEditor(null);
      await invalidateConfiguration();
    }
  });
  const deleteAllocationRule = useMutation({ mutationFn: tradeCopilotApi.deleteAllocationRule, onSuccess: invalidateConfiguration });
  const saveStrategyRule = useMutation({
    mutationFn: () => {
      const payload: CreateStrategyRulePayload = {
        ...strategyRuleForm,
        priority: parseDecimalInput(strategyRuleForm.priority, 100)
      };
      return editingStrategyRuleId
        ? tradeCopilotApi.updateStrategyRule(editingStrategyRuleId, payload)
        : tradeCopilotApi.createStrategyRule(payload);
    },
    onSuccess: async () => {
      resetStrategyRuleForm();
      setActiveEditor(null);
      await invalidateConfiguration();
    }
  });
  const deleteStrategyRule = useMutation({ mutationFn: tradeCopilotApi.deleteStrategyRule, onSuccess: invalidateConfiguration });

  return (
    <>
      <PageHeader
        title="Configuration strategie"
        description="Parametrage initial : enveloppes, actifs, cles de repartition et regles qui structurent le pilotage."
      />

      <section className="strategyOverview">
        <article>
          <span>Portefeuilles</span>
          <strong>{portfolios.length}</strong>
          <small>Cles globales</small>
        </article>
        <article>
          <span>Actifs</span>
          <strong>{assets.length}</strong>
          <small>Statuts strategiques</small>
        </article>
        <article>
          <span>Cles par ligne</span>
          <strong>{allocationRulesQuery.data?.length ?? 0}</strong>
          <small>Ponderations cibles</small>
        </article>
        <article>
          <span>Regles</span>
          <strong>{strategyRulesQuery.data?.length ?? 0}</strong>
          <small>Decisions explicites</small>
        </article>
      </section>

      <nav className="strategySectionNav" aria-label="Configuration de la strategie">
        {strategySections.map((section) => (
          <button
            aria-current={activeSection === section.key ? "page" : undefined}
            className={activeSection === section.key ? "active" : ""}
            key={section.key}
            onClick={() => openSection(section.key)}
            type="button"
          >
            <strong>{section.label}</strong>
            <span>{section.detail}</span>
          </button>
        ))}
      </nav>

      {activeSection === "overview" ? (
        <section className="strategyLanding">
          <Panel title="Etat de la strategie" subtitle="Verifier le socle avant d'analyser les ecarts.">
            <div className="strategyChecklist">
              <StrategyChecklistItem
                isReady={portfolios.length > 0}
                label="Portefeuilles"
                detail={portfolios.length > 0 ? `${portfolios.length} enveloppe(s) configuree(s).` : "Creer au moins un portefeuille."}
                actionLabel="Configurer"
                onAction={() => openSection("portfolios")}
              />
              <StrategyChecklistItem
                isReady={assets.length > 0}
                label="Actifs"
                detail={assets.length > 0 ? `${assets.length} actif(s) disponibles.` : "Ajouter les titres a suivre."}
                actionLabel="Configurer"
                onAction={() => openSection("assets")}
              />
              <StrategyChecklistItem
                isReady={allocationRules.length > 0}
                label="Repartition"
                detail={allocationRules.length > 0 ? `${allocationRules.length} cle(s) par ligne.` : "Associer les actifs aux portefeuilles."}
                actionLabel="Repartir"
                onAction={() => openSection("allocation")}
              />
              <StrategyChecklistItem
                isReady={strategyRules.length > 0}
                label="Regles"
                detail={strategyRules.length > 0 ? `${activeRulesCount} regle(s) active(s).` : "Formaliser les arbitrages a surveiller."}
                actionLabel="Definir"
                onAction={() => openSection("rules")}
              />
            </div>
          </Panel>
          <Panel title="Cles globales" subtitle="La cible des portefeuilles doit rester explicite.">
            <div className="strategyWeightSummary">
              <strong>{formatPercent(totalPortfolioWeight)}</strong>
              <span>{Math.abs(totalPortfolioWeight - 1) < 0.0001 ? "Repartition globale complete." : "La somme des cles globales differe de 100 %."}</span>
              <button className="ghostButton secondaryButton" onClick={() => openSection("portfolios")} type="button">Voir les portefeuilles</button>
            </div>
          </Panel>
        </section>
      ) : null}

      {activeSection === "portfolios" ? (
        <Panel
          className="strategyPanel strategySectionPanel"
          title="Portefeuilles"
          subtitle="Enveloppes d'investissement et poids cible globaux."
          action={<button className="ghostButton" onClick={createPortfolio} type="button">Ajouter</button>}
        >
          {activeEditor === "portfolio" ? (
            <section className="strategyEditor">
              <h3>{editingPortfolioId ? "Modifier le portefeuille" : "Nouveau portefeuille"}</h3>
              <form className="form" onSubmit={(event) => { event.preventDefault(); savePortfolio.mutate(); }}>
                <label>Nom<input value={portfolioForm.name} onChange={(event) => setPortfolioForm({ ...portfolioForm, name: event.target.value })} required /></label>
                <label>Type<select value={portfolioForm.type} onChange={(event) => setPortfolioForm({ ...portfolioForm, type: event.target.value as CreatePortfolioPayload["type"] })}>{portfolioTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
                <label>Courtier<input value={portfolioForm.broker} onChange={(event) => setPortfolioForm({ ...portfolioForm, broker: event.target.value })} required /></label>
                <label>Cle<DecimalInput min={0} max={1} step="0.01" value={portfolioForm.targetWeight} onChange={(value) => setPortfolioForm({ ...portfolioForm, targetWeight: value })} /></label>
                <div className="formActions">
                  <button type="submit">{editingPortfolioId ? "Mettre a jour" : "Ajouter le portefeuille"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
            </section>
          ) : null}
          {(savePortfolio.error || deletePortfolio.error) ? <p className="stateError">{errorText(savePortfolio.error ?? deletePortfolio.error)}</p> : null}
          <QueryState isLoading={portfoliosQuery.isLoading} error={portfoliosQuery.error}>
            <div className="compactList">
              {portfolios.length === 0 ? <p className="emptyState">Aucun portefeuille configure.</p> : null}
              {portfolios.map((portfolio) => (
                <div className={editingPortfolioId === portfolio.id ? "compactRow editingEntity" : "compactRow"} key={portfolio.id}>
                  <div><strong>{portfolio.name}</strong><span>{portfolio.broker} - {formatPercent(portfolio.targetWeight)}</span></div>
                  <div className="rowActions">
                    {editingPortfolioId === portfolio.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingPortfolioId === portfolio.id} label={`Modifier ${portfolio.name}`} onClick={() => editPortfolio(portfolio)} />
                    <ActionIconButton action="delete" label={`Supprimer ${portfolio.name}`} onClick={() => deletePortfolio.mutate(portfolio.id)} />
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>
      ) : null}

      {activeSection === "assets" ? (
        <Panel
          className="strategyPanel strategySectionPanel"
          title="Actifs"
          subtitle="Titres utilisables dans les allocations et les regles."
          action={<button className="ghostButton" onClick={createAsset} type="button">Ajouter</button>}
        >
          {activeEditor === "asset" ? (
            <section className="strategyEditor">
              <h3>{editingAssetId ? "Modifier l'actif" : "Nouvel actif"}</h3>
              <form className="form" onSubmit={(event) => { event.preventDefault(); saveAsset.mutate(); }}>
                <label>Nom<input value={assetForm.name} onChange={(event) => setAssetForm({ ...assetForm, name: event.target.value })} required /></label>
                <label>Symbole<input value={assetForm.symbol} onChange={(event) => setAssetForm({ ...assetForm, symbol: event.target.value })} required /></label>
                <label>Type<select value={assetForm.type} onChange={(event) => setAssetForm({ ...assetForm, type: event.target.value as CreateAssetPayload["type"] })}>{assetTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
                <label>Statut<select value={assetForm.strategicStatus} onChange={(event) => setAssetForm({ ...assetForm, strategicStatus: event.target.value as CreateAssetPayload["strategicStatus"] })}>{strategicStatuses.map((status) => <option key={status}>{status}</option>)}</select></label>
                <div className="formActions">
                  <button type="submit">{editingAssetId ? "Mettre a jour" : "Ajouter l'actif"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
            </section>
          ) : null}
          {(saveAsset.error || deleteAsset.error) ? <p className="stateError">{errorText(saveAsset.error ?? deleteAsset.error)}</p> : null}
          <QueryState isLoading={assetsQuery.isLoading} error={assetsQuery.error}>
            <div className="compactList">
              {assets.length === 0 ? <p className="emptyState">Aucun actif configure.</p> : null}
              {assets.map((asset) => (
                <div className={editingAssetId === asset.id ? "compactRow editingEntity" : "compactRow"} key={asset.id}>
                  <div><strong>{asset.name}</strong><span>{asset.symbol} - {asset.strategicStatus}</span></div>
                  <div className="rowActions">
                    {editingAssetId === asset.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingAssetId === asset.id} label={`Modifier ${asset.name}`} onClick={() => editAsset(asset)} />
                    <ActionIconButton action="delete" label={`Supprimer ${asset.name}`} onClick={() => deleteAsset.mutate(asset.id)} />
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>
      ) : null}

      {activeSection === "allocation" ? (
        <Panel
          className="strategyPanel strategySectionPanel"
          title="Repartition"
          subtitle="Allocation des actifs dans chaque portefeuille."
          action={<button className="ghostButton" disabled={!selectedAllocationPortfolioId || assets.length === 0} onClick={createAllocationRule} type="button">Ajouter</button>}
        >
          <div className="allocationToolbar">
            <label>
              Portefeuille
              <select value={selectedAllocationPortfolioId} onChange={(event) => setAllocationPortfolioId(event.target.value)} disabled={portfolios.length === 0}>
                {portfolios.length === 0 ? <option value="">Aucun portefeuille</option> : null}
                {portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}
              </select>
            </label>
            <div className="allocationGauge">
              <span>Somme des cles</span>
              <strong>{formatPercent(visibleAllocationWeight)}</strong>
              <small>{selectedAllocationPortfolio?.name ?? "Selectionner un portefeuille"}</small>
            </div>
          </div>
          {activeEditor === "allocation" ? (
            <section className="strategyEditor">
              <h3>{editingAllocationRuleId ? "Modifier la cle" : "Nouvelle cle"}</h3>
              <form className="form" onSubmit={(event) => { event.preventDefault(); saveAllocationRule.mutate(); }}>
                <label>Portefeuille<select value={allocationForm.portfolioId} onChange={(event) => setAllocationForm({ ...allocationForm, portfolioId: event.target.value })} required disabled={Boolean(editingAllocationRuleId)}><option value="">Selectionner</option>{portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
                <label>Actif<select value={allocationForm.assetId} onChange={(event) => setAllocationForm({ ...allocationForm, assetId: event.target.value })} required disabled={Boolean(editingAllocationRuleId)}><option value="">Selectionner</option>{assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.name} - {asset.symbol}</option>)}</select></label>
                <label>Cle<DecimalInput min={0} max={1} step="0.01" value={allocationForm.targetWeight} onChange={(value) => setAllocationForm({ ...allocationForm, targetWeight: value })} /></label>
                <label>Statut<select value={allocationForm.status} onChange={(event) => setAllocationForm({ ...allocationForm, status: event.target.value as CreateAllocationRulePayload["status"] })}>{allocationRuleStatuses.map((status) => <option key={status}>{status}</option>)}</select></label>
                <div className="formActions">
                  <button type="submit" disabled={!allocationForm.portfolioId || !allocationForm.assetId}>{editingAllocationRuleId ? "Mettre a jour" : "Ajouter la cle"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
            </section>
          ) : null}
          {(saveAllocationRule.error || deleteAllocationRule.error) ? <p className="stateError">{errorText(saveAllocationRule.error ?? deleteAllocationRule.error)}</p> : null}
          <QueryState isLoading={allocationRulesQuery.isLoading} error={allocationRulesQuery.error}>
            <div className="compactList">
              {!selectedAllocationPortfolioId ? <p className="emptyState">Creer un portefeuille avant de definir sa repartition.</p> : null}
              {selectedAllocationPortfolioId && visibleAllocationRules.length === 0 ? <p className="emptyState">Aucune cle definie pour ce portefeuille.</p> : null}
              {visibleAllocationRules.map((rule) => (
                <div className={editingAllocationRuleId === rule.id ? "compactRow editingEntity" : "compactRow"} key={rule.id}>
                  <div>
                    <strong>{assetById.get(rule.assetId)?.name ?? "Actif"}</strong>
                    <span>{assetById.get(rule.assetId)?.symbol ?? "Symbole"} - {formatPercent(rule.targetWeight)} - {rule.status}</span>
                  </div>
                  <div className="rowActions">
                    {editingAllocationRuleId === rule.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingAllocationRuleId === rule.id} label={`Modifier la cle de ${assetById.get(rule.assetId)?.name ?? "l'actif"}`} onClick={() => editAllocationRule(rule)} />
                    <ActionIconButton action="delete" label={`Supprimer la cle de ${assetById.get(rule.assetId)?.name ?? "l'actif"}`} onClick={() => deleteAllocationRule.mutate(rule.id)} />
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>
      ) : null}

      {activeSection === "rules" ? (
        <Panel
          className="strategyPanel strategySectionPanel"
          title="Regles"
          subtitle="Cadre de decision explicite et maintenable."
          action={<button className="ghostButton" onClick={createStrategyRule} type="button">Ajouter</button>}
        >
          {activeEditor === "rule" ? (
            <section className="strategyEditor strategyRuleEditor">
              <h3>{editingStrategyRuleId ? "Modifier la regle" : "Nouvelle regle"}</h3>
              <form className="form" onSubmit={(event) => { event.preventDefault(); saveStrategyRule.mutate(); }}>
                <label>Nom<input value={strategyRuleForm.name} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, name: event.target.value })} required /></label>
                <label>Description<input value={strategyRuleForm.description} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, description: event.target.value })} required /></label>
                <label>Portefeuille<select value={strategyRuleForm.portfolioId ?? ""} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, portfolioId: event.target.value || null })}><option value="">Tous</option>{portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
                <label>Actif<select value={strategyRuleForm.assetId ?? ""} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, assetId: event.target.value || null })}><option value="">Tous</option>{assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.name} - {asset.symbol}</option>)}</select></label>
                <label>Condition<input value={strategyRuleForm.triggerCondition ?? ""} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, triggerCondition: event.target.value || null })} /></label>
                <label>Action recommandee<input value={strategyRuleForm.recommendedAction} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, recommendedAction: event.target.value })} required /></label>
                <label>Priorite<DecimalInput value={strategyRuleForm.priority} onChange={(value) => setStrategyRuleForm({ ...strategyRuleForm, priority: value })} /></label>
                <div className="formActions">
                  <button type="submit">{editingStrategyRuleId ? "Mettre a jour" : "Ajouter la regle"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
            </section>
          ) : null}
          {(saveStrategyRule.error || deleteStrategyRule.error) ? <p className="stateError">{errorText(saveStrategyRule.error ?? deleteStrategyRule.error)}</p> : null}
          <QueryState isLoading={strategyRulesQuery.isLoading} error={strategyRulesQuery.error}>
            <div className="compactList">
              {strategyRules.length === 0 ? <p className="emptyState">Aucune regle de decision configuree.</p> : null}
              {strategyRules.map((rule) => (
                <div className={editingStrategyRuleId === rule.id ? "compactRow editingEntity" : "compactRow"} key={rule.id}>
                  <div>
                    <strong>{rule.name}</strong>
                    <span>{rule.recommendedAction}</span>
                    <span>{rule.portfolioId ? portfolioById.get(rule.portfolioId)?.name : "Tous portefeuilles"} - {rule.assetId ? assetById.get(rule.assetId)?.name : "Tous actifs"}</span>
                  </div>
                  <div className="rowActions">
                    {editingStrategyRuleId === rule.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingStrategyRuleId === rule.id} label={`Modifier ${rule.name}`} onClick={() => editStrategyRule(rule)} />
                    <ActionIconButton action="delete" label={`Supprimer ${rule.name}`} onClick={() => deleteStrategyRule.mutate(rule.id)} />
                  </div>
                </div>
              ))}
            </div>
          </QueryState>
        </Panel>
      ) : null}
    </>
  );
}

function StrategyChecklistItem({
  actionLabel,
  detail,
  isReady,
  label,
  onAction
}: {
  actionLabel: string;
  detail: string;
  isReady: boolean;
  label: string;
  onAction: () => void;
}) {
  return (
    <article className={isReady ? "strategyChecklistItem ready" : "strategyChecklistItem"}>
      <div>
        <strong>{label}</strong>
        <span>{detail}</span>
      </div>
      <button className="ghostButton secondaryButton" onClick={onAction} type="button">{actionLabel}</button>
    </article>
  );
}
