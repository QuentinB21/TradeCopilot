import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import type { CreateAssetPricePayload } from "../domain/types";
import { formatCurrency } from "../lib/format";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

export function PricesPage() {
  const queryClient = useQueryClient();
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const pricesQuery = useQuery({ queryKey: ["prices"], queryFn: tradeCopilotApi.getPrices });
  const [form, setForm] = useState<CreateAssetPricePayload>({
    assetId: "",
    date: new Date().toISOString().slice(0, 10),
    open: null,
    high: null,
    low: null,
    close: 0,
    currency: "EUR",
    source: "manual"
  });
  const assetById = useMemo(() => new Map((assetsQuery.data ?? []).map((asset) => [asset.id, asset])), [assetsQuery.data]);

  const createPrice = useMutation({
    mutationFn: () => tradeCopilotApi.createPrice(form),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["prices"] }),
        queryClient.invalidateQueries({ queryKey: ["positions"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
    }
  });

  return (
    <>
      <PageHeader title="Prix" description="Saisie manuelle des derniers cours en attendant le fournisseur automatique." />
      <section className="grid">
        <Panel title="Nouveau prix">
          <form className="form" onSubmit={(event) => { event.preventDefault(); createPrice.mutate(); }}>
            <label>Actif<select value={form.assetId} onChange={(event) => setForm({ ...form, assetId: event.target.value })} required><option value="">Selectionner</option>{(assetsQuery.data ?? []).map((asset) => <option value={asset.id} key={asset.id}>{asset.symbol} - {asset.name}</option>)}</select></label>
            <label>Date<input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} required /></label>
            <label>Cloture<input type="number" step="0.000001" value={form.close} onChange={(event) => setForm({ ...form, close: Number(event.target.value) })} required /></label>
            <label>Devise<input value={form.currency} onChange={(event) => setForm({ ...form, currency: event.target.value })} maxLength={3} required /></label>
            <label>Source<input value={form.source} onChange={(event) => setForm({ ...form, source: event.target.value })} required /></label>
            <button type="submit" disabled={!form.assetId || createPrice.isPending}>Enregistrer le prix</button>
          </form>
        </Panel>

        <Panel title="Historique des prix">
          <QueryState isLoading={pricesQuery.isLoading} error={pricesQuery.error}>
            <div className="tableWrap compactTable">
              <table>
                <thead><tr><th>Date</th><th>Actif</th><th>Cloture</th><th>Source</th></tr></thead>
                <tbody>
                  {(pricesQuery.data ?? []).slice(0, 20).map((price) => (
                    <tr key={price.id}>
                      <td>{price.date}</td>
                      <td>{assetById.get(price.assetId)?.symbol ?? "-"}</td>
                      <td>{formatCurrency(price.close)}</td>
                      <td>{price.source}</td>
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
