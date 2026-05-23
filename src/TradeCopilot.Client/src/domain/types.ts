export type PortfolioType = "Pea" | "SecuritiesAccount" | "Crypto" | "Other";
export type AssetType = "Etf" | "Stock" | "Cash" | "Other" | "Crypto";
export type StrategicStatus = "Core" | "Conviction" | "Observation" | "Frozen" | "PlannedExit" | "Sold";
export type TransactionType = "Deposit" | "Withdrawal" | "Buy" | "Sell" | "Dividend" | "Fee" | "Split" | "ManualAdjustment";

export type Portfolio = {
  id: string;
  name: string;
  type: PortfolioType;
  broker: string;
  baseCurrency: string;
  cashBalance: number;
  targetWeight: number;
};

export type Asset = {
  id: string;
  name: string;
  symbol: string;
  isin: string | null;
  type: AssetType;
  currency: string;
  priceProvider: string | null;
  marketSymbol: string | null;
  strategicStatus: StrategicStatus;
};

export type Transaction = {
  id: string;
  portfolioId: string;
  assetId: string | null;
  type: TransactionType;
  date: string;
  quantity: number;
  unitPrice: number;
  fees: number;
  currency: string;
  comment: string | null;
  importSource: string | null;
  externalId: string | null;
};

export type TransactionImportProvider = "TradeRepublic" | "Boursobank";

export type TransactionImportResult = {
  rowsRead: number;
  importedTransactions: number;
  createdAssets: number;
  duplicateRows: number;
  skippedRows: number;
  warnings: {
    rowNumber: number | null;
    code: string;
    message: string;
    recommendation: string;
  }[];
};

export type InstrumentSearchResult = {
  symbol: string;
  name: string;
  exchange: string | null;
  exchangeDisplay: string | null;
  quoteType: string | null;
  currency: string | null;
  suggestedType: AssetType;
  provider: string;
};

export type MarketQuote = {
  symbol: string;
  date: string;
  open: number | null;
  high: number | null;
  low: number | null;
  close: number;
  currency: string;
  provider: string;
  retrievedAt: string;
};

export type Position = {
  portfolioId: string;
  portfolioName: string;
  assetId: string;
  assetName: string;
  symbol: string;
  strategicStatus: StrategicStatus;
  quantity: number;
  averageBuyPrice: number;
  investedAmount: number;
  marketPrice: number;
  hasMarketPrice: boolean;
  marketValue: number;
  unrealizedGain: number;
  unrealizedGainPercent: number;
  realizedGain: number;
  weight: number;
  targetWeight: number | null;
  allocationDrift: number | null;
};

export type PortfolioSummary = {
  portfolioId: string;
  name: string;
  marketValue: number;
  investedAmount: number;
  unrealizedGain: number;
  unrealizedGainPercent: number;
  targetWeight: number;
  actualWeight: number;
  allocationDrift: number;
};

export type PortfolioHistoryPoint = {
  portfolioId: string;
  portfolioName: string;
  marketValue: number;
  investedAmount: number;
  unrealizedGain: number;
};

export type DashboardHistoryPoint = {
  date: string;
  totalMarketValue: number;
  totalInvested: number;
  totalUnrealizedGain: number;
  portfolios: PortfolioHistoryPoint[];
};

export type Dashboard = {
  totalMarketValue: number;
  totalInvested: number;
  totalUnrealizedGain: number;
  totalUnrealizedGainPercent: number;
  portfolios: PortfolioSummary[];
  positions: Position[];
  history: DashboardHistoryPoint[];
  ruleAlerts: RuleAlert[];
};

export type MonthlyInvestmentPlan = {
  amount: number;
  envelopes: {
    portfolioId: string;
    portfolioName: string;
    amount: number;
    lines: {
      assetId: string;
      symbol: string;
      assetName: string;
      amount: number;
      targetWeight: number;
      currentWeight: number | null;
      rationale: string;
      ruleImpacts: RuleImpact[];
    }[];
  }[];
  notes: string[];
};

export type RepartitionStatus = "Active" | "Frozen" | "ExitOnly";

export type Repartition = {
  id: string;
  portfolioId: string;
  assetId: string;
  targetWeight: number;
  minWeight: number | null;
  maxWeight: number | null;
  status: RepartitionStatus;
};

export type StrategyRule = {
  id: string;
  portfolioId: string | null;
  assetId: string | null;
  name: string;
  description: string;
  triggerCondition: string | null;
  recommendedAction: string;
  definition: RuleDefinition | null;
  priority: number;
  isActive: boolean;
};

export type Strategy = {
  globalAllocation: { envelope: string; targetWeight: number }[];
  rules: StrategyRule[];
};

export type RuleTargetType = "Asset" | "Portfolio" | "Position";
export type RuleTargetMode = "All" | "Specific" | "PortfolioAssets";
export type RuleConditionMetric = "Always" | "PriceChangePercent" | "AllocationDrift" | "UnrealizedGainPercent";
export type RuleComparisonOperator = "LessThanOrEqual" | "GreaterThanOrEqual" | "Equal";
export type RuleValueUnit = "None" | "Percent" | "PercentPoint";
export type RuleTimeUnit = "Day" | "Week" | "Month" | "Year";
export type RuleEffectType = "AlertOnly" | "BlockBuy" | "ReduceBuy" | "PrioritizeBuy" | "RequireReview";
export type RuleEffectStrength = "Soft" | "Hard";
export type RuleSeverity = "Info" | "Warning" | "Critical";

export type RuleDefinition = {
  version: number;
  target: {
    type: RuleTargetType;
    mode: RuleTargetMode;
    portfolioId: string | null;
    assetId: string | null;
  };
  condition: {
    metric: RuleConditionMetric;
    operator: RuleComparisonOperator;
    value: number | null;
    unit: RuleValueUnit;
    period: {
      amount: number;
      unit: RuleTimeUnit;
    } | null;
  };
  effect: {
    type: RuleEffectType;
    strength: RuleEffectStrength;
    severity: RuleSeverity;
    message: string;
  };
};

export type RuleAlert = {
  ruleId: string;
  ruleName: string;
  severity: RuleSeverity;
  portfolioId: string | null;
  portfolioName: string | null;
  assetId: string | null;
  assetName: string | null;
  message: string;
  explanation: string;
  measuredValue: number | null;
  thresholdValue: number | null;
};

export type RuleImpact = {
  ruleId: string;
  ruleName: string;
  effectType: RuleEffectType;
  strength: RuleEffectStrength;
  severity: RuleSeverity;
  message: string;
  explanation: string;
};

export type CreatePortfolioPayload = Omit<Portfolio, "id">;
export type CreateAssetPayload = Omit<Asset, "id"> & { country: string | null };
export type CreateTransactionPayload = Omit<Transaction, "id" | "importSource" | "externalId">;
export type UpdateTransactionPayload = Omit<Transaction, "id" | "importSource" | "externalId">;
export type CreateRepartitionPayload = Omit<Repartition, "id">;
export type UpdateRepartitionPayload = Omit<Repartition, "id" | "portfolioId" | "assetId">;
export type CreateStrategyRulePayload = Omit<StrategyRule, "id">;
