import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { DecimalInput } from "../components/DecimalInput";
import type { AssetPrice, CreateAssetPricePayload } from "../domain/types";
import { formatCurrency } from "../lib/format";
import { parseDecimalInput, parseNullableDecimalInput, toNumberInput } from "../lib/numberInput";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

type PriceForm = Omit<CreateAssetPricePayload, "open" | "high" | "low" | "close"> & {
  open: string;
  high: string;
  low: string;
  close: string;
};

const emptyPrice: PriceForm = {
  assetId: "",
  date: new Date().toISOString().slice(0, 10),
  open: "",
  high: "",
  low: "",
  close: "",
  currency: "EUR",
  source: "manual"
};

function errorText(error: unknown) {
  return error instanceof Error ? error.message : "Operation impossible pour le moment.";
}

export function PricesPage() {
  const queryClient = useQueryClient();
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const pricesQuery = useQuery({ queryKey: ["prices"], queryFn: tradeCopilotApi.getPrices });
  const [form, setForm] = useState<PriceForm>(emptyPrice);
  const [editingId, setEditingId] = useState<string | null>(null);
  const assetById = useMemo(() => new Map((assetsQuery.data ?? []).map((asset) => [asset.id, asset])), [assetsQuery.data]);

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["prices"] }),
      queryClient.invalidateQueries({ queryKey: ["positions"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] })
    ]);
  };

  const resetForm = () => {
    setForm(emptyPrice);
    setEditingId(null);
  };

  const editPrice = (price: AssetPrice) => {
    setEditingId(price.id);
    setForm({
      assetId: price.assetId,
      date: price.date,
      open: toNumberInput(price.open),
      high: toNumberInput(price.high),
      low: toNumberInput(price.low),
      close: toNumberInput(price.close),
      currency: price.currency,
      source: price.source
    });
  };

  const savePrice = useMutation({
    mutationFn: () => {
      const payload: CreateAssetPricePayload = {
        ...form,
        open: parseNullableDecimalInput(form.open),
        high: parseNullableDecimalInput(form.high),
        low: parseNullableDecimalInput(form.low),
        close: parseDecimalInput(form.close)
      };
      return editingId ? tradeCopilotApi.updatePrice(editingId, payload) : tradeCopilotApi.createPrice(payload);
    },
    onSuccess: async () => {
      resetForm();
      await invalidate();
    }
  });

  const deletePrice = useMutation({
    mutationFn: tradeCopilotApi.deletePrice,
    onSuccess: invalidate
  });

  const fetchMarketPrice = useMutation({
    mutationFn: () => {
      const asset = assetById.get(form.assetId);
      if (!asset) {
        throw new Error("Selectionnez d'abord un actif.");
      }

      return tradeCopilotApi.getMarketQuote(asset.symbol);
    },
    onSuccess: (quote) => {
      setForm({
        ...form,
        date: quote.date,
        open: toNumberInput(quote.open),
        high: toNumberInput(quote.high),
        low: toNumberInput(quote.low),
        close: toNumberInput(quote.close),
        currency: quote.currency,
        source: quote.provider
      });
    }
  });

  const prices = pricesQuery.data ?? [];

  return (
    <>
      <PageHeader title="Prix" description="Saisie et correction des derniers cours utilises pour valoriser les positions." />
      <section className="grid">
        <Panel title={editingId ? "Modifier le prix" : "Nouveau prix"}>
          <form className="form" onSubmit={(event) => { event.preventDefault(); savePrice.mutate(); }}>
            <label>Actif<select value={form.assetId} onChange={(event) => setForm({ ...form, assetId: event.target.value })} required><option value="">Selectionner</option>{(assetsQuery.data ?? []).map((asset) => <option value={asset.id} key={asset.id}>{asset.symbol} - {asset.name}</option>)}</select></label>
            <button className="secondaryButton" type="button" disabled={!form.assetId || fetchMarketPrice.isPending} onClick={() => fetchMarketPrice.mutate()}>Recuperer le dernier cours</button>
            <label>Date<input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} required /></label>
            <label>Cloture<DecimalInput step="0.000001" value={form.close} onChange={(value) => setForm({ ...form, close: value })} required /></label>
            <label>Ouverture<DecimalInput step="0.000001" value={form.open} onChange={(value) => setForm({ ...form, open: value })} /></label>
            <label>Plus haut<DecimalInput step="0.000001" value={form.high} onChange={(value) => setForm({ ...form, high: value })} /></label>
            <label>Plus bas<DecimalInput step="0.000001" value={form.low} onChange={(value) => setForm({ ...form, low: value })} /></label>
            <label>Devise<input value={form.currency} onChange={(event) => setForm({ ...form, currency: event.target.value })} maxLength={3} required /></label>
            <label>Source<input value={form.source} onChange={(event) => setForm({ ...form, source: event.target.value })} required /></label>
            <div className="formActions">
              <button type="submit" disabled={!form.assetId || savePrice.isPending}>{editingId ? "Mettre a jour" : "Enregistrer le prix"}</button>
              {editingId && <button className="secondaryButton" type="button" onClick={resetForm}>Annuler</button>}
            </div>
          </form>
          {(savePrice.error || deletePrice.error || fetchMarketPrice.error) && <p className="stateError">{errorText(savePrice.error ?? deletePrice.error ?? fetchMarketPrice.error)}</p>}
        </Panel>

        <Panel title="Historique des prix">
          <QueryState isLoading={pricesQuery.isLoading} error={pricesQuery.error}>
            {prices.length === 0 ? (
              <p className="emptyState">Aucun prix saisi.</p>
            ) : (
              <div className="tableWrap compactTable">
                <table>
                  <thead><tr><th>Date</th><th>Actif</th><th>Cloture</th><th>Source</th><th></th></tr></thead>
                  <tbody>
                    {prices.slice(0, 50).map((price) => (
                      <tr key={price.id}>
                        <td>{price.date}</td>
                        <td>{assetById.get(price.assetId)?.symbol ?? "-"}</td>
                        <td>{formatCurrency(price.close)}</td>
                        <td>{price.source}</td>
                        <td>
                          <div className="rowActions">
                            <button className="linkButton" type="button" onClick={() => editPrice(price)}>Modifier</button>
                            <button className="linkButton dangerText" type="button" onClick={() => deletePrice.mutate(price.id)}>Supprimer</button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </QueryState>
        </Panel>
      </section>
    </>
  );
}
