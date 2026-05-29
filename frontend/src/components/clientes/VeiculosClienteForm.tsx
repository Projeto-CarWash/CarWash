import { Car, Plus, Trash2 } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useFieldArray, useFormContext } from 'react-hook-form';

import { Button } from '@/components/ui/button';

import { VeiculoModal } from './VeiculoModal';

import type { ClienteFormData, VeiculoLocalFormData } from '@/schemas/clienteSchema';

export function VeiculosClienteForm() {
  const {
    control,
    formState: { errors },
  } = useFormContext<ClienteFormData>();
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'veiculos',
  });

  const [modalOpen, setModalOpen] = useState(false);

  const existingPlacas = fields.map((v) => v.placa);

  const handleAddVeiculo = useCallback(
    (veiculo: VeiculoLocalFormData) => {
      append(veiculo);
    },
    [append],
  );

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-zinc-100">Veículos vinculados</h3>
        <p className="mt-1 text-sm text-zinc-500">
          Adicione um ou mais veículos. Cada veículo fica associado a este cliente.
        </p>
      </div>

      {/* Lista de veículos */}
      <div className="space-y-3">
        {fields.length === 0 ? (
          <div className="rounded-xl border border-zinc-800/60 bg-zinc-900/20 px-4 py-6 text-center">
            <p className="text-[11px] font-bold tracking-[0.15em] text-zinc-500">
              NENHUM VEÍCULO VINCULADO AINDA
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {fields.map((field, index) => (
              <div
                key={field.id}
                className="flex items-center justify-between rounded-xl border border-zinc-800/60 bg-zinc-900/20 px-4 py-3 transition-colors hover:bg-zinc-800/20"
              >
                <div className="flex items-center gap-3 min-w-0 flex-1">
                  <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-zinc-800/60">
                    <Car className="h-5 w-5 text-zinc-400" />
                  </div>
                  <div className="min-w-0">
                    <p className="text-sm font-medium text-zinc-200">
                      {field.marca && field.modelo
                        ? `${field.marca} ${field.modelo}`
                        : field.modelo || 'Veículo'}
                    </p>
                    <p className="text-xs text-zinc-500">
                      {[field.anoModelo, field.cor?.toUpperCase()].filter(Boolean).join(' · ')}
                    </p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <span className="font-mono text-sm font-medium tracking-wider text-zinc-300">
                    {field.placa.length >= 3
                      ? `${field.placa.slice(0, 3)}-${field.placa.slice(3)}`
                      : field.placa}
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => remove(index)}
                    className="h-8 w-8 shrink-0 text-zinc-400 hover:bg-red-500/10 hover:text-red-400"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Botão adicionar */}
        <button
          type="button"
          onClick={() => setModalOpen(true)}
          className="flex w-full items-center justify-center gap-2 rounded-xl border-2 border-dashed border-zinc-700/60 px-4 py-4 text-[11px] font-bold tracking-[0.15em] text-zinc-500 transition-colors hover:border-zinc-600 hover:text-zinc-300"
        >
          <Plus className="h-3.5 w-3.5" />
          ADICIONAR VEÍCULO
        </button>
      </div>

      {/* Erros de array */}
      {errors.veiculos?.root?.message && (
        <p className="mt-2 text-sm font-medium text-red-400">{errors.veiculos.root.message}</p>
      )}
      {errors.veiculos?.message && (
        <p className="mt-2 text-sm font-medium text-red-400">{errors.veiculos.message}</p>
      )}

      {/* Modal */}
      <VeiculoModal
        open={modalOpen}
        onOpenChange={setModalOpen}
        veiculoNumero={fields.length + 1}
        existingPlacas={existingPlacas}
        onSave={handleAddVeiculo}
      />
    </div>
  );
}
