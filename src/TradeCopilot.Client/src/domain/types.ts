export type PortfolioType = "Pea" | "SecuritiesAccount" | "Crypto" | "Other";
export type AssetType = "Etf" | "Stock" | "Cash" | "Other";
export type StrategicStatus = "Core" | "Conviction" | "Observation" | "Frozen" | "PlannedExit" | "Sold";
export type TransactionType = "Deposit" | "Withdrawal" | "Buy" | "Sell" | "Dividend" | "Fee" | "Split" | "ManualAdjustment";

export type Portfolio = {
  id: string;
  name: string;
  type: PortfolioType;
  broker: string;
  baseCurrency: string;
  cashBalance: number;
};

export type Asset = {
  id: string;
  name: string;
  symbol: string;
  isin: string | null;
  type: AssetType;
  currency: string;
  sector: string | null;
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
};

export type AssetPrice = {
  id: string;
  assetId: string;
  date: string;
  open: number | null;
  high: number | null;
  low: number | null;
  close: number;
  currency: string;
  source: string;
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
};

export type Dashboard = {
  totalMarketValue: number;
  totalInvested: number;
  totalUnrealizedGain: number;
  totalUnrealizedGainPercent: number;
  portfolios: PortfolioSummary[];
  positions: Position[];
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
    }[];
  }[];
  notes: string[];
};

export type Strategy = {
  globalAllocation: { envelope: string; targetWeight: number }[];
  peaRules: string[];
  tradeRepublicRules: string[];
};

export type CreatePortfolioPayload = Omit<Portfolio, "id">;
export type CreateAssetPayload = Omit<Asset, "id"> & { country: string | null; priceProvider: string | null };
export type CreateTransactionPayload = Omit<Transaction, "id">;
export type CreateAssetPricePayload = Omit<AssetPrice, "id">;
