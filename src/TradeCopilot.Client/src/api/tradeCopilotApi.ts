import { deleteJson, getJson, postForm, postJson, putJson } from "./client";
import type {
  Asset,
  AssetPrice,
  CreateAssetPayload,
  CreateAssetPricePayload,
  CreateAllocationRulePayload,
  CreatePortfolioPayload,
  CreateStrategyRulePayload,
  CreateTransactionPayload,
  Dashboard,
  InstrumentSearchResult,
  MarketQuote,
  MonthlyInvestmentPlan,
  Portfolio,
  Position,
  Strategy,
  AllocationRule,
  StrategyRule,
  Transaction,
  TransactionImportProvider,
  TransactionImportResult,
  UpdateAllocationRulePayload,
  UpdateAssetPricePayload,
  UpdateTransactionPayload
} from "../domain/types";

export const tradeCopilotApi = {
  getDashboard: () => getJson<Dashboard>("/api/dashboard"),
  searchInstruments: (query: string) => getJson<InstrumentSearchResult[]>(`/api/market-data/instruments?query=${encodeURIComponent(query)}`),
  getMarketQuote: (symbol: string) => getJson<MarketQuote>(`/api/market-data/quotes/${encodeURIComponent(symbol)}`),
  getPositions: () => getJson<Position[]>("/api/positions"),
  getPortfolios: () => getJson<Portfolio[]>("/api/portfolios"),
  createPortfolio: (payload: CreatePortfolioPayload) => postJson<Portfolio>("/api/portfolios", payload),
  updatePortfolio: (id: string, payload: CreatePortfolioPayload) => putJson<Portfolio>(`/api/portfolios/${id}`, payload),
  deletePortfolio: (id: string) => deleteJson(`/api/portfolios/${id}`),
  getAssets: () => getJson<Asset[]>("/api/assets"),
  createAsset: (payload: CreateAssetPayload) => postJson<Asset>("/api/assets", payload),
  updateAsset: (id: string, payload: CreateAssetPayload) => putJson<Asset>(`/api/assets/${id}`, payload),
  deleteAsset: (id: string) => deleteJson(`/api/assets/${id}`),
  getTransactions: () => getJson<Transaction[]>("/api/transactions"),
  createTransaction: (payload: CreateTransactionPayload) => postJson<Transaction>("/api/transactions", payload),
  updateTransaction: (id: string, payload: UpdateTransactionPayload) => putJson<Transaction>(`/api/transactions/${id}`, payload),
  deleteTransaction: (id: string) => deleteJson(`/api/transactions/${id}`),
  importTransactions: (provider: TransactionImportProvider, portfolioId: string, file: File) => {
    const body = new FormData();
    body.append("provider", provider);
    body.append("portfolioId", portfolioId);
    body.append("file", file);
    return postForm<TransactionImportResult>("/api/transaction-imports", body);
  },
  getPrices: () => getJson<AssetPrice[]>("/api/prices"),
  createPrice: (payload: CreateAssetPricePayload) => postJson<AssetPrice>("/api/prices", payload),
  updatePrice: (id: string, payload: UpdateAssetPricePayload) => putJson<AssetPrice>(`/api/prices/${id}`, payload),
  deletePrice: (id: string) => deleteJson(`/api/prices/${id}`),
  getAllocationRules: () => getJson<AllocationRule[]>("/api/allocation-rules"),
  createAllocationRule: (payload: CreateAllocationRulePayload) => postJson<AllocationRule>("/api/allocation-rules", payload),
  updateAllocationRule: (id: string, payload: UpdateAllocationRulePayload) => putJson<AllocationRule>(`/api/allocation-rules/${id}`, payload),
  deleteAllocationRule: (id: string) => deleteJson(`/api/allocation-rules/${id}`),
  createMonthlyPlan: (amount: number) => postJson<MonthlyInvestmentPlan>("/api/monthly-plan", { amount }),
  getStrategy: () => getJson<Strategy>("/api/strategy"),
  getStrategyRules: () => getJson<StrategyRule[]>("/api/strategy-rules"),
  createStrategyRule: (payload: CreateStrategyRulePayload) => postJson<StrategyRule>("/api/strategy-rules", payload),
  updateStrategyRule: (id: string, payload: CreateStrategyRulePayload) => putJson<StrategyRule>(`/api/strategy-rules/${id}`, payload),
  deleteStrategyRule: (id: string) => deleteJson(`/api/strategy-rules/${id}`)
};
