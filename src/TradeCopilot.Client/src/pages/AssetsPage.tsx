import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { ActionIconButton } from "../components/ActionIconButton";
import { assetTypes, strategicStatuses } from "../domain/options";
import type { Asset, CreateAssetPayload, InstrumentSearchResult } from "../domain/types";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

const emptyAsset: CreateAssetPayload = {
  name: "",
  symbol: "",
  isin: null,
  type: "Stock",
  currency: "EUR",
  sector: "",
  country: "",
  priceProvider: "manual",
  marketSymbol: null,
  strategicStatus: "Conviction"
};

export function AssetsPage() {
  const queryClient = useQueryClient();
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateAssetPayload>(emptyAsset);
  const [search, setSearch] = useState("");
  const [searchResults, setSearchResults] = useState<InstrumentSearchResult[]>([]);

  const searchInstruments = useMutation({
    mutationFn: () => tradeCopilotApi.searchInstruments(search),
    onSuccess: setSearchResults
  });

  const useInstrument = useMutation({
    mutationFn: async (instrument: InstrumentSearchResult) => {
      try {
        const quote = await tradeCopilotApi.getMarketQuote(instrument.symbol);
        return { instrument, currency: quote.currency };
      } catch {
        return { instrument, currency: instrument.currency ?? form.currency };
      }
    },
    onSuccess: ({ instrument, currency }) => {
      setForm({
        ...form,
        name: instrument.name,
        symbol: instrument.symbol,
        type: instrument.suggestedType,
        currency,
        sector: instrument.sector,
        priceProvider: instrument.provider,
        marketSymbol: instrument.symbol
      });
    }
  });

  const saveAsset = useMutation({
    mutationFn: () => editingId ? tradeCopilotApi.updateAsset(editingId, form) : tradeCopilotApi.createAsset(form),
    onSuccess: async () => {
      setEditingId(null);
      setForm(emptyAsset);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["assets"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
    }
  });

  function edit(asset: Asset) {
    setEditingId(asset.id);
    setForm({
      name: asset.name,
      symbol: asset.symbol,
      isin: asset.isin,
      type: asset.type,
      currency: asset.currency,
      sector: asset.sector,
      country: "",
      priceProvider: asset.priceProvider,
      marketSymbol: asset.marketSymbol,
      strategicStatus: asset.strategicStatus
    });
  }

  return (
    <>
      <PageHeader title="Actifs" description="Rechercher un instrument, completer ses informations puis l'utiliser dans la strategie et les transactions." />
      <section className="assetWorkspace">
        <Panel
          className="assetEditorPanel"
          title={editingId ? "Modifier un actif" : "Nouvel actif"}
          subtitle="Nom, ticker ou ISIN pour eviter la saisie manuelle inutile."
        >
          <form className="form searchForm" onSubmit={(event) => { event.preventDefault(); searchInstruments.mutate(); }}>
            <label>Rechercher par nom, ticker ou ISIN<input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Ex: Air Liquide, AI.PA, US0378331005" /></label>
            <button type="submit" disabled={search.trim().length < 2 || searchInstruments.isPending}>Rechercher</button>
          </form>
          {searchResults.length > 0 ? (
            <div className="searchResults">
              {searchResults.map((result) => (
                <button type="button" key={`${result.provider}-${result.symbol}`} onClick={() => useInstrument.mutate(result)}>
                  <strong>{result.name}</strong>
                  <span>{result.symbol}</span>
                  <small>{result.exchangeDisplay ?? result.exchange ?? result.provider}</small>
                </button>
              ))}
            </div>
          ) : null}
          <form className="form" onSubmit={(event) => { event.preventDefault(); saveAsset.mutate(); }}>
            <label>Nom<input value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} required /></label>
            <label>Symbole<input value={form.symbol} onChange={(event) => setForm({ ...form, symbol: event.target.value })} required /></label>
            <label>ISIN<input value={form.isin ?? ""} onChange={(event) => setForm({ ...form, isin: event.target.value || null })} /></label>
            <label>Type<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value as CreateAssetPayload["type"] })}>{assetTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Devise<input value={form.currency} onChange={(event) => setForm({ ...form, currency: event.target.value })} required maxLength={3} /></label>
            <label>Secteur<input value={form.sector ?? ""} onChange={(event) => setForm({ ...form, sector: event.target.value || null })} /></label>
            <label>Statut<select value={form.strategicStatus} onChange={(event) => setForm({ ...form, strategicStatus: event.target.value as CreateAssetPayload["strategicStatus"] })}>{strategicStatuses.map((status) => <option key={status}>{status}</option>)}</select></label>
            <div className="formActions">
              <button type="submit">{editingId ? "Enregistrer" : "Creer"}</button>
              {editingId ? <button className="secondaryButton" type="button" onClick={() => { setEditingId(null); setForm(emptyAsset); }}>Annuler</button> : null}
            </div>
          </form>
        </Panel>

        <Panel className="assetRegistryPanel" title="Actifs suivis" subtitle="Referentiel utilise par les cles et le suivi de positions.">
          <QueryState isLoading={assetsQuery.isLoading} error={assetsQuery.error}>
            <div className="tableWrap compactTable">
              <table>
                <thead><tr><th>Actif</th><th>Cotation liee</th><th>Type</th><th>Statut</th><th></th></tr></thead>
                <tbody>
                  {(assetsQuery.data ?? []).map((asset) => (
                    <tr className={editingId === asset.id ? "editingRow" : undefined} key={asset.id}>
                      <td><strong>{asset.name}</strong><span>{asset.symbol}</span></td>
                      <td>{asset.marketSymbol ? <><strong>{asset.marketSymbol}</strong><span>{asset.priceProvider ?? "Source auto"}</span></> : <span>A lier si le cours manque</span>}</td>
                      <td>{asset.type}</td>
                      <td><span className="status">{asset.strategicStatus}</span></td>
                      <td>
                        <div className="rowActions">
                          {editingId === asset.id ? <span className="editingBadge">En edition</span> : null}
                          <ActionIconButton action="edit" isActive={editingId === asset.id} label={`Modifier ${asset.name}`} onClick={() => edit(asset)} />
                        </div>
                      </td>
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
