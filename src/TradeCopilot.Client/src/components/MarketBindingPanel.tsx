import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Link2, Search } from "lucide-react";
import { useEffect, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import type { InstrumentSearchResult } from "../domain/types";

export type MarketBindingAsset = {
  assetId: string;
  assetName: string;
  symbol: string;
};

type MarketBindingPanelProps = {
  assets: MarketBindingAsset[];
};

export function MarketBindingPanel({ assets }: MarketBindingPanelProps) {
  const queryClient = useQueryClient();
  const [selectedAssetId, setSelectedAssetId] = useState(assets[0]?.assetId ?? "");
  const selectedAsset = assets.find((asset) => asset.assetId === selectedAssetId) ?? assets[0];
  const [query, setQuery] = useState(selectedAsset?.assetName ?? "");
  const [results, setResults] = useState<InstrumentSearchResult[]>([]);

  useEffect(() => {
    if (!selectedAsset) {
      return;
    }

    if (selectedAsset.assetId !== selectedAssetId) {
      setSelectedAssetId(selectedAsset.assetId);
      setQuery(selectedAsset.assetName);
      setResults([]);
    }
  }, [selectedAsset, selectedAssetId]);

  const searchInstruments = useMutation({
    mutationFn: () => tradeCopilotApi.searchInstruments(query),
    onSuccess: setResults
  });

  const bindInstrument = useMutation({
    mutationFn: (instrument: InstrumentSearchResult) => {
      if (!selectedAsset) {
        throw new Error("Selectionnez un actif a lier.");
      }

      return tradeCopilotApi.bindMarketInstrument(selectedAsset.assetId, instrument.symbol, instrument.provider);
    },
    onSuccess: async () => {
      setResults([]);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
        queryClient.invalidateQueries({ queryKey: ["assets"] }),
        queryClient.invalidateQueries({ queryKey: ["positions"] })
      ]);
    }
  });

  if (!selectedAsset) {
    return null;
  }

  function selectAsset(asset: MarketBindingAsset) {
    setSelectedAssetId(asset.assetId);
    setQuery(asset.assetName);
    setResults([]);
  }

  return (
    <div className="marketBinding">
      <div className="bindingAssetList" aria-label="Actifs a lier a une cotation">
        {assets.map((asset) => (
          <button
            className={asset.assetId === selectedAsset.assetId ? "bindingAsset active" : "bindingAsset"}
            key={asset.assetId}
            onClick={() => selectAsset(asset)}
            type="button"
          >
            <strong>{asset.assetName}</strong>
            <span>{asset.symbol}</span>
          </button>
        ))}
      </div>
      <div className="bindingEditor">
        <div className="bindingLead">
          <Link2 size={18} />
          <div>
            <strong>Lier {selectedAsset.assetName} a une cotation</strong>
            <span>Rechercher le nom, le ticker Yahoo ou l'ISIN, puis choisir l'instrument qui correspond.</span>
          </div>
        </div>
        <form className="bindingSearch" onSubmit={(event) => { event.preventDefault(); searchInstruments.mutate(); }}>
          <label>
            Instrument de marche
            <span className="bindingSearchField">
              <Search size={16} />
              <input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Microsoft, MSFT ou US5949181045" />
            </span>
          </label>
          <button type="submit" disabled={query.trim().length < 2 || searchInstruments.isPending}>Rechercher</button>
        </form>
        {searchInstruments.error ? <p className="stateError">La recherche de cotation a echoue.</p> : null}
        {bindInstrument.error ? <p className="stateError">Le binding n'a pas pu etre enregistre.</p> : null}
        {bindInstrument.isSuccess ? <p className="stateSuccess">Cotation liee. La valorisation se met a jour avec le cours retenu.</p> : null}
        {results.length > 0 ? (
          <div className="bindingResults">
            {results.map((result) => (
              <article key={`${result.provider}-${result.symbol}`}>
                <div>
                  <strong>{result.name}</strong>
                  <span>{[result.symbol, result.currency, result.exchangeDisplay ?? result.exchange ?? result.provider].filter(Boolean).join(" - ")}</span>
                </div>
                <button type="button" onClick={() => bindInstrument.mutate(result)} disabled={bindInstrument.isPending}>
                  Utiliser ce cours
                </button>
              </article>
            ))}
          </div>
        ) : null}
      </div>
    </div>
  );
}
