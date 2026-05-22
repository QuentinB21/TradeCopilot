import type { AssetType, PortfolioType, RepartitionStatus, StrategicStatus, TransactionType } from "./types";

export const portfolioTypes: PortfolioType[] = ["Pea", "SecuritiesAccount", "Crypto", "Other"];
export const transactionTypes: TransactionType[] = ["Deposit", "Withdrawal", "Buy", "Sell", "Dividend", "Fee", "Split", "ManualAdjustment"];

export const assetTypeOptions: Array<{ label: string; value: AssetType }> = [
  { value: "Etf", label: "ETF" },
  { value: "Stock", label: "Action" },
  { value: "Crypto", label: "Crypto" },
  { value: "Cash", label: "Liquidites" },
  { value: "Other", label: "Autre actif" }
];

export const strategicStatusOptions: Array<{ label: string; value: StrategicStatus }> = [
  { value: "Core", label: "Noyau" },
  { value: "Conviction", label: "Conviction" },
  { value: "Observation", label: "Observation" },
  { value: "PlannedExit", label: "A ceder" }
];

export const repartitionStatusOptions: Array<{ label: string; value: RepartitionStatus }> = [
  { value: "Active", label: "Active" },
  { value: "Frozen", label: "Gelee" },
  { value: "ExitOnly", label: "Vente uniquement" }
];

const assetTypeLabels = Object.fromEntries(assetTypeOptions.map((option) => [option.value, option.label])) as Record<AssetType, string>;

const strategicStatusLabels: Record<StrategicStatus, string> = {
  Core: "Noyau",
  Conviction: "Conviction",
  Observation: "Observation",
  PlannedExit: "A ceder",
  Frozen: "A reclasser",
  Sold: "Vendu"
};

const repartitionStatusLabels = Object.fromEntries(repartitionStatusOptions.map((option) => [option.value, option.label])) as Record<RepartitionStatus, string>;

export function formatAssetType(type: AssetType) {
  return assetTypeLabels[type];
}

export function formatStrategicStatus(status: StrategicStatus) {
  return strategicStatusLabels[status];
}

export function formatRepartitionStatus(status: RepartitionStatus) {
  return repartitionStatusLabels[status];
}
