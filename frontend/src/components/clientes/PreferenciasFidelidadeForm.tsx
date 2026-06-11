import { ChevronRight, Plus, Trash2 } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useFieldArray, useFormContext, useWatch } from 'react-hook-form';

import { Button } from '@/components/ui/button';
import { maskCpfCnpj } from '@/lib/masks';

import { FiliadoModal } from './FiliadoModal';

import type {
  ClienteFormData,
  CanalValue,
  FiliadoFormData,
  LembreteValue,
} from '@/schemas/clienteSchema';

const LEMBRETES_OPTIONS: { value: LembreteValue; label: string }[] = [
  { value: '24H', label: '24H ANTES' },
  { value: '12H', label: '12H ANTES' },
  { value: '6H', label: '6H ANTES' },
  { value: '1H', label: '1H ANTES' },
  { value: 'NENHUM', label: 'NENHUM' },
];

const CANAIS_OPTIONS: { value: CanalValue; label: string }[] = [
  { value: 'WHATSAPP', label: 'WHATSAPP' },
  { value: 'EMAIL', label: 'E-MAIL' },
  { value: 'SMS', label: 'SMS' },
  { value: 'LIGACAO', label: 'LIGAÇÃO' },
];

interface PreferenciasFidelidadeFormProps {
  isSubmitting: boolean;
}

