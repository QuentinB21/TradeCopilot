import type { AssetType, PortfolioType, StrategicStatus, TransactionType } from "./types";

export const portfolioTypes: PortfolioType[] = ["Pea", "SecuritiesAccount", "Crypto", "Other"];
export const assetTypes: AssetType[] = ["Etf", "Stock", "Cash", "Other"];
export const strategicStatuses: StrategicStatus[] = ["Core", "Conviction", "Observation", "Frozen", "PlannedExit", "Sold"];
export const transactionTypes: TransactionType[] = ["Deposit", "Withdrawal", "Buy", "Sell", "Dividend", "Fee", "Split", "ManualAdjustment"];
