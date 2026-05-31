import { Car, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { type VeiculoLocalFormData, veiculoItemSchema } from '@/schemas/clienteSchema';

const PRESET_COLORS = [
  { name: 'VERMELHO', hex: '#E61F2D' },
  { name: 'PRETO', hex: '#1A1A1A' },
  { name: 'BRANCO', hex: '#F5F5F5' },
  { name: 'CINZA', hex: '#808080' },
  { name: 'AZUL', hex: '#2563EB' },
  { name: 'BEGE', hex: '#D4A843' },
  { name: 'VERDE', hex: '#22C55E' },
] as const;

interface VeiculoModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  veiculoNumero: number;
  existingPlacas: string[];
  onSave: (veiculo: VeiculoLocalFormData) => void;
}

export function VeiculoModal({
  open,
  onOpenChange,
  veiculoNumero,
  existingPlacas,
  onSave,
}: VeiculoModalProps) {
  const [placa, setPlaca] = useState('');
  const [renavam, setRenavam] = useState('');
  const [marca, setMarca] = useState('');
  const [modelo, setModelo] = useState('');
  const [anoModelo, setAnoModelo] = useState('');
  const [categoria, setCategoria] = useState('');
  const [cor, setCor] = useState('');
  const [corHex, setCorHex] = useState('');
  const [observacoes, setObservacoes] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    const val = placa.replace(/[\s-]/g, '');
    if (val.length === 7) {
      if (!/^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/i.test(val)) {
        // eslint-disable-next-line react-hooks/set-state-in-effect
        setErrors((prev) => ({ ...prev, placa: 'Formato inválido (ex: ABC-1234 ou ABC1D23)' }));
      } else {
        setErrors((prev) => {
          const { placa: _placa, ...rest } = prev;
          return rest;
        });
      }
    } else {
      setErrors((prev) => {
        if (prev.placa === 'Formato inválido (ex: ABC-1234 ou ABC1D23)') {
          const { placa: _placa, ...rest } = prev;
          return rest;
        }
        return prev;
      });
    }
  }, [placa]);

  const placaDisplay = placa.length >= 3 ? `${placa.slice(0, 3)}-${placa.slice(3)}` : placa;

  useEffect(() => {
    const nums = anoModelo.replace(/\D/g, '');
    let rangeError = false;
    let compatError = false;

    if (nums.length >= 4) {
      const year1 = parseInt(nums.slice(0, 4), 10);
      if (year1 < 1930 || year1 > 2027) rangeError = true;
    }
    if (nums.length === 8) {
      const year1 = parseInt(nums.slice(0, 4), 10);
      const year2 = parseInt(nums.slice(4, 8), 10);
      if (year2 < 1930 || year2 > 2027) rangeError = true;

      if (!rangeError && year2 !== year1 && year2 !== year1 + 1) {
        compatError = true;
      }
    }

    // eslint-disable-next-line react-hooks/set-state-in-effect
    setErrors((prev) => {
      const currentErr = prev.anoModelo;

      if (rangeError) {
        if (currentErr !== 'O ano deve ser entre 1930 e 2027.') {
          return { ...prev, anoModelo: 'O ano deve ser entre 1930 e 2027.' };
        }
        return prev;
      }

      if (compatError) {
        if (currentErr !== 'O modelo deve ser do mesmo ano ou até 1 ano à frente.') {
          return { ...prev, anoModelo: 'O modelo deve ser do mesmo ano ou até 1 ano à frente.' };
        }
        return prev;
      }

      if (
        currentErr === 'O ano deve ser entre 1930 e 2027.' ||
        currentErr === 'O modelo deve ser do mesmo ano ou até 1 ano à frente.'
      ) {
        const { anoModelo: _anoModelo, ...rest } = prev;
        return rest;
      }

      return prev;
    });
  }, [anoModelo]);

  const resetForm = useCallback(() => {
    setPlaca('');
    setRenavam('');
    setMarca('');
    setModelo('');
    setAnoModelo('');
    setCategoria('');
    setCor('');
    setCorHex('');
    setObservacoes('');
    setErrors({});
  }, []);

  const handleClose = useCallback(() => {
    resetForm();
    onOpenChange(false);
  }, [resetForm, onOpenChange]);

  const handleSelectColor = useCallback((name: string, hex: string) => {
    setCor(name);
    setCorHex(hex);
  }, []);

  const handleSave = useCallback(() => {
    const data = {
      placa,
      renavam: renavam || undefined,
      marca,
      modelo,
      anoModelo: anoModelo || undefined,
      categoria: categoria || undefined,
      cor,
      corHex: corHex || undefined,
      observacoesAtendimento: observacoes || undefined,
    };

    const result = veiculoItemSchema.safeParse(data);

    if (!result.success) {
      const errMap: Record<string, string> = {};
      result.error.issues.forEach((err) => {
        if (err.path[0]) {
          errMap[err.path[0] as string] = err.message;
        }
      });
      setErrors(errMap);
      return;
    }

    const normalizedPlaca = result.data.placa;
    if (existingPlacas.includes(normalizedPlaca)) {
      setErrors({ placa: 'Esta placa já foi adicionada.' });
      return;
    }

    setErrors({});
    onSave({
      placa: result.data.placa,
      renavam: result.data.renavam,
      marca: result.data.marca,
      modelo: result.data.modelo,
      anoModelo: result.data.anoModelo,
      categoria: result.data.categoria,
      cor: result.data.cor,
      corHex: result.data.corHex,
      observacoesAtendimento: result.data.observacoesAtendimento,
    });
    resetForm();
    onOpenChange(false);
  }, [
    placa,
    renavam,
    marca,
    modelo,
    anoModelo,
    categoria,
    cor,
    corHex,
    observacoes,
    existingPlacas,
    onSave,
    onOpenChange,
    resetForm,
  ]);

  const placaIsValid = /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/i.test(placa.replace(/[\s-]/g, ''));

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) handleClose();
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="!max-w-lg overflow-y-auto max-h-[90vh] rounded-2xl border border-zinc-800/60 bg-zinc-900 p-0 text-zinc-100 shadow-2xl sm:!max-w-lg"
      >
        <DialogTitle className="sr-only">Adicionar veículo</DialogTitle>
        <DialogDescription className="sr-only">
          Formulário para adicionar um novo veículo ao cadastro do cliente.
        </DialogDescription>

        <div className="p-6 pb-0">
          {/* Header: placa + badge veículo nº */}
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-3">
              {placa.length > 0 ? (
                <div className="flex items-center gap-1.5 rounded-lg border-2 border-zinc-600 bg-zinc-800 px-3 py-1.5">
                  <div className="flex h-5 items-center rounded bg-blue-700 px-1.5">
                    <span className="text-[8px] font-bold leading-none text-white">BR · RJ</span>
                  </div>
                  <span className="font-mono text-lg font-bold tracking-wider text-zinc-100">
                    {placaDisplay || '---'}
                  </span>
                </div>
              ) : (
                <div className="flex h-10 items-center gap-2 rounded-lg border border-dashed border-zinc-700 bg-zinc-800/40 px-4">
                  <Car className="h-4 w-4 text-zinc-500" />
                  <span className="text-sm text-zinc-500">Digite a placa</span>
                </div>
              )}
            </div>
            <div className="flex items-center gap-2">
              <span className="rounded-full border border-zinc-700 bg-zinc-800/60 px-3 py-1 text-[10px] font-bold tracking-[0.15em] text-zinc-400">
                VEÍCULO Nº {veiculoNumero}
              </span>
              <button
                type="button"
                onClick={handleClose}
                className="rounded-full p-1 text-zinc-500 transition-colors hover:bg-zinc-800 hover:text-zinc-300"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Identificação do veículo */}
          <div className="mt-6">
            <h3 className="text-base font-semibold text-zinc-100">Identificação do veículo</h3>
            <p className="mt-0.5 text-sm text-zinc-500">
              Comece pela placa e preencha os dados do veículo.
            </p>
          </div>

          <div className="mt-4 grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">PLACA</Label>
              <Input
                value={placa}
                onChange={(e) =>
                  setPlaca(
                    e.target.value
                      .toUpperCase()
                      .replace(/[^A-Z0-9]/g, '')
                      .slice(0, 7),
                  )
                }
                placeholder="AAA0000"
                className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                  errors.placa
                    ? 'border-red-500/60 bg-red-950/20'
                    : placaIsValid
                      ? 'border-green-500/60 bg-green-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                }`}
              />
              {errors.placa && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.placa}
                </p>
              )}
              {placaIsValid && !errors.placa && (
                <p className="text-xs text-green-500">✓ Placa válida</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
                RENAVAM{' '}
                <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
              </Label>
              <Input
                value={renavam}
                onChange={(e) => setRenavam(e.target.value.replace(/\D/g, '').slice(0, 11))}
                placeholder=""
                className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
              />
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">MARCA</Label>
              <Input
                value={marca}
                onChange={(e) => setMarca(e.target.value.slice(0, 20))}
                placeholder="Porsche"
                maxLength={20}
                className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                  errors.marca
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-zinc-700/60 bg-zinc-900/50'
                }`}
              />
              {errors.marca && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.marca}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">MODELO</Label>
              <Input
                value={modelo}
                onChange={(e) => setModelo(e.target.value.slice(0, 20))}
                placeholder="911 Carrera S"
                maxLength={20}
                className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                  errors.modelo
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-zinc-700/60 bg-zinc-900/50'
                }`}
              />
              {errors.modelo && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.modelo}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
                ANO / MODELO{' '}
                <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
              </Label>
              <Input
                value={anoModelo}
                onChange={(e) => {
                  let val = e.target.value.replace(/\D/g, '');
                  if (val.length > 8) val = val.slice(0, 8);
                  if (val.length > 4) {
                    val = `${val.slice(0, 4)} / ${val.slice(4)}`;
                  }
                  setAnoModelo(val);
                }}
                placeholder="2024 / 2025"
                maxLength={11}
                className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                  errors.anoModelo
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-zinc-700/60 bg-zinc-900/50'
                }`}
              />
              {errors.anoModelo && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.anoModelo}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
                CATEGORIA{' '}
                <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
              </Label>
              <Input
                value={categoria}
                onChange={(e) => setCategoria(e.target.value.slice(0, 40))}
                placeholder="Esportivo · Médio"
                className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
              />
            </div>
          </div>

          {/* Visual */}
          <div className="mt-6">
            <h3 className="text-base font-semibold text-zinc-100">Visual</h3>
            <p className="mt-0.5 text-sm text-zinc-500">
              Ajuda o atendente a identificar o carro na pista.
            </p>
          </div>

          <div className="mt-4">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
              COR PREDOMINANTE
            </Label>
            <div className="mt-2 flex flex-wrap gap-2">
              {PRESET_COLORS.map((c) => (
                <button
                  key={c.name}
                  type="button"
                  onClick={() => handleSelectColor(c.name, c.hex)}
                  className={`group flex flex-col items-center gap-1.5 rounded-xl p-2 transition-all ${
                    cor === c.name ? 'bg-zinc-800 ring-2 ring-red-500/60' : 'hover:bg-zinc-800/50'
                  }`}
                  title={c.name}
                >
                  <div
                    className="h-8 w-8 rounded-full border-2 transition-all"
                    style={{
                      backgroundColor: c.hex,
                      borderColor: cor === c.name ? '#EF4444' : 'transparent',
                    }}
                  />
                  <span className="text-[9px] font-medium tracking-wider text-zinc-500">
                    {c.name}
                  </span>
                </button>
              ))}
            </div>

            {cor && (
              <div className="mt-3 flex items-center gap-2">
                <div
                  className="h-6 w-6 rounded-full border border-zinc-700"
                  style={{ backgroundColor: corHex || '#808080' }}
                />
                <div>
                  <p className="text-[9px] tracking-wider text-zinc-500">SELECIONADA</p>
                  <p className="text-sm font-semibold text-zinc-200">
                    {cor} {corHex && <span className="font-normal text-zinc-500">· {corHex}</span>}
                  </p>
                </div>
              </div>
            )}
            {errors.cor && (
              <p className="mt-1 flex items-center gap-1 text-xs text-red-500">
                <X className="h-3 w-3" /> {errors.cor}
              </p>
            )}
          </div>

          {/* Observações de atendimento */}
          <div className="mt-6 space-y-1.5">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
              OBSERVAÇÕES DE ATENDIMENTO
            </Label>
            <textarea
              value={observacoes}
              onChange={(e) => setObservacoes(e.target.value)}
              placeholder="Cuidado com aerofólio traseiro — não apoiar escada."
              rows={3}
              className="w-full resize-none rounded-xl border border-zinc-700/60 bg-zinc-900/50 px-3 py-2.5 text-sm text-zinc-200 placeholder:text-zinc-600 focus:border-zinc-600 focus:outline-none"
              maxLength={500}
            />
            {errors.observacoesAtendimento && (
              <p className="text-xs text-red-500">{errors.observacoesAtendimento}</p>
            )}
          </div>
        </div>

        {/* Footer */}
        <div className="mt-6 flex items-center justify-end gap-3 border-t border-zinc-800/60 px-6 py-4">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            className="h-10 rounded-full border-zinc-700/60 bg-transparent px-6 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
          >
            Cancelar
          </Button>
          <Button
            type="button"
            onClick={handleSave}
            className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
          >
            Salvar e continuar
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