export function PreferenciasFidelidadeForm({ isSubmitting }: PreferenciasFidelidadeFormProps) {
  const { control, setValue } = useFormContext<ClienteFormData>();
  const [filiadoModalOpen, setFiliadoModalOpen] = useState(false);

  const watchedLembretes = useWatch({ control, name: 'lembretes' });
  const watchedCanais = useWatch({ control, name: 'canaisPreferenciais' });
  const observacoesGerais = useWatch({ control, name: 'observacoesGerais' }) ?? '';

  const {
    fields: filiados,
    append: appendFiliado,
    remove: removeFiliado,
  } = useFieldArray({
    control,
    name: 'filiados',
  });

  const toggleLembrete = useCallback(
    (value: LembreteValue) => {
      const current = watchedLembretes ?? [];
      const idx = current.indexOf(value);
      if (idx >= 0) {
        const next = [...current];
        next.splice(idx, 1);
        setValue('lembretes', next, { shouldDirty: true });
      } else {
        if (value === 'NENHUM') {
          setValue('lembretes', ['NENHUM'], { shouldDirty: true });
          return;
        }
        const filtered = current.filter((v) => v !== 'NENHUM');
        filtered.push(value);
        setValue('lembretes', filtered, { shouldDirty: true });
      }
    },
    [watchedLembretes, setValue],
  );

  const toggleCanal = useCallback(
    (value: CanalValue) => {
      const current = watchedCanais ?? [];
      const idx = current.indexOf(value);
      if (idx >= 0) {
        const next = [...current];
        next.splice(idx, 1);
        setValue('canaisPreferenciais', next, { shouldDirty: true });
      } else {
        setValue('canaisPreferenciais', [...current, value], { shouldDirty: true });
      }
    },
    [watchedCanais, setValue],
  );

  const existingCpfs = filiados.map((f) => f.cpf.replace(/\D/g, ''));

  const handleAddFiliado = useCallback(
    (filiado: FiliadoFormData) => {
      appendFiliado(filiado);
    },
    [appendFiliado],
  );

  return (
    <div>
      {/* Lembretes de agendamento */}
      <div className="mb-8">
        <h3 className="text-base font-semibold text-foreground">Lembretes de agendamento</h3>
        <p className="mt-0.5 text-sm text-muted-foreground">
          Quando o cliente deve receber lembretes antes do serviço?
        </p>
        <div className="mt-3 flex flex-wrap gap-2">
          {LEMBRETES_OPTIONS.map((opt) => {
            const isSelected = (watchedLembretes ?? []).includes(opt.value);
            return (
              <button
                key={opt.value}
                type="button"
                onClick={() => toggleLembrete(opt.value)}
                className={`rounded-lg border px-4 py-2 text-[11px] font-bold tracking-[0.1em] transition-all ${
                  isSelected
                    ? 'border-[#FF1F2E] bg-[#FF1F2E1F] text-foreground'
                    : 'border-border bg-transparent text-muted-foreground hover:border-ring hover:text-foreground'
                }`}
              >
                {opt.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* Canais de contato preferenciais */}
      <div className="mb-8">
        <h3 className="text-base font-semibold text-foreground">Canais de contato preferenciais</h3>
        <p className="mt-0.5 text-sm text-muted-foreground">Como o cliente prefere receber lembretes?</p>
        <div className="mt-3 flex flex-wrap gap-2">
          {CANAIS_OPTIONS.map((opt) => {
            const isSelected = (watchedCanais ?? []).includes(opt.value);
            return (
              <button
                key={opt.value}
                type="button"
                onClick={() => toggleCanal(opt.value)}
                className={`rounded-lg border px-4 py-2 text-[11px] font-bold tracking-[0.1em] transition-all ${
                  isSelected
                    ? 'border-[#FF1F2E] bg-[#FF1F2E1F] text-foreground'
                    : 'border-border bg-transparent text-muted-foreground hover:border-ring hover:text-foreground'
                }`}
              >
                {opt.label}
              </button>
            );
          })}
        </div>
      </div>

      {/* Observações gerais */}
      <div className="mb-8">
        <h3 className="text-base font-semibold text-foreground">Observações gerais</h3>
        <p className="mt-0.5 text-sm text-muted-foreground">Notas importantes sobre este cliente.</p>
        <textarea
          value={observacoesGerais}
          onChange={(e) => setValue('observacoesGerais', e.target.value, { shouldDirty: true })}
          placeholder="Ex.: cliente corporativo, atende terça a quinta..."
          rows={4}
          className="mt-3 w-full resize-none rounded-xl border border-border bg-card px-4 py-3 text-sm text-foreground placeholder:text-muted-foreground focus:border-border focus:outline-none"
          maxLength={1000}
        />
      </div>

      {/* Filiados */}
      <div className="mb-8">
        <h3 className="text-base font-semibold text-foreground">Filiados</h3>
        <p className="mt-0.5 text-sm text-muted-foreground">
          Adicione outras pessoas que são filiadas a este cliente. Elas também podem receber
          lembretes de agendamentos.
        </p>

        <div className="mt-4 space-y-3">
          {filiados.length === 0 ? (
            <div className="rounded-xl border border-border bg-card px-4 py-6 text-center">
              <p className="text-[11px] font-bold tracking-[0.15em] text-muted-foreground">
                NENHUM FILIADO ADICIONADO AINDA
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              {filiados.map((filiado, index) => {
                const detalhes = [
                  filiado.cpf ? maskCpfCnpj(filiado.cpf) : '',
                  filiado.telefone,
                  filiado.email,
                ]
                  .filter((item): item is string => Boolean(item && item.trim().length > 0))
                  .join(' · ');

                return (
                  <div
                    key={filiado.id}
                    className="flex items-center justify-between rounded-xl border border-border bg-card px-4 py-3 transition-colors hover:bg-accent"
                  >
                    <div className="min-w-0 flex-1">
                      <p className="text-sm font-medium text-foreground">{filiado.nome}</p>
                      <p className="text-xs text-muted-foreground">{detalhes}</p>
                    </div>
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeFiliado(index)}
                      className="ml-3 h-8 w-8 shrink-0 text-muted-foreground hover:bg-red-500/10 hover:text-red-400"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </div>
                );
              })}
            </div>
          )}

          <button
            type="button"
            onClick={() => setFiliadoModalOpen(true)}
            className="flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-border px-4 py-4 text-[11px] font-bold tracking-[0.15em] text-muted-foreground transition-colors hover:border-ring hover:text-foreground"
          >
            <Plus className="h-3.5 w-3.5" />
            ADICIONAR FILIADO
          </button>
        </div>
      </div>

      {/* Navigation */}
      <div className="flex items-center justify-end pt-2">
        <Button
          type="submit"
          disabled={isSubmitting}
          className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
        >
          {isSubmitting ? (
            'Salvando...'
          ) : (
            <>
              Concluir cadastro
              <ChevronRight className="ml-1 h-4 w-4" />
            </>
          )}
        </Button>
      </div>

      {/* Filiado Modal */}
      <FiliadoModal
        open={filiadoModalOpen}
        onOpenChange={setFiliadoModalOpen}
        existingCpfs={existingCpfs}
        onSave={handleAddFiliado}
      />
    </div>
  );
}
