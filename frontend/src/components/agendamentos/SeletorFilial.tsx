import { AlertCircle, Building2, Loader2, RefreshCw } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';

import type { FilialResumo } from '@/types/filial';

interface SeletorFilialProps {
  filialId: string;
  onChange: (filialId: string, filialNome: string) => void;
  filiais: FilialResumo[];
  carregando: boolean;
  erro: boolean;
  onRetry: () => void;
  tentouAvancar: boolean;
  disabled?: boolean;
}

/**
 * Campo obrigatório de seleção de filial (RF019).
 *
 * <p>Estados: loading, erro de carga (com retry), lista vazia (bloqueia),
 * normal e validação obrigatória. Acessível por teclado com `aria-invalid`,
 * `aria-describedby` e `aria-live`.</p>
 */
export function SeletorFilial({
  filialId,
  onChange,
  filiais,
  carregando,
  erro,
  onRetry,
  tentouAvancar,
  disabled = false,
}: SeletorFilialProps) {
  const erroIdRef = 'filial-erro';
  const temErroValidacao = tentouAvancar && !filialId && !carregando && !erro && filiais.length > 0;
  const semFiliais = !carregando && !erro && filiais.length === 0;

  function handleChange(e: React.ChangeEvent<HTMLSelectElement>) {
    const id = e.target.value;
    const filial = filiais.find((f) => f.id === id);
    onChange(id, filial?.nome ?? '');
  }

  /** Formata o label de cada opção: nome + cidade/UF quando disponíveis. */
  function formatarLabel(f: FilialResumo): string {
    const partes = [f.nome];
    if (f.cidade && f.uf) {
      partes.push(`(${f.cidade}/${f.uf})`);
    } else if (f.cidade) {
      partes.push(`(${f.cidade})`);
    } else if (f.uf) {
      partes.push(`(${f.uf})`);
    }
    return partes.join(' ');
  }

  return (
    <div className="space-y-1.5">
      <Label
        htmlFor="ag-filial"
        className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
      >
        FILIAL <span className="text-red-500">*</span>
      </Label>

      {carregando && (
        <div
          className="flex items-center gap-2 rounded-xl border border-zinc-800/40 bg-zinc-900/20 px-4 py-3 text-sm text-zinc-500"
          aria-live="polite"
        >
          <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
          Carregando filiais…
        </div>
      )}

      {erro && (
        <div className="rounded-xl border border-red-500/30 bg-red-950/20 px-4 py-3">
          <p className="text-sm text-red-400">
            Não foi possível carregar as filiais no momento. Tente novamente.
          </p>
          <Button
            type="button"
            variant="outline"
            onClick={onRetry}
            className="mt-2 h-8 rounded-full border-red-500/30 bg-transparent px-4 text-xs text-red-400 hover:bg-red-950/30"
          >
            <RefreshCw className="mr-1 h-3 w-3" /> Tentar novamente
          </Button>
        </div>
      )}

      {semFiliais && (
        <div className="rounded-xl border border-amber-500/30 bg-amber-950/20 px-4 py-3">
          <p className="text-sm text-amber-400">
            Não há filiais ativas disponíveis para agendamento.
          </p>
        </div>
      )}

      {!carregando && !erro && filiais.length > 0 && (
        <>
          <div className="relative">
            <Building2
              className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500"
              aria-hidden="true"
            />
            <select
              id="ag-filial"
              value={filialId}
              onChange={handleChange}
              disabled={disabled}
              aria-invalid={temErroValidacao}
              aria-describedby={temErroValidacao ? erroIdRef : undefined}
              className={`h-10 w-full cursor-pointer appearance-none rounded-xl pl-9 pr-4 text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 disabled:cursor-not-allowed disabled:opacity-50 [color-scheme:dark] ${
                temErroValidacao
                  ? 'border border-red-500/60 bg-red-950/20 text-zinc-200'
                  : 'border border-zinc-700/60 bg-zinc-900/50 text-zinc-200'
              }`}
            >
              <option value="" disabled className="text-zinc-600">
                Selecione uma filial
              </option>
              {filiais.map((f) => (
                <option key={f.id} value={f.id}>
                  {formatarLabel(f)}
                </option>
              ))}
            </select>
          </div>
        </>
      )}

      <div aria-live="polite">
        {temErroValidacao && (
          <p
            id={erroIdRef}
            role="alert"
            className="flex items-center gap-1.5 text-xs text-red-500"
          >
            <AlertCircle className="h-3.5 w-3.5" />
            Selecione uma filial para prosseguir.
          </p>
        )}
      </div>
    </div>
  );
}
