import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { ActionIconButton } from "../components/ActionIconButton";
import { DecimalInput } from "../components/DecimalInput";
import { assetTypeOptions, formatRepartitionStatus, formatStrategicStatus, portfolioTypes, repartitionStatusOptions, strategicStatusOptions } from "../domain/options";
import type {
  Asset,
  CreateAssetPayload,
  CreatePortfolioPayload,
  CreateRepartitionPayload,
  CreateStrategyRulePayload,
  Portfolio,
  Repartition,
  RuleComparisonOperator,
  RuleConditionMetric,
  RuleDefinition,
  RuleEffectStrength,
  RuleEffectType,
  RuleSeverity,
  RuleTimeUnit,
  StrategyRule,
  UpdateRepartitionPayload
} from "../domain/types";
import { formatPercent } from "../lib/format";
import { parseDecimalInput, parseNullableDecimalInput, toNumberInput } from "../lib/numberInput";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { Pagination } from "../components/Pagination";
import { QueryState } from "../components/QueryState";
import { usePagination } from "../hooks/usePagination";

type PortfolioForm = Omit<CreatePortfolioPayload, "cashBalance" | "targetWeight"> & {
  cashBalance: string;
  targetWeight: string;
};

type RepartitionForm = Omit<CreateRepartitionPayload, "targetWeight" | "minWeight" | "maxWeight"> & {
  targetWeight: string;
  minWeight: string;
  maxWeight: string;
};

type StrategyRuleForm = {
  name: string;
  description: string;
  priorityLevel: RulePriorityLevel;
  isActive: boolean;
  targetScope: "AllAssets" | "SpecificAsset" | "PortfolioAssets";
  portfolioId: string;
  assetId: string;
  metric: RuleConditionMetric;
  operator: RuleComparisonOperator;
  value: string;
  periodAmount: string;
  periodUnit: RuleTimeUnit;
  effectType: RuleEffectType;
  effectStrength: RuleEffectStrength;
  severity: RuleSeverity;
  message: string;
};

type RulePriorityLevel = "High" | "Normal" | "Low";

type RuleOption<T extends string> = {
  value: T;
  label: string;
  detail?: string;
};

const ruleTargetScopeOptions: RuleOption<StrategyRuleForm["targetScope"]>[] = [
  { value: "AllAssets", label: "Tous les actifs", detail: "La regle est evaluee sur chaque ligne suivie." },
  { value: "SpecificAsset", label: "Un actif precis", detail: "La regle cible uniquement l'actif choisi." },
  { value: "PortfolioAssets", label: "Les actifs d'un portefeuille", detail: "La regle cible les lignes d'une enveloppe." }
];

const ruleMetricOptions: RuleOption<RuleConditionMetric>[] = [
  { value: "Always", label: "Toujours actif" },
  { value: "PriceChangePercent", label: "Variation du cours" },
  { value: "AllocationDrift", label: "Ecart a la cible" },
  { value: "UnrealizedGainPercent", label: "Gain latent" }
];

const ruleOperatorOptions: RuleOption<RuleComparisonOperator>[] = [
  { value: "LessThanOrEqual", label: "Inferieur ou egal" },
  { value: "GreaterThanOrEqual", label: "Superieur ou egal" },
  { value: "Equal", label: "Egal" }
];

const ruleTimeUnitOptions: RuleOption<RuleTimeUnit>[] = [
  { value: "Day", label: "jours" },
  { value: "Week", label: "semaines" },
  { value: "Month", label: "mois" },
  { value: "Year", label: "annees" }
];

const ruleEffectOptions: RuleOption<RuleEffectType>[] = [
  { value: "AlertOnly", label: "Alerter uniquement", detail: "Affiche un signal sans modifier l'assistant." },
  { value: "BlockBuy", label: "Ne pas acheter", detail: "Met la proposition d'achat a zero." },
  { value: "ReduceBuy", label: "Acheter moins", detail: "Reduit le poids dans l'assistant." },
  { value: "PrioritizeBuy", label: "Prioriser l'achat", detail: "Augmente le poids dans l'assistant." },
  { value: "RequireReview", label: "Demander verification", detail: "Conserve le montant mais marque la ligne." }
];

const ruleStrengthOptions: RuleOption<RuleEffectStrength>[] = [
  { value: "Soft", label: "Souple" },
  { value: "Hard", label: "Forte" }
];

