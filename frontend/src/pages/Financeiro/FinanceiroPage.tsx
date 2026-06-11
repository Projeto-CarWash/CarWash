import { DollarSign, Construction, TrendingUp, Receipt, Wallet } from 'lucide-react';

import { Card, CardContent } from '@/components/ui/card';

/** Cartões de indicadores (placeholder — sem backend de financeiro ainda). */
const INDICADORES = [
  { icon: TrendingUp, rotulo: 'Faturamento', descricao: 'Receita no período' },
  { icon: Receipt, rotulo: 'A receber', descricao: 'Pagamentos pendentes' },
  { icon: Wallet, rotulo: 'Ticket médio', descricao: 'Valor médio por OS' },
];

/**
 * Tela de Financeiro (placeholder). O backend ainda não expõe endpoints
 * financeiros; esta tela já fica roteada e no tema (light/dark), pronta para
 * receber os dados reais quando a API existir.
 */
export function FinanceiroPage() {
  return (
    <div className="space-y-6 px-6 py-6 md:px-8 md:py-8">
      <div>
        <h1 className="flex items-center gap-2 text-2xl font-bold tracking-tight text-foreground">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-red-600/10 text-red-500">
            <DollarSign className="h-5 w-5" />
          </span>
          Financeiro
        </h1>
        <p className="mt-1 text-sm text-muted-foreground">
          Acompanhe o faturamento, recebimentos e indicadores financeiros da rede.
        </p>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {INDICADORES.map((ind) => (
          <Card key={ind.rotulo} className="border border-border bg-card">
            <CardContent className="flex items-center gap-4 p-5">
              <span className="flex h-11 w-11 items-center justify-center rounded-xl bg-muted text-muted-foreground">
                <ind.icon className="h-5 w-5" />
              </span>
              <div>
                <p className="text-sm font-semibold text-foreground">{ind.rotulo}</p>
                <p className="text-xs text-muted-foreground">{ind.descricao}</p>
                <p className="mt-1 text-lg font-bold text-muted-foreground/60">—</p>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card className="border border-dashed border-border bg-card">
        <CardContent className="flex flex-col items-center justify-center gap-3 px-6 py-16 text-center">
          <span className="flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
            <Construction className="h-6 w-6" />
          </span>
          <h2 className="text-lg font-semibold text-foreground">Módulo em construção</h2>
          <p className="max-w-md text-sm text-muted-foreground">
            O módulo financeiro está em desenvolvimento. Em breve você poderá consultar faturamento,
            contas a receber e relatórios financeiros por filial e período.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
