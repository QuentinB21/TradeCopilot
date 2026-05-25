import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Search, X } from "lucide-react";
import { type ReactNode, useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { ActionIconButton } from "../components/ActionIconButton";
import { assetTypeOptions, formatAssetType, formatStrategicStatus, strategicStatusOptions } from "../domain/options";
import type { Asset, CreateAssetPayload, InstrumentSearchResult } from "../domain/types";
import { MarketBindingPanel } from "../components/MarketBindingPanel";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { Pagination } from "../components/Pagination";
import { QueryState } from "../components/QueryState";
import { useBodyScrollLock } from "../hooks/useBodyScrollLock";
import { usePagination } from "../hooks/usePagination";

const emptyAsset: CreateAssetPayload = {
  name: "",
  symbol: "",
  isin: null,
  type: "Stock",
  currency: "EUR",
  country: "",
  priceProvider: "manual",
  marketSymbol: null,
  strategicStatus: "Conviction"
};

type AssetDrawer = "editor" | "binding" | null;

export function AssetsPage() {
  const queryClient = useQueryClient();
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<CreateAssetPayload>(emptyAsset);
  const [instrumentSearch, setInstrumentSearch] = useState("");
  const [searchResults, setSearchResults] = useState<InstrumentSearchResult[]>([]);
  const [bindingAssetId, setBindingAssetId] = useState<string | null>(null);
  const [drawer, setDrawer] = useState<AssetDrawer>(null);
  const [registrySearch, setRegistrySearch] = useState("");
  const assets = assetsQuery.data ?? [];
  const filteredAssets = useMemo(() => {
    const query = registrySearch.trim().toLocaleLowerCase();
    if (!query) {
      return assets;
    }

    return assets.filter((asset) =>
      [asset.name, asset.symbol, asset.isin, asset.marketSymbol]
        .filter(Boolean)
        .some((value) => value!.toLocaleLowerCase().includes(query)));
  }, [assets, registrySearch]);
  const assetPagination = usePagination(filteredAssets);

  const searchInstruments = useMutation({
    mutationFn: () => tradeCopilotApi.searchInstruments(instrumentSearch),
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
      setSearchResults([]);
      setDrawer(null);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["assets"] }),
        queryClient.invalidateQueries({ queryKey: ["dashboard"] })
      ]);
    }
  });

  function openCreate() {
    setEditingId(null);
    setBindingAssetId(null);
    setForm(emptyAsset);
    setInstrumentSearch("");
    setSearchResults([]);
    setDrawer("editor");
  }

  function closeDrawer() {
    setDrawer(null);
    setEditingId(null);
    setBindingAssetId(null);
    setForm(emptyAsset);
    setInstrumentSearch("");
    setSearchResults([]);
  }

  function edit(asset: Asset) {
    if (drawer === "editor" && editingId === asset.id) {
      closeDrawer();
      return;
    }

    setEditingId(asset.id);
    setBindingAssetId(null);
    setInstrumentSearch(asset.name);
    setSearchResults([]);
    setDrawer("editor");
    setForm({
      name: asset.name,
      symbol: asset.symbol,
      isin: asset.isin,
      type: asset.type,
      currency: asset.currency,
      country: "",
      priceProvider: asset.priceProvider,
      marketSymbol: asset.marketSymbol,
      strategicStatus: asset.strategicStatus
    });
  }

  function toggleBinding(asset: Asset) {
    if (drawer === "binding" && bindingAssetId === asset.id) {
      closeDrawer();
      return;
    }

    setEditingId(null);
    setForm(emptyAsset);
    setBindingAssetId(asset.id);
    setDrawer("binding");
  }

  const bindingAsset = assets.find((asset) => asset.id === bindingAssetId);

  return (
    <>
      <PageHeader title="Actifs" description="Rechercher un instrument, completer ses informations puis l'utiliser dans la strategie et les transactions." />
      <Panel
        className="assetRegistryPanel assetRegistryFull"
        title="Actifs suivis"
        subtitle="Referentiel utilise par les cles, les transactions et le suivi de valorisation."
        action={<button className="ghostButton" onClick={openCreate} type="button">Ajouter un actif</button>}
      >
        <div className="registryToolbar">
          <label>
            Rechercher dans les actifs
            <span>
              <Search size={16} />
              <input value={registrySearch} onChange={(event) => setRegistrySearch(event.target.value)} placeholder="Nom, symbole, ISIN ou cotation" />
            </span>
          </label>
        </div>
          <QueryState isLoading={assetsQuery.isLoading} error={assetsQuery.error}>
            {filteredAssets.length === 0 ? <p className="emptyState">Aucun actif ne correspond a la recherche.</p> : null}
            <div className="tableWrap compactTable">
              <table>
                <thead><tr><th>Actif</th><th>Cotation liee</th><th>Nature</th><th>Role strategique</th><th></th></tr></thead>
                <tbody>
                  {assetPagination.pageItems.map((asset) => (
                    <tr className={(editingId === asset.id || bindingAssetId === asset.id) ? "editingRow" : undefined} key={asset.id}>
                      <td><strong>{asset.name}</strong><span>{asset.symbol}</span></td>
                      <td>
                        <strong>{asset.marketSymbol ?? asset.symbol}</strong>
                        <span>{marketBindingDetail(asset)}</span>
                      </td>
                      <td>{formatAssetType(asset.type)}</td>
                      <td><span className="status">{formatStrategicStatus(asset.strategicStatus)}</span></td>
                      <td>
                        <div className="rowActions">
                          {editingId === asset.id ? <span className="editingBadge">En edition</span> : null}
                          <ActionIconButton action="link" isActive={drawer === "binding" && bindingAssetId === asset.id} label={`Changer la cotation de ${asset.name}`} onClick={() => toggleBinding(asset)} />
                          <ActionIconButton action="edit" isActive={editingId === asset.id} label={`Modifier ${asset.name}`} onClick={() => edit(asset)} />
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pagination {...assetPagination} itemLabel="actifs" onPageChange={assetPagination.setPage} />
          </QueryState>
      </Panel>
      {drawer ? (
        <AssetDrawerShell
          title={drawer === "editor" ? (editingId ? "Modifier un actif" : "Nouvel actif") : `Cotation de ${bindingAsset?.name ?? "l'actif"}`}
          subtitle={drawer === "editor"
            ? "Rechercher un instrument puis completer les informations utiles."
            : "Corriger ou remplacer la cotation utilisee par la valorisation."}
          onClose={closeDrawer}
        >
          {drawer === "editor" ? (
            <>
              <form className="form searchForm" onSubmit={(event) => { event.preventDefault(); searchInstruments.mutate(); }}>
                <label>Rechercher par nom, ticker ou ISIN<input value={instrumentSearch} onChange={(event) => setInstrumentSearch(event.target.value)} placeholder="Ex: Air Liquide, AI.PA, US0378331005" /></label>
                <button type="submit" disabled={instrumentSearch.trim().length < 2 || searchInstruments.isPending}>Rechercher</button>
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
                <label>Nature<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value as CreateAssetPayload["type"] })}>{assetTypeOptions.map((type) => <option value={type.value} key={type.value}>{type.label}</option>)}</select></label>
                <label>Devise<input value={form.currency} onChange={(event) => setForm({ ...form, currency: event.target.value })} required maxLength={3} /></label>
                <label>Role strategique<select value={form.strategicStatus} onChange={(event) => setForm({ ...form, strategicStatus: event.target.value as CreateAssetPayload["strategicStatus"] })}>{strategicStatusOptions.map((status) => <option value={status.value} key={status.value}>{status.label}</option>)}</select></label>
                <div className="formActions">
                  <button type="submit">{editingId ? "Enregistrer" : "Creer"}</button>
                  <button className="secondaryButton" type="button" onClick={closeDrawer}>Annuler</button>
                </div>
              </form>
            </>
          ) : bindingAsset ? (
            <MarketBindingPanel assets={[{ assetId: bindingAsset.id, assetName: bindingAsset.name, symbol: bindingAsset.symbol }]} />
          ) : null}
        </AssetDrawerShell>
      ) : null}
    </>
  );
}

function AssetDrawerShell({
  children,
  onClose,
  subtitle,
  title
}: {
  children: ReactNode;
  onClose: () => void;
  subtitle: string;
  title: string;
}) {
  useBodyScrollLock(true);

  return (
    <div className="assetDrawerLayer" role="presentation">
      <button aria-label="Fermer le panneau" className="assetDrawerBackdrop" onClick={onClose} type="button" />
      <aside aria-label={title} className="assetDrawer">
        <header>
          <div>
            <h2>{title}</h2>
            <span>{subtitle}</span>
          </div>
          <button aria-label="Fermer" className="actionIconButton" onClick={onClose} type="button"><X size={16} /></button>
        </header>
        <div className="assetDrawerContent">{children}</div>
      </aside>
    </div>
  );
}

function marketBindingDetail(asset: Asset) {
  if (asset.marketSymbol) {
    return `${asset.priceProvider ?? "Source marche"}`;
  }

  if (asset.type === "Cash") {
    return "Liquidites sans cours de marche.";
  }

  return asset.isin
    ? "Recherche automatique via le symbole ou l'ISIN."
    : "Recherche automatique via le symbole.";
}
