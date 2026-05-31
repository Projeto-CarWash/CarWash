import { Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useFieldArray, useFormContext } from 'react-hook-form';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

import type { ClienteFormData } from '@/schemas/clienteSchema';

export function VeiculosClienteForm() {
  const {
    control,
    trigger,
    formState: { errors },
  } = useFormContext<ClienteFormData>();
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'veiculos',
  });

  const [placa, setPlaca] = useState('');
  const [modelo, setModelo] = useState('');
  const [fabricante, setFabricante] = useState('');
  const [cor, setCor] = useState('');
  const [localErrors, setLocalErrors] = useState<Record<string, string>>({});

  const handleAddVeiculo = async () => {
    // Validate local fields
    const veiculoLocalSchema = z.object({
      placa: z
        .string()
        .min(1, 'Placa é obrigatória.')
        .transform((val) => val.trim().replace(/\s+/g, '').replace(/-/g, '').toUpperCase())
        .refine((val) => val.length === 7, {
          message: 'Placa deve conter 7 caracteres válidos.',
        })
        .refine((val) => /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(val), {
          message: 'Placa inválida. Formatos aceitos: AAA0000 ou AAA0A00.',
        }),
      modelo: z
        .string()
        .min(1, 'Modelo é obrigatório.')
        .transform((val) => val.trim())
        .refine((val) => val.length >= 2 && val.length <= 80, {
          message: 'Modelo deve ter entre 2 e 80 caracteres.',
        }),
      fabricante: z
        .string()
        .min(1, 'Fabricante é obrigatório.')
        .transform((val) => val.trim())
        .refine((val) => val.length >= 2 && val.length <= 80, {
          message: 'Fabricante deve ter entre 2 e 80 caracteres.',
        }),
      cor: z
        .string()
        .min(1, 'Cor é obrigatória.')
        .transform((val) => val.trim())
        .refine((val) => val.length >= 2 && val.length <= 40, {
          message: 'Cor deve ter entre 2 e 40 caracteres.',
        }),
    });

    const data = { placa, modelo, fabricante, cor };
    const result = veiculoLocalSchema.safeParse(data);

    if (!result.success) {
      const errMap: Record<string, string> = {};
      result.error.issues.forEach((err: z.ZodIssue) => {
        if (err.path[0]) {
          errMap[err.path[0] as string] = err.message;
        }
      });
      setLocalErrors(errMap);
      return;
    }

    const validatedData = result.data;

    // Check for duplicates
    if (fields.some((v) => v.placa === validatedData.placa)) {
      setLocalErrors({ placa: 'Esta placa já foi adicionada no cadastro atual.' });
      return;
    }

    setLocalErrors({});
    append(validatedData);

    // Clear local inputs
    setPlaca('');
    setModelo('');
    setFabricante('');
    setCor('');

    // Trigger validation for the veiculos array so the min(1) error goes away if present
    await trigger('veiculos');
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium text-zinc-100">Veículos do Cliente</h3>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div className="space-y-2">
          <Label
            htmlFor="veiculo-placa"
            className={localErrors.placa ? 'text-red-400' : 'text-zinc-300'}
          >
            Placa <span className="text-red-500">*</span>
          </Label>
          <Input
            id="veiculo-placa"
            value={placa}
            onChange={(e) => setPlaca(e.target.value.toUpperCase())}
            placeholder="AAA0000"
            className={`border-zinc-800 bg-zinc-900/50 text-zinc-100 placeholder:text-zinc-600 ${
              localErrors.placa ? 'border-red-500/50 focus-visible:ring-red-500/50' : ''
            }`}
          />
          {localErrors.placa && (
            <p className="text-xs font-medium text-red-400">{localErrors.placa}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label
            htmlFor="veiculo-modelo"
            className={localErrors.modelo ? 'text-red-400' : 'text-zinc-300'}
          >
            Modelo <span className="text-red-500">*</span>
          </Label>
          <Input
            id="veiculo-modelo"
            value={modelo}
            onChange={(e) => setModelo(e.target.value)}
            placeholder="Ex: Civic"
            className={`border-zinc-800 bg-zinc-900/50 text-zinc-100 placeholder:text-zinc-600 ${
              localErrors.modelo ? 'border-red-500/50 focus-visible:ring-red-500/50' : ''
            }`}
          />
          {localErrors.modelo && (
            <p className="text-xs font-medium text-red-400">{localErrors.modelo}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label
            htmlFor="veiculo-fabricante"
            className={localErrors.fabricante ? 'text-red-400' : 'text-zinc-300'}
          >
            Fabricante <span className="text-red-500">*</span>
          </Label>
          <Input
            id="veiculo-fabricante"
            value={fabricante}
            onChange={(e) => setFabricante(e.target.value)}
            placeholder="Ex: Honda"
            className={`border-zinc-800 bg-zinc-900/50 text-zinc-100 placeholder:text-zinc-600 ${
              localErrors.fabricante ? 'border-red-500/50 focus-visible:ring-red-500/50' : ''
            }`}
          />
          {localErrors.fabricante && (
            <p className="text-xs font-medium text-red-400">{localErrors.fabricante}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label
            htmlFor="veiculo-cor"
            className={localErrors.cor ? 'text-red-400' : 'text-zinc-300'}
          >
            Cor <span className="text-red-500">*</span>
          </Label>
          <div className="flex gap-2">
            <div className="flex-1">
              <Input
                id="veiculo-cor"
                value={cor}
                onChange={(e) => setCor(e.target.value)}
                placeholder="Ex: Prata"
                className={`border-zinc-800 bg-zinc-900/50 text-zinc-100 placeholder:text-zinc-600 ${
                  localErrors.cor ? 'border-red-500/50 focus-visible:ring-red-500/50' : ''
                }`}
              />
            </div>
            <Button
              type="button"
              onClick={handleAddVeiculo}
              className="shrink-0 bg-red-600 hover:bg-red-700"
              title="Adicionar veículo"
            >
              <Plus className="h-4 w-4" />
            </Button>
          </div>
          {localErrors.cor && <p className="text-xs font-medium text-red-400">{localErrors.cor}</p>}
        </div>
      </div>

      <div className="mt-6 rounded-lg border border-zinc-800 bg-zinc-900/20 overflow-hidden">
        <div className="flex items-center justify-between border-b border-zinc-800 bg-zinc-800/30 px-4 py-3">
          <h4 className="text-sm font-medium text-zinc-300">
            Veículos adicionados: {fields.length}
          </h4>
        </div>

        {fields.length === 0 ? (
          <div className="px-4 py-8 text-center">
            <p className="text-sm text-zinc-500">
              Nenhum veículo adicionado. Adicione ao menos um veículo para concluir o cadastro.
            </p>
          </div>
        ) : (
          <ul className="divide-y divide-zinc-800">
            {fields.map((field, index) => (
              <li
                key={field.id}
                className="flex items-center justify-between px-4 py-3 hover:bg-zinc-800/10 transition-colors"
              >
                <div className="grid flex-1 grid-cols-2 gap-4 sm:grid-cols-4">
                  <div>
                    <span className="block text-[10px] uppercase tracking-wider text-zinc-500">
                      Placa
                    </span>
                    <span className="font-medium text-zinc-200">{field.placa}</span>
                  </div>
                  <div>
                    <span className="block text-[10px] uppercase tracking-wider text-zinc-500">
                      Modelo
                    </span>
                    <span className="text-zinc-300">{field.modelo}</span>
                  </div>
                  <div>
                    <span className="block text-[10px] uppercase tracking-wider text-zinc-500">
                      Fabricante
                    </span>
                    <span className="text-zinc-300">{field.fabricante}</span>
                  </div>
                  <div>
                    <span className="block text-[10px] uppercase tracking-wider text-zinc-500">
                      Cor
                    </span>
                    <span className="text-zinc-300">{field.cor}</span>
                  </div>
                </div>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  onClick={() => remove(index)}
                  className="ml-4 h-8 w-8 text-zinc-400 hover:bg-red-500/10 hover:text-red-400"
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {(errors.veiculos?.root?.message ?? errors.veiculos?.message) && (
        <div
          role="alert"
          className="flex items-start gap-3 rounded-xl border border-red-500/40 bg-red-950/20 px-4 py-3 animate-in fade-in duration-200"
        >
          <span className="mt-0.5 flex h-4 w-4 shrink-0 items-center justify-center rounded-full bg-red-500/20 text-red-500">
            !
          </span>
          <p className="text-sm font-medium text-red-400">
            {errors.veiculos?.root?.message ?? errors.veiculos?.message}
          </p>
        </div>
      )}
    </div>
  );
}
