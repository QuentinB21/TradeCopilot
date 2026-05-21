import { useMutation } from "@tanstack/react-query";
import { BrainCircuit } from "lucide-react";
import { useState } from "react";
import { tradeCopilotApi } from "../api/tradeCopilotApi";
import { DecimalInput } from "../components/DecimalInput";
import { formatCurrency, formatPercent } from "../lib/format";
import { parseDecimalInput } from "../lib/numberInput";
import { Metric } from "../components/Metric";
import { PageHeader } from "../components/PageHeader";
import { Panel } from "../components/Panel";

export function AssistantPage() {
  const [amount, setAmount] = useState("400");
  const monthlyPlan = useMutation({ mutationFn: tradeCopilotApi.createMonthlyPlan });
  const amountValue = parseDecimalInput(amount);

  return (
    <>
      <PageHeader
        title="Assistant mensuel"
        description="Transformer les cles cibles et les statuts strategiques en proposition d'allocation mensuelle."
        action={<button className="ghostButton" onClick={() => monthlyPlan.mutate(amountValue)} type="button"><BrainCircuit size={18} /> Calculer</button>}
      />
      <section className="assistantWorkspace">
        <Panel className="assistantInputPanel" title="Parametres" subtitle="Le calcul reste consultatif et ne declenche aucun ordre.">
          <form className="form" onSubmit={(event) => { event.preventDefault(); monthlyPlan.mutate(amountValue); }}>
            <label>Montant a investir<DecimalInput min={0} step={50} value={amount} onChange={setAmount} /></label>
            <button type="submit">Generer la recommandation</button>
          </form>
          {monthlyPlan.data ? (
            <div className="metrics singleMetric">
              <Metric title="Montant analyse" value={formatCurrency(monthlyPlan.data.amount)} icon={<BrainCircuit size={20} />} />
            </div>
          ) : null}
        </Panel>

        <Panel className="assistantPlanPanel" title="Plan propose">
          {monthlyPlan.data ? (
            <div className="plan">
              {monthlyPlan.data.envelopes.map((envelope) => (
                <div className="planEnvelope" key={envelope.portfolioId}>
                  <div className="splitHeader"><strong>{envelope.portfolioName}</strong><span>{formatCurrency(envelope.amount)}</span></div>
                  {envelope.lines.map((line) => (
                    <div className="compactRow" key={line.assetId}>
                      <div>
                        <strong>{line.assetName}</strong>
                        <span>{line.symbol}</span>
                      </div>
                      <div className="rightText">
                        <strong>{formatCurrency(line.amount)}</strong>
                        <span>Cible {formatPercent(line.targetWeight)}</span>
                      </div>
                    </div>
                  ))}
                </div>
              ))}
              <ul className="notes">{monthlyPlan.data.notes.map((note) => <li key={note}>{note}</li>)}</ul>
            </div>
          ) : (
            <p className="emptyState">Lance le calcul pour obtenir une proposition d'achat mensuelle. Aucune operation reelle n'est executee.</p>
          )}
        </Panel>
      </section>
    </>
  );
}
