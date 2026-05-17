import { getJson, postJson, putJson } from "./client";
import type {
  Asset,
  AssetPrice,
  CreateAssetPayload,
  CreateAssetPricePayload,
  CreatePortfolioPayload,
  CreateTransactionPayload,
  Dashboard,
  MonthlyInvestmentPlan,
  Portfolio,
  Position,
  Strategy,
  Transaction
} from "../domain/types";

export const tradeCopilotApi = {
  getDashboard: () => getJson<Dashboard>("/api/dashboard"),
  getPositions: () => getJson<Position[]>("/api/positions"),
  getPortfolios: () => getJson<Portfolio[]>("/api/portfolios"),
  createPortfolio: (payload: CreatePortfolioPayload) => postJson<Portfolio>("/api/portfolios", payload),
  updatePortfolio: (id: string, payload: CreatePortfolioPayload) => putJson<Portfolio>(`/api/portfolios/${id}`, payload),
  getAssets: () => getJson<Asset[]>("/api/assets"),
  createAsset: (payload: CreateAssetPayload) => postJson<Asset>("/api/assets", payload),
  updateAsset: (id: string, payload: CreateAssetPayload) => putJson<Asset>(`/api/assets/${id}`, payload),
  getTransactions: () => getJson<Transaction[]>("/api/transactions"),
  createTransaction: (payload: CreateTransactionPayload) => postJson<Transaction>("/api/transactions", payload),
  getPrices: () => getJson<AssetPrice[]>("/api/prices"),
  createPrice: (payload: CreateAssetPricePayload) => postJson<AssetPrice>("/api/prices", payload),
  createMonthlyPlan: (amount: number) => postJson<MonthlyInvestmentPlan>("/api/monthly-plan", { amount }),
  getStrategy: () => getJson<Strategy>("/api/strategy")
};