const ruleSeverityOptions: RuleOption<RuleSeverity>[] = [
  { value: "Info", label: "Information" },
  { value: "Warning", label: "Attention" },
  { value: "Critical", label: "Critique" }
];

const rulePriorityOptions: RuleOption<RulePriorityLevel>[] = [
  { value: "High", label: "Haute", detail: "S'applique avant les autres regles." },
  { value: "Normal", label: "Normale", detail: "Ordre standard pour la plupart des regles." },
  { value: "Low", label: "Basse", detail: "S'applique apres les regles plus importantes." }
];

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
  country: "",
  priceProvider: "manual",
  marketSymbol: null,
  strategicStatus: "Conviction"
};

const emptyRepartition: RepartitionForm = {
  portfolioId: "",
  assetId: "",
  targetWeight: "",
  minWeight: "",
  maxWeight: "",
  status: "Active"
};

const emptyStrategyRule: StrategyRuleForm = {
  name: "",
  description: "",
  priorityLevel: "Normal",
  isActive: true,
  targetScope: "AllAssets",
  portfolioId: "",
  assetId: "",
  metric: "PriceChangePercent",
  operator: "LessThanOrEqual",
  value: "10",
  periodAmount: "1",
  periodUnit: "Month",
  effectType: "RequireReview",
  effectStrength: "Soft",
  severity: "Warning",
  message: "Verifier la ligne avant toute decision."
};

function errorText(error: unknown) {
  return error instanceof Error ? error.message : "Operation impossible pour le moment.";
}

function labelFor<T extends string>(options: RuleOption<T>[], value: T) {
  return options.find((option) => option.value === value)?.label ?? value;
}

function priorityLevelFromValue(priority: number): RulePriorityLevel {
  if (priority <= 50) {
    return "High";
  }

  if (priority >= 150) {
    return "Low";
  }

  return "Normal";
}

function priorityValueFromLevel(level: RulePriorityLevel) {
  return level === "High" ? 10 : level === "Low" ? 200 : 100;
}

function hydrateStrategyRuleForm(rule: StrategyRule): StrategyRuleForm {
  const definition = rule.definition;
  const targetScope = definition?.target.mode === "Specific"
    ? "SpecificAsset"
    : definition?.target.mode === "PortfolioAssets"
    ? "PortfolioAssets"
    : "AllAssets";

  return {
    name: rule.name,
    description: rule.description,
    priorityLevel: priorityLevelFromValue(rule.priority),
    isActive: rule.isActive,
    targetScope,
    portfolioId: definition?.target.portfolioId ?? rule.portfolioId ?? "",
    assetId: definition?.target.assetId ?? rule.assetId ?? "",
    metric: definition?.condition.metric ?? "PriceChangePercent",
    operator: definition?.condition.operator ?? "LessThanOrEqual",
    value: definition?.condition.value == null ? "" : toNumberInput(definition.condition.value * 100),
    periodAmount: definition?.condition.period ? toNumberInput(definition.condition.period.amount) : "1",
    periodUnit: definition?.condition.period?.unit ?? "Month",
    effectType: definition?.effect.type ?? "RequireReview",
    effectStrength: definition?.effect.strength ?? "Soft",
    severity: definition?.effect.severity ?? "Warning",
    message: definition?.effect.message ?? rule.recommendedAction
  };
}

function buildRuleDefinition(form: StrategyRuleForm): RuleDefinition {
  const isAlways = form.metric === "Always";
  const targetPortfolioId = form.targetScope === "PortfolioAssets" ? form.portfolioId || null : null;
  const targetAssetId = form.targetScope === "SpecificAsset" ? form.assetId || null : null;

  return {
    version: 1,
    target: {
      type: "Asset",
      mode: form.targetScope === "SpecificAsset" ? "Specific" : form.targetScope === "PortfolioAssets" ? "PortfolioAssets" : "All",
      portfolioId: targetPortfolioId,
      assetId: targetAssetId
    },
    condition: {
      metric: form.metric,
      operator: form.operator,
      value: isAlways ? null : parseDecimalInput(form.value) / 100,
      unit: isAlways ? "None" : "Percent",
      period: form.metric === "PriceChangePercent"
        ? { amount: Math.max(1, Math.trunc(parseDecimalInput(form.periodAmount, 1))), unit: form.periodUnit }
        : null
    },
    effect: {
      type: form.effectType,
      strength: form.effectStrength,
      severity: form.severity,
      message: form.message.trim()
    }
  };
}

