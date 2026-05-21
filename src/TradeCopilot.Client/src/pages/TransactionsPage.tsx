import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { FileUp } from "lucide-react";
import { DragEvent, useMemo, useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { ActionIconButton } from "../components/ActionIconButton";
import { DecimalInput } from "../components/DecimalInput";
import { transactionTypes } from "../domain/options";
import type { CreateTransactionPayload, Transaction, TransactionImportProvider, TransactionImportResult } from "../domain/types";
import { formatCurrency } from "../lib/format";
import { parseDecimalInput, toNumberInput } from "../lib/numberInput";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";
import { QueryState } from "../components/QueryState";

type TransactionForm = Omit<CreateTransactionPayload, "quantity" | "unitPrice" | "fees"> & {
  quantity: string;
  unitPrice: string;
  fees: string;
};

const emptyTransaction: TransactionForm = {
  portfolioId: "",
  assetId: "",
  type: "Buy",
  date: new Date().toISOString().slice(0, 10),
  quantity: "",
  unitPrice: "",
  fees: "",
  currency: "EUR",
  comment: ""
};

function errorText(error: unknown) {
  return error instanceof Error ? error.message : "Operation impossible pour le moment.";
}

type WarningGroup = {
  code: string;
  message: string;
  recommendation: string;
  rows: number[];
  count: number;
};

function groupWarnings(warnings: TransactionImportResult["warnings"]): WarningGroup[] {
  const groups = new Map<string, WarningGroup>();
  for (const warning of warnings) {
    const key = `${warning.code}|${warning.message}|${warning.recommendation}`;
    const group = groups.get(key) ?? {
      code: warning.code,
      message: warning.message,
      recommendation: warning.recommendation,
      rows: [],
      count: 0
    };

    group.count += 1;
    if (warning.rowNumber !== null) {
      group.rows.push(warning.rowNumber);
    }

    groups.set(key, group);
  }

  return Array.from(groups.values());
}

function formatRows(rows: number[]) {
  if (rows.length === 0) {
    return "Fichier";
  }

  const sortedRows = [...rows].sort((left, right) => left - right);
  const ranges: string[] = [];
  let start = sortedRows[0];
  let previous = sortedRows[0];

  for (const row of sortedRows.slice(1)) {
    if (row === previous + 1) {
      previous = row;
      continue;
    }

    ranges.push(start === previous ? `${start}` : `${start}-${previous}`);
    start = row;
    previous = row;
  }

  ranges.push(start === previous ? `${start}` : `${start}-${previous}`);
  return `Ligne${rows.length > 1 ? "s" : ""} ${ranges.join(", ")}`;
}

export function TransactionsPage() {
  const queryClient = useQueryClient();
  const portfoliosQuery = useQuery({ queryKey: ["portfolios"], queryFn: tradeCopilotApi.getPortfolios });
  const assetsQuery = useQuery({ queryKey: ["assets"], queryFn: tradeCopilotApi.getAssets });
  const transactionsQuery = useQuery({ queryKey: ["transactions"], queryFn: tradeCopilotApi.getTransactions });
  const [form, setForm] = useState<TransactionForm>(emptyTransaction);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [importProvider, setImportProvider] = useState<TransactionImportProvider>("TradeRepublic");
  const [importPortfolioId, setImportPortfolioId] = useState("");
  const [importFile, setImportFile] = useState<File | null>(null);
  const [isFileDragActive, setIsFileDragActive] = useState(false);

  const assetById = useMemo(() => new Map((assetsQuery.data ?? []).map((asset) => [asset.id, asset])), [assetsQuery.data]);
  const portfolioById = useMemo(() => new Map((portfoliosQuery.data ?? []).map((portfolio) => [portfolio.id, portfolio])), [portfoliosQuery.data]);

  const invalidate = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ["transactions"] }),
      queryClient.invalidateQueries({ queryKey: ["positions"] }),
      queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
      queryClient.invalidateQueries({ queryKey: ["assets"] }),
      queryClient.invalidateQueries({ queryKey: ["portfolios"] })
    ]);
  };

  const resetForm = () => {
    setForm(emptyTransaction);
    setEditingId(null);
  };

  const editTransaction = (transaction: Transaction) => {
    setEditingId(transaction.id);
    setForm({
      portfolioId: transaction.portfolioId,
      assetId: transaction.assetId ?? "",
      type: transaction.type,
      date: transaction.date,
      quantity: toNumberInput(transaction.quantity),
      unitPrice: toNumberInput(transaction.unitPrice),
      fees: toNumberInput(transaction.fees),
      currency: transaction.currency,
      comment: transaction.comment ?? ""
    });
  };

  const saveTransaction = useMutation({
    mutationFn: () => {
      const payload: CreateTransactionPayload = {
        ...form,
        assetId: form.assetId || null,
        quantity: parseDecimalInput(form.quantity),
        unitPrice: parseDecimalInput(form.unitPrice),
        fees: parseDecimalInput(form.fees)
      };
      return editingId
        ? tradeCopilotApi.updateTransaction(editingId, payload)
        : tradeCopilotApi.createTransaction(payload);
    },
    onSuccess: async () => {
      resetForm();
      await invalidate();
    }
  });

  const deleteTransaction = useMutation({
    mutationFn: tradeCopilotApi.deleteTransaction,
    onSuccess: invalidate
  });

  const importTransactions = useMutation({
    mutationFn: () => {
      if (!importFile) {
        throw new Error("Selectionnez un fichier CSV ou Excel.");
      }

      return tradeCopilotApi.importTransactions(importProvider, importPortfolioId, importFile);
    },
    onSuccess: async () => {
      setImportFile(null);
      await invalidate();
    }
  });

  const transactions = transactionsQuery.data ?? [];
  const importWarningGroups = groupWarnings(importTransactions.data?.warnings ?? []);

  function useDroppedFile(event: DragEvent<HTMLLabelElement>) {
    event.preventDefault();
    setIsFileDragActive(false);
    setImportFile(event.dataTransfer.files?.[0] ?? null);
  }

  return (
    <>
      <PageHeader title="Transactions" description="Importer en priorite, corriger ensuite. Les saisies manuelles restent disponibles pour les cas incomplets." />
      <section className="transactionsWorkspace">
        <Panel className="importPanel" title="Importer des transactions" subtitle="Choisir la provenance pour appliquer le bon traitement CSV.">
          <form className="form" onSubmit={(event) => { event.preventDefault(); importTransactions.mutate(); }}>
            <label>Provenance<select value={importProvider} onChange={(event) => setImportProvider(event.target.value as TransactionImportProvider)}><option value="TradeRepublic">Trade Republic</option><option value="Boursobank">Boursobank</option></select></label>
            <label>Portefeuille cible<select value={importPortfolioId} onChange={(event) => setImportPortfolioId(event.target.value)} required><option value="">Selectionner</option>{(portfoliosQuery.data ?? []).map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
            <label
              className={isFileDragActive ? "fileDropzone active" : "fileDropzone"}
              onDragEnter={(event) => { event.preventDefault(); setIsFileDragActive(true); }}
              onDragLeave={() => setIsFileDragActive(false)}
              onDragOver={(event) => event.preventDefault()}
              onDrop={useDroppedFile}
            >
              <span className="fileDropIcon"><FileUp size={38} strokeWidth={1.8} /></span>
              <strong>{importFile ? importFile.name : "Glissez-deposez votre fichier CSV ou XLSX ici"}</strong>
              <span className="fileDropSeparator">ou</span>
              <span className="filePicker">{importFile ? "Remplacer" : "Parcourir"}</span>
              <input
                className="fileInput"
                type="file"
                accept=".csv,.xlsx,text/csv,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                onChange={(event) => setImportFile(event.target.files?.[0] ?? null)}
                required
              />
            </label>
            <button type="submit" disabled={!importPortfolioId || !importFile || importTransactions.isPending}>Importer les transactions</button>
          </form>
          {importTransactions.data ? (
            <div className="importSummary">
              <strong>{importTransactions.data.importedTransactions} transaction(s) importee(s)</strong>
              <span>{importTransactions.data.createdAssets} actif(s) cree(s), {importTransactions.data.duplicateRows} doublon(s), {importTransactions.data.skippedRows} ligne(s) ignoree(s).</span>
              {importTransactions.data.warnings.length > 0 ? (
                <details>
                  <summary>{importTransactions.data.warnings.length} avertissement(s), regroupes en {importWarningGroups.length} sujet(s)</summary>
                  <div className="warningGroups">
                    {importWarningGroups.map((warning) => (
                      <div className="warningGroup" key={`${warning.code}-${warning.message}`}>
                        <strong>{formatRows(warning.rows)} - {warning.message}</strong>
                        <span>{warning.recommendation}</span>
                      </div>
                    ))}
                  </div>
                </details>
              ) : null}
              {importTransactions.data.duplicateRows > 0 ? (
                <span>{importTransactions.data.duplicateRows} ligne(s) existaient deja dans TradeCopilot et ont ete ignorees. C'est le comportement attendu lors d'un nouvel import contenant d'anciennes operations.</span>
              ) : null}
            </div>
          ) : null}
          {importTransactions.error && <p className="stateError">{errorText(importTransactions.error)}</p>}
        </Panel>

        <Panel className="transactionEditorPanel" title={editingId ? "Modifier la transaction" : "Saisie manuelle"} subtitle="Pour une position deja detenue, saisir l'achat d'ouverture avec quantite et PRU.">
          <form className="form" onSubmit={(event) => { event.preventDefault(); saveTransaction.mutate(); }}>
            <label>Portefeuille<select value={form.portfolioId} onChange={(event) => setForm({ ...form, portfolioId: event.target.value })} required><option value="">Selectionner</option>{(portfoliosQuery.data ?? []).map((portfolio) => <option value={portfolio.id} key={portfolio.id}>{portfolio.name}</option>)}</select></label>
            <label>Actif<select value={form.assetId ?? ""} onChange={(event) => setForm({ ...form, assetId: event.target.value })}><option value="">Aucun</option>{(assetsQuery.data ?? []).map((asset) => <option value={asset.id} key={asset.id}>{asset.name} - {asset.symbol}</option>)}</select></label>
            <label>Type<select value={form.type} onChange={(event) => setForm({ ...form, type: event.target.value as CreateTransactionPayload["type"] })}>{transactionTypes.map((type) => <option key={type}>{type}</option>)}</select></label>
            <label>Date<input type="date" value={form.date} onChange={(event) => setForm({ ...form, date: event.target.value })} required /></label>
            <label>Quantite<DecimalInput step="0.000001" value={form.quantity} onChange={(value) => setForm({ ...form, quantity: value })} /></label>
            <label>Prix unitaire ou PRU<DecimalInput step="0.000001" value={form.unitPrice} onChange={(value) => setForm({ ...form, unitPrice: value })} /></label>
            <label>Frais<DecimalInput step="0.01" value={form.fees} onChange={(value) => setForm({ ...form, fees: value })} /></label>
            <label>Devise<input value={form.currency} onChange={(event) => setForm({ ...form, currency: event.target.value })} maxLength={3} required /></label>
            <label>Commentaire<input value={form.comment ?? ""} onChange={(event) => setForm({ ...form, comment: event.target.value })} /></label>
            <div className="formActions">
              <button type="submit" disabled={!form.portfolioId || saveTransaction.isPending}>{editingId ? "Mettre a jour" : "Ajouter"}</button>
              {editingId && <button className="secondaryButton" type="button" onClick={resetForm}>Annuler</button>}
            </div>
          </form>
          {(saveTransaction.error || deleteTransaction.error) && <p className="stateError">{errorText(saveTransaction.error ?? deleteTransaction.error)}</p>}
        </Panel>

        <Panel className="ledgerPanel" title="Historique" subtitle="Operations importees et corrections manuelles.">
          <QueryState isLoading={transactionsQuery.isLoading} error={transactionsQuery.error}>
            {transactions.length === 0 ? (
              <p className="emptyState">Aucune transaction saisie.</p>
            ) : (
              <div className="tableWrap compactTable">
                <table>
                  <thead><tr><th>Date</th><th>Type</th><th>Portefeuille</th><th>Actif</th><th>Quantite</th><th>Montant</th><th></th></tr></thead>
                  <tbody>
                    {transactions.map((transaction) => (
                      <tr className={editingId === transaction.id ? "editingRow" : undefined} key={transaction.id}>
                        <td>{transaction.date}</td>
                        <td>{transaction.type}</td>
                        <td>{portfolioById.get(transaction.portfolioId)?.name ?? "-"}</td>
                        <td>
                          {transaction.assetId ? (
                            <>
                              <strong>{assetById.get(transaction.assetId)?.name ?? "-"}</strong>
                              <span>{assetById.get(transaction.assetId)?.symbol ?? "-"}</span>
                            </>
                          ) : "-"}
                        </td>
                        <td>{transaction.quantity}</td>
                        <td>{formatCurrency(transaction.quantity * transaction.unitPrice + transaction.fees)}</td>
                        <td>
                          <div className="rowActions">
                            {editingId === transaction.id ? <span className="editingBadge">En edition</span> : null}
                            <ActionIconButton action="edit" isActive={editingId === transaction.id} label={`Modifier la transaction du ${transaction.date}`} onClick={() => editTransaction(transaction)} />
                            <ActionIconButton action="delete" label={`Supprimer la transaction du ${transaction.date}`} onClick={() => deleteTransaction.mutate(transaction.id)} />
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
