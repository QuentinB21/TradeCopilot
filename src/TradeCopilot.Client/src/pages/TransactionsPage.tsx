import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { transactionTypes } from "../domain/options";
import type { CreateTransactionPayload } from "../domain/types";
import { formatCurrency } from "../lib/format";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

export function TransactionsPage() {
  const queryClient = useQueryClient();
  const portfoliosQuery = useQuery({ queryKey: ["portfolios"], queryFn: tradeCopilotApi.getPortfolios });
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const transactionsQuery = useQuery({ queryKey: ["transactions"], queryFn: tradeCopilotApi.getTransactions });
  const [form, setForm] = useState<CreateTransactionPayload>({
    portfolioId: "",
    assetId: "",
    type: "Buy",
    date: new Date().toISOString().slice(0, 10),
    quantity: 0,
    unitPrice: 0,
    fees: 0,
    currency: "EUR",
    comment: ""
  });

  const assetById = useMemo(() => new Map((assetsQuery.data ?? []).map((asset) => [asset.id, asset])), [assetsQuery.data]);
  const portfolioById = useMemo(() => new Map((portfoliosQuery.data ?? []).map((portfolio) => [portfolio.id, portfolio])), [portfoliosQuery.data]);

  const createTransaction = useMutation({
    mutationFn: () => tradeCopilotApi.createTransaction({ ...form, assetId: form.assetId || null }),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["transactions"] }),
        queryClient.invalidateQueries({ queryKey: ["positions"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
    }
  });

  return (
    <>
      <PageHeader title="Transactions" description="Saisie append-only des achats, ventes, versements, dividendes et frais." />
      <section className="grid">
        <Panel title="Nouvelle transaction">
          <form className="form" onSubmit={(event) => { event.preventDefault(); createTransaction.mutate(); }}>
            <label>Portefeuille<select value={form.portfolioId} onChange={(event) => setForm({ ...form, portfolioId: event.target.value })} required><option value="">Selectionner</option>{(portfoliosQuery.data ?? []).map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
            <label>Actif<select value={form.assetId ?? ""} onChange={(event) => setForm({ ...form, assetId: event.target.value })}><option value="">Aucun</option>{(assetsQuery.data ?? []).map((asset) => <option value={asset.id} key={asset.id}>{asset.symbol} - {asset.name}</option>)}</select></label>
            <label>Type<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value as CreateTransactionPayload["type"] })}>{transactionTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Date<input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} required /></label>
            <label>Quantite<input type="number" step="0.000001" value={form.quantity} onChange={(event) => setForm({ ...form, quantity: Number(event.target.value) })} /></label>
            <label>Prix unitaire<input type="number" step="0.000001" value={form.unitPrice} onChange={(event) => setForm({ ...form, unitPrice: Number(event.target.value) })} /></label>
            <label>Frais<input type="number" step="0.01" value={form.fees} onChange={(event) => setForm({ ...form, fees: Number(event.target.value) })} /></label>
            <label>Commentaire<input value={form.comment ?? ""} onChange={(event) => setForm({ ...form, comment: event.target.value })} /></label>
            <button type="submit" disabled={!form.portfolioId || createTransaction.isPending}>Ajouter</button>
          </form>
        </Panel>

        <Panel title="Historique">
          <QueryState isLoading={transactionsQuery.isLoading} error={transactionsQuery.error}>
            <div className="tableWrap compactTable">
              <table>
                <thead><tr><th>Date</th><th>Type</th><th>Portefeuille</th><th>Actif</th><th>Montant</th></tr></thead>
                <tbody>
                  {(transactionsQuery.data ?? []).map((transaction) => (
                    <tr key={transaction.id}>
                      <td>{transaction.date}</td>
                      <td>{transaction.type}</td>
                      <td>{portfolioById.get(transaction.portfolioId)?.name ?? "-"}</td>
                      <td>{transaction.assetId ? assetById.get(transaction.assetId)?.symbol ?? "-" : "-"}</td>
                      <td>{formatCurrency(transaction.quantity * transaction.unitPrice + transaction.fees)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </QueryState>
        </Panel>
      </section>
    </>
  );
}