function describeRuleCondition(form: StrategyRuleForm) {
  if (form.metric === "Always") {
    return "Toujours active";
  }

  const metric = labelFor(ruleMetricOptions, form.metric).toLowerCase();
  const operator = labelFor(ruleOperatorOptions, form.operator).toLowerCase();
  const period = form.metric === "PriceChangePercent"
    ? ` sur ${parseDecimalInput(form.periodAmount, 1)} ${labelFor(ruleTimeUnitOptions, form.periodUnit)}`
    : "";
  return `${metric} ${operator} ${parseDecimalInput(form.value)} %${period}`;
}

function describeRuleTarget(form: StrategyRuleForm, portfolioById: Map<string, Portfolio>, assetById: Map<string, Asset>) {
  if (form.targetScope === "SpecificAsset") {
    return assetById.get(form.assetId)?.name ?? "Actif a choisir";
  }

  if (form.targetScope === "PortfolioAssets") {
    return `Actifs de ${portfolioById.get(form.portfolioId)?.name ?? "portefeuille a choisir"}`;
  }

  return "Tous les actifs suivis";
}

function buildStrategyRulePayload(form: StrategyRuleForm, portfolioById: Map<string, Portfolio>, assetById: Map<string, Asset>): CreateStrategyRulePayload {
  const definition = buildRuleDefinition(form);
  const condition = describeRuleCondition(form);
  return {
    portfolioId: definition.target.portfolioId,
    assetId: definition.target.assetId,
    name: form.name.trim(),
    description: form.description.trim(),
    triggerCondition: `${describeRuleTarget(form, portfolioById, assetById)} - ${condition}`,
    recommendedAction: form.message.trim(),
    definition,
    priority: priorityValueFromLevel(form.priorityLevel),
    isActive: form.isActive
  };
}

function isStrategyRuleFormInvalid(form: StrategyRuleForm) {
  if (!form.name.trim() || !form.description.trim() || !form.message.trim()) {
    return true;
  }

  if (form.targetScope === "SpecificAsset" && !form.assetId) {
    return true;
  }

  if (form.targetScope === "PortfolioAssets" && !form.portfolioId) {
    return true;
  }

  if (form.metric !== "Always" && form.value.trim() === "") {
    return true;
  }

  return form.metric === "PriceChangePercent" && parseDecimalInput(form.periodAmount, 0) <= 0;
}

export function StrategyPage() {
  const queryClient = useQueryClient();
  const portfoliosQuery = useQuery({ queryKey: ["portfolios"], queryFn: tradeCopilotApi.getPortfolios });
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const repartitionsQuery = useQuery({ queryKey: ["repartitions"], queryFn: tradeCopilotApi.getRepartitions });
  const strategyRulesQuery = useQuery({ queryKey: ["strategy-rules"], queryFn: tradeCopilotApi.getStrategyRules });
  const [portfolioForm, setPortfolioForm] = useState<PortfolioForm>(emptyPortfolio);
  const [assetForm, setAssetForm] = useState<CreateAssetPayload>(emptyAsset);
  const [repartitionForm, setRepartitionForm] = useState<RepartitionForm>(emptyRepartition);
  const [strategyRuleForm, setStrategyRuleForm] = useState<StrategyRuleForm>(emptyStrategyRule);
  const [editingPortfolioId, setEditingPortfolioId] = useState<string | null>(null);
  const [editingAssetId, setEditingAssetId] = useState<string | null>(null);
  const [editingRepartitionId, setEditingRepartitionId] = useState<string | null>(null);
  const [editingStrategyRuleId, setEditingStrategyRuleId] = useState<string | null>(null);
  const [activeSection, setActiveSection] = useState<StrategySection>("overview");
  const [activeEditor, setActiveEditor] = useState<StrategyEditor>(null);
  const [allocationPortfolioId, setAllocationPortfolioId] = useState("");

  const portfolios = portfoliosQuery.data ?? [];
  const assets = assetsQuery.data ?? [];
  const portfolioById = useMemo(() => new Map(portfolios.map((portfolio) => [portfolio.id, portfolio])), [portfolios]);
  const assetById = useMemo(() => new Map(assets.map((asset) => [asset.id, asset])), [assets]);
  const repartitions = repartitionsQuery.data ?? [];
  const strategyRules = strategyRulesQuery.data ?? [];
  const selectedAllocationPortfolioId = portfolioById.has(allocationPortfolioId)
    ? allocationPortfolioId
    : portfolios[0]?.id ?? "";
  const selectedAllocationPortfolio = portfolioById.get(selectedAllocationPortfolioId);
  const visibleRepartitions = repartitions.filter((repartition) => repartition.portfolioId === selectedAllocationPortfolioId);
  const visibleAllocationWeight = visibleRepartitions.reduce((total, repartition) => total + repartition.targetWeight, 0);
  const isAllocationWeightOverTarget = visibleAllocationWeight > 1.000001;
  const allocationTargetWeight = parseDecimalInput(repartitionForm.targetWeight);
  const isAllocationTargetWeightInvalid = allocationTargetWeight > 1;
  const projectedAllocationWeight = repartitions
    .filter((repartition) => repartition.portfolioId === repartitionForm.portfolioId && repartition.id !== editingRepartitionId)
    .reduce((total, repartition) => total + repartition.targetWeight, allocationTargetWeight);
  const isProjectedAllocationWeightInvalid = Boolean(repartitionForm.portfolioId)
    && projectedAllocationWeight > 1.000001;
  const totalPortfolioWeight = portfolios.reduce((total, portfolio) => total + portfolio.targetWeight, 0);
  const portfolioTargetWeight = parseDecimalInput(portfolioForm.targetWeight);
  const isPortfolioTargetWeightInvalid = portfolioTargetWeight > 1;
  const projectedPortfolioWeight = portfolios
    .filter((portfolio) => portfolio.id !== editingPortfolioId)
    .reduce((total, portfolio) => total + portfolio.targetWeight, portfolioTargetWeight);
  const isProjectedPortfolioWeightInvalid = projectedPortfolioWeight > 1.000001;
  const isPortfolioWeightOverTarget = totalPortfolioWeight > 1.000001;
  const activeRulesCount = strategyRules.filter((rule) => rule.isActive).length;
  const isRuleFormInvalid = isStrategyRuleFormInvalid(strategyRuleForm);
  const portfolioPagination = usePagination(portfolios);
  const assetPagination = usePagination(assets);
  const allocationPagination = usePagination(visibleRepartitions);
  const rulePagination = usePagination(strategyRules);

  const invalidateConfiguration = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["portfolios"] }),
      queryClient.invalidateQueries({ queryKey: ["assets"] }),
      queryClient.invalidateQueries({ queryKey: ["repartitions"] }),
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
    if (activeEditor === "portfolio" && editingPortfolioId === portfolio.id) {
      activateEditor(null);
      return;
    }

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
    if (activeEditor === "asset" && editingAssetId === asset.id) {
      activateEditor(null);
      return;
    }

    activateEditor("asset");
    setActiveSection("assets");
    setEditingAssetId(asset.id);
    setAssetForm({
      name: asset.name,
      symbol: asset.symbol,
      isin: asset.isin,
      type: asset.type,
      currency: asset.currency,
      country: null,
      priceProvider: asset.priceProvider,
      marketSymbol: asset.marketSymbol,
      strategicStatus: asset.strategicStatus
    });
  };

  const resetRepartitionForm = () => {
    setRepartitionForm(emptyRepartition);
    setEditingRepartitionId(null);
  };

  const editRepartition = (repartition: Repartition) => {
    if (activeEditor === "allocation" && editingRepartitionId === repartition.id) {
      activateEditor(null);
      return;
    }

    activateEditor("allocation");
    setActiveSection("allocation");
    setAllocationPortfolioId(repartition.portfolioId);
    setEditingRepartitionId(repartition.id);
    setRepartitionForm({
      portfolioId: repartition.portfolioId,
      assetId: repartition.assetId,
      targetWeight: toNumberInput(repartition.targetWeight),
      minWeight: toNumberInput(repartition.minWeight),
      maxWeight: toNumberInput(repartition.maxWeight),
      status: repartition.status
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
      resetRepartitionForm();
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

  const createRepartition = () => {
    resetRepartitionForm();
    activateEditor("allocation");
    setActiveSection("allocation");
    setRepartitionForm({ ...emptyRepartition, portfolioId: selectedAllocationPortfolioId });
  };

  const createStrategyRule = () => {
    resetStrategyRuleForm();
    activateEditor("rule");
    setActiveSection("rules");
  };

  const editStrategyRule = (rule: StrategyRule) => {
    if (activeEditor === "rule" && editingStrategyRuleId === rule.id) {
      activateEditor(null);
      return;
    }

    activateEditor("rule");
    setActiveSection("rules");
    setEditingStrategyRuleId(rule.id);
    setStrategyRuleForm(hydrateStrategyRuleForm(rule));
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
  const saveRepartition = useMutation({
    mutationFn: () => {
      const createPayload: CreateRepartitionPayload = {
        ...repartitionForm,
        targetWeight: parseDecimalInput(repartitionForm.targetWeight),
        minWeight: parseNullableDecimalInput(repartitionForm.minWeight),
        maxWeight: parseNullableDecimalInput(repartitionForm.maxWeight)
      };
      if (!editingRepartitionId) {
        return tradeCopilotApi.createRepartition(createPayload);
      }

      const updatePayload: UpdateRepartitionPayload = {
        targetWeight: parseDecimalInput(repartitionForm.targetWeight),
        minWeight: parseNullableDecimalInput(repartitionForm.minWeight),
        maxWeight: parseNullableDecimalInput(repartitionForm.maxWeight),
        status: repartitionForm.status
      };
      return tradeCopilotApi.updateRepartition(editingRepartitionId, updatePayload);
    },
    onSuccess: async () => {
      resetRepartitionForm();
      setActiveEditor(null);
      await invalidateConfiguration();
    }
  });
  const deleteRepartition = useMutation({ mutationFn: tradeCopilotApi.deleteRepartition, onSuccess: invalidateConfiguration });
  const saveStrategyRule = useMutation({
    mutationFn: () => {
      const payload = buildStrategyRulePayload(strategyRuleForm, portfolioById, assetById);
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
                isReady={repartitions.length > 0}
                label="Repartition"
                detail={repartitions.length > 0 ? `${repartitions.length} cle(s) par ligne.` : "Associer les actifs aux portefeuilles."}
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
            <div className={isPortfolioWeightOverTarget ? "strategyWeightSummary warning" : "strategyWeightSummary"}>
              <strong>{formatPercent(totalPortfolioWeight)}</strong>
              <span>{isPortfolioWeightOverTarget ? "La somme des cles globales depasse 100 %." : Math.abs(totalPortfolioWeight - 1) < 0.0001 ? "Repartition globale complete." : "La somme des cles globales differe de 100 %."}</span>
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
                  <button type="submit" disabled={isPortfolioTargetWeightInvalid || isProjectedPortfolioWeightInvalid}>{editingPortfolioId ? "Mettre a jour" : "Ajouter le portefeuille"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
              {isPortfolioTargetWeightInvalid ? <p className="stateError">Une cle globale ne peut pas depasser 100 %.</p> : null}
              {!isPortfolioTargetWeightInvalid && isProjectedPortfolioWeightInvalid ? <p className="stateError">Cette cle ferait depasser 100 % pour les portefeuilles.</p> : null}
            </section>
          ) : null}
          {(savePortfolio.error || deletePortfolio.error) ? <p className="stateError">{errorText(savePortfolio.error ?? deletePortfolio.error)}</p> : null}
          <QueryState isLoading={portfoliosQuery.isLoading} error={portfoliosQuery.error}>
            <div className="compactList">
              {portfolios.length === 0 ? <p className="emptyState">Aucun portefeuille configure.</p> : null}
              {portfolioPagination.pageItems.map((portfolio) => (
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
            <Pagination {...portfolioPagination} itemLabel="portefeuilles" onPageChange={portfolioPagination.setPage} />
          </QueryState>
        </Panel>
      ) : null}

      {activeSection === "assets" ? (
        <Panel
          className="strategyPanel strategySectionPanel"
          title="Actifs"
          subtitle="Titres deja ajoutes depuis la page Actifs et utilisables dans les allocations."
        >
          {activeEditor === "asset" ? (
            <section className="strategyEditor">
              <h3>{editingAssetId ? "Modifier l'actif" : "Nouvel actif"}</h3>
              <form className="form" onSubmit={(event) => { event.preventDefault(); saveAsset.mutate(); }}>
                <label>Nom<input value={assetForm.name} onChange={(event) => setAssetForm({ ...assetForm, name: event.target.value })} required /></label>
                <label>Symbole<input value={assetForm.symbol} onChange={(event) => setAssetForm({ ...assetForm, symbol: event.target.value })} required /></label>
                <label>Nature<select value={assetForm.type} onChange={(event) => setAssetForm({ ...assetForm, type: event.target.value as CreateAssetPayload["type"] })}>{assetTypeOptions.map((type) => <option value={type.value} key={type.value}>{type.label}</option>)}</select></label>
                <label>Role strategique<select value={assetForm.strategicStatus} onChange={(event) => setAssetForm({ ...assetForm, strategicStatus: event.target.value as CreateAssetPayload["strategicStatus"] })}>{strategicStatusOptions.map((status) => <option value={status.value} key={status.value}>{status.label}</option>)}</select></label>
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
              {assets.length === 0 ? <p className="emptyState">Aucun actif configure. Utilisez la page Actifs pour rechercher et ajouter un titre.</p> : null}
              {assetPagination.pageItems.map((asset) => (
                <div className={editingAssetId === asset.id ? "compactRow editingEntity" : "compactRow"} key={asset.id}>
                  <div><strong>{asset.name}</strong><span>{asset.symbol} - {formatStrategicStatus(asset.strategicStatus)}</span></div>
                  <div className="rowActions">
                    {editingAssetId === asset.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingAssetId === asset.id} label={`Modifier ${asset.name}`} onClick={() => editAsset(asset)} />
                    <ActionIconButton action="delete" label={`Supprimer ${asset.name}`} onClick={() => deleteAsset.mutate(asset.id)} />
                  </div>
                </div>
              ))}
            </div>
            <Pagination {...assetPagination} itemLabel="actifs" onPageChange={assetPagination.setPage} />
          </QueryState>
        </Panel>
      ) : null}

      {activeSection === "allocation" ? (
        <Panel
          className="strategyPanel strategySectionPanel"
          title="Repartition"
          subtitle="Allocation des actifs dans chaque portefeuille."
          action={<button className="ghostButton" disabled={!selectedAllocationPortfolioId || assets.length === 0} onClick={createRepartition} type="button">Ajouter</button>}
        >
          <div className="allocationToolbar">
            <label>
              Portefeuille
              <select value={selectedAllocationPortfolioId} onChange={(event) => setAllocationPortfolioId(event.target.value)} disabled={portfolios.length === 0}>
                {portfolios.length === 0 ? <option value="">Aucun portefeuille</option> : null}
                {portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}
              </select>
            </label>
            <div className={isAllocationWeightOverTarget ? "allocationGauge warning" : "allocationGauge"}>
              <span>Somme des cles</span>
              <strong>{formatPercent(visibleAllocationWeight)}</strong>
              <small>{selectedAllocationPortfolio?.name ?? "Selectionner un portefeuille"}</small>
              {isAllocationWeightOverTarget ? <small className="allocationWarning">La repartition depasse 100 %. Corrigez les cles de ce portefeuille.</small> : null}
            </div>
          </div>
          {activeEditor === "allocation" ? (
            <section className="strategyEditor">
              <h3>{editingRepartitionId ? "Modifier la cle" : "Nouvelle cle"}</h3>
              <form className="form" onSubmit={(event) => { event.preventDefault(); saveRepartition.mutate(); }}>
                <label>Portefeuille<select value={repartitionForm.portfolioId} onChange={(event) => setRepartitionForm({ ...repartitionForm, portfolioId: event.target.value })} required disabled={Boolean(editingRepartitionId)}><option value="">Selectionner</option>{portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
                <label>Actif<select value={repartitionForm.assetId} onChange={(event) => setRepartitionForm({ ...repartitionForm, assetId: event.target.value })} required disabled={Boolean(editingRepartitionId)}><option value="">Selectionner</option>{assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.name} - {asset.symbol}</option>)}</select></label>
                <label>Cle<DecimalInput min={0} max={1} step="0.01" value={repartitionForm.targetWeight} onChange={(value) => setRepartitionForm({ ...repartitionForm, targetWeight: value })} /></label>
                <label>Etat de repartition<select value={repartitionForm.status} onChange={(event) => setRepartitionForm({ ...repartitionForm, status: event.target.value as CreateRepartitionPayload["status"] })}>{repartitionStatusOptions.map((status) => <option value={status.value} key={status.value}>{status.label}</option>)}</select></label>
                <div className="formActions">
                  <button type="submit" disabled={!repartitionForm.portfolioId || !repartitionForm.assetId || isAllocationTargetWeightInvalid || isProjectedAllocationWeightInvalid}>{editingRepartitionId ? "Mettre a jour" : "Ajouter la cle"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
              {isAllocationTargetWeightInvalid ? <p className="stateError">Une cle individuelle ne peut pas depasser 100 %.</p> : null}
              {!isAllocationTargetWeightInvalid && isProjectedAllocationWeightInvalid ? <p className="stateError">Cette cle ferait depasser 100 % pour ce portefeuille.</p> : null}
            </section>
          ) : null}
          {(saveRepartition.error || deleteRepartition.error) ? <p className="stateError">{errorText(saveRepartition.error ?? deleteRepartition.error)}</p> : null}
          <QueryState isLoading={repartitionsQuery.isLoading} error={repartitionsQuery.error}>
            <div className="compactList">
              {!selectedAllocationPortfolioId ? <p className="emptyState">Creer un portefeuille avant de definir sa repartition.</p> : null}
              {selectedAllocationPortfolioId && visibleRepartitions.length === 0 ? <p className="emptyState">Aucune cle definie pour ce portefeuille.</p> : null}
              {allocationPagination.pageItems.map((repartition) => (
                <div className={editingRepartitionId === repartition.id ? "compactRow editingEntity" : "compactRow"} key={repartition.id}>
                  <div>
                    <strong>{assetById.get(repartition.assetId)?.name ?? "Actif"}</strong>
                    <span>{assetById.get(repartition.assetId)?.symbol ?? "Symbole"} - {formatPercent(repartition.targetWeight)} - {formatRepartitionStatus(repartition.status)}</span>
                  </div>
                  <div className="rowActions">
                    {editingRepartitionId === repartition.id ? <span className="editingBadge">En edition</span> : null}
                    <ActionIconButton action="edit" isActive={editingRepartitionId === repartition.id} label={`Modifier la cle de ${assetById.get(repartition.assetId)?.name ?? "l'actif"}`} onClick={() => editRepartition(repartition)} />
                    <ActionIconButton action="delete" label={`Supprimer la cle de ${assetById.get(repartition.assetId)?.name ?? "l'actif"}`} onClick={() => deleteRepartition.mutate(repartition.id)} />
                  </div>
                </div>
              ))}
            </div>
            <Pagination {...allocationPagination} itemLabel="cles" onPageChange={allocationPagination.setPage} />
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
                <div className="ruleBuilderGrid">
                  <label>
                    Cible
                    <select
                      value={strategyRuleForm.targetScope}
                      onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, targetScope: event.target.value as StrategyRuleForm["targetScope"] })}
                    >
                      {ruleTargetScopeOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                    </select>
                  </label>
                  {strategyRuleForm.targetScope === "PortfolioAssets" ? (
                    <label>
                      Portefeuille
                      <select value={strategyRuleForm.portfolioId} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, portfolioId: event.target.value })} required>
                        <option value="">Selectionner</option>
                        {portfolios.map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}
                      </select>
                    </label>
                  ) : null}
                  {strategyRuleForm.targetScope === "SpecificAsset" ? (
                    <label>
                      Actif
                      <select value={strategyRuleForm.assetId} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, assetId: event.target.value })} required>
                        <option value="">Selectionner</option>
                        {assets.map((asset) => <option value={asset.id} key={asset.id}>{asset.name} - {asset.symbol}</option>)}
                      </select>
                    </label>
                  ) : null}
                </div>
                <div className="ruleBuilderGrid">
                  <label>
                    Condition
                    <select value={strategyRuleForm.metric} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, metric: event.target.value as RuleConditionMetric })}>
                      {ruleMetricOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                    </select>
                  </label>
                  {strategyRuleForm.metric !== "Always" ? (
                    <>
                      <label>
                        Operateur
                        <select value={strategyRuleForm.operator} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, operator: event.target.value as RuleComparisonOperator })}>
                          {ruleOperatorOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                        </select>
                      </label>
                      <label>
                        Seuil en %
                        <DecimalInput value={strategyRuleForm.value} onChange={(value) => setStrategyRuleForm({ ...strategyRuleForm, value })} />
                      </label>
                    </>
                  ) : null}
                  {strategyRuleForm.metric === "PriceChangePercent" ? (
                    <>
                      <label>
                        Duree
                        <DecimalInput min={1} step={1} value={strategyRuleForm.periodAmount} onChange={(value) => setStrategyRuleForm({ ...strategyRuleForm, periodAmount: value })} />
                      </label>
                      <label>
                        Unite
                        <select value={strategyRuleForm.periodUnit} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, periodUnit: event.target.value as RuleTimeUnit })}>
                          {ruleTimeUnitOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                        </select>
                      </label>
                    </>
                  ) : null}
                </div>
                <div className="ruleBuilderGrid">
                  <label>
                    Effet assistant
                    <select value={strategyRuleForm.effectType} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, effectType: event.target.value as RuleEffectType })}>
                      {ruleEffectOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                    </select>
                  </label>
                  <label>
                    Intensite
                    <select value={strategyRuleForm.effectStrength} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, effectStrength: event.target.value as RuleEffectStrength })}>
                      {ruleStrengthOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                    </select>
                  </label>
                  <label>
                    Alerte
                    <select value={strategyRuleForm.severity} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, severity: event.target.value as RuleSeverity })}>
                      {ruleSeverityOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                    </select>
                  </label>
                </div>
                <label>
                  Message affiche
                  <input value={strategyRuleForm.message} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, message: event.target.value })} required />
                </label>
                <div className="rulePreview">
                  <strong>Apercu logique</strong>
                  <span>{describeRuleTarget(strategyRuleForm, portfolioById, assetById)} - {describeRuleCondition(strategyRuleForm)}</span>
                  <span>{labelFor(ruleEffectOptions, strategyRuleForm.effectType)} : {strategyRuleForm.message || "Message a renseigner"}</span>
                </div>
                <label>
                  Ordre d'application
                  <select value={strategyRuleForm.priorityLevel} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, priorityLevel: event.target.value as RulePriorityLevel })}>
                    {rulePriorityOptions.map((option) => <option value={option.value} key={option.value}>{option.label}</option>)}
                  </select>
                  <small>{rulePriorityOptions.find((option) => option.value === strategyRuleForm.priorityLevel)?.detail}</small>
                </label>
                <label className="checkboxLabel"><input type="checkbox" checked={strategyRuleForm.isActive} onChange={(event) => setStrategyRuleForm({ ...strategyRuleForm, isActive: event.target.checked })} /> Regle active</label>
                <div className="formActions">
                  <button type="submit" disabled={isRuleFormInvalid}>{editingStrategyRuleId ? "Mettre a jour" : "Ajouter la regle"}</button>
                  <button className="secondaryButton" type="button" onClick={() => activateEditor(null)}>Annuler</button>
                </div>
              </form>
            </section>
          ) : null}
          {(saveStrategyRule.error || deleteStrategyRule.error) ? <p className="stateError">{errorText(saveStrategyRule.error ?? deleteStrategyRule.error)}</p> : null}
          <QueryState isLoading={strategyRulesQuery.isLoading} error={strategyRulesQuery.error}>
            <div className="compactList">
              {strategyRules.length === 0 ? <p className="emptyState">Aucune regle de decision configuree.</p> : null}
              {rulePagination.pageItems.map((rule) => (
                <div className={editingStrategyRuleId === rule.id ? "compactRow editingEntity" : "compactRow"} key={rule.id}>
                  <div>
                    <strong>{rule.name}</strong>
                    <span>{rule.triggerCondition ?? rule.description}</span>
                    <span>
                      {rule.definition ? `${labelFor(ruleEffectOptions, rule.definition.effect.type)} - ${labelFor(ruleSeverityOptions, rule.definition.effect.severity)}` : rule.recommendedAction}
                      {" - Ordre "}
                      {labelFor(rulePriorityOptions, priorityLevelFromValue(rule.priority)).toLowerCase()}
                    </span>
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
            <Pagination {...rulePagination} itemLabel="regles" onPageChange={rulePagination.setPage} />
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
