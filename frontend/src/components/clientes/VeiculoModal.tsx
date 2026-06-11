import { Car, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { PRESET_COLORS } from '@/lib/colors';
import { type VeiculoLocalFormData, veiculoItemSchema } from '@/schemas/clienteSchema';

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
  const [fabricante, setFabricante] = useState('');
  const [modelo, setModelo] = useState('');
  const [ano, setAno] = useState('');
  const [corHex, setCorHex] = useState('');
  const [corNomeDisplay, setCorNomeDisplay] = useState('');
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

  // Validate ano in real-time
  useEffect(() => {
    if (!ano) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setErrors((prev) => {
        if (prev.ano) {
          const { ano: _ano, ...rest } = prev;
          return rest;
        }
        return prev;
      });
      return;
    }

    const parsed = parseInt(ano, 10);
    if (isNaN(parsed) || parsed < 1930 || parsed > 2027) {
      setErrors((prev) => {
        if (prev.ano !== 'O ano deve ser entre 1930 e 2027.') {
          return { ...prev, ano: 'O ano deve ser entre 1930 e 2027.' };
        }
        return prev;
      });
    } else {
      setErrors((prev) => {
        if (prev.ano) {
          const { ano: _ano, ...rest } = prev;
          return rest;
        }
        return prev;
      });
    }
  }, [ano]);

  const resetForm = useCallback(() => {
    setPlaca('');
    setFabricante('');
    setModelo('');
    setAno('');
    setCorHex('');
    setCorNomeDisplay('');
    setErrors({});
  }, []);

  const handleClose = useCallback(() => {
    resetForm();
    onOpenChange(false);
  }, [resetForm, onOpenChange]);

  const handleSelectColor = useCallback((name: string, hex: string) => {
    setCorHex(hex);
    setCorNomeDisplay(name);
  }, []);

  const handleSave = useCallback(() => {
    const data: Record<string, unknown> = {
      placa,
      fabricante,
      modelo,
      cor: corHex,
    };

    // Only include ano if provided
    if (ano) {
      data.ano = parseInt(ano, 10);
    }

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
      fabricante: result.data.fabricante,
      modelo: result.data.modelo,
      cor: result.data.cor,
      ano: result.data.ano,
    });
    resetForm();
    onOpenChange(false);
  }, [placa, fabricante, modelo, ano, corHex, existingPlacas, onSave, onOpenChange, resetForm]);

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
        className="!max-w-lg overflow-y-auto max-h-[90vh] rounded-2xl border border-border bg-card p-0 text-foreground shadow-2xl sm:!max-w-lg"
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
                <div className="flex items-center gap-1.5 rounded-lg border-2 border-border bg-muted px-3 py-1.5">
                  <div className="flex h-5 items-center rounded bg-blue-700 px-1.5">
                    <span className="text-[8px] font-bold leading-none text-foreground">BR · RJ</span>
                  </div>
                  <span className="font-mono text-lg font-bold tracking-wider text-foreground">
                    {placaDisplay || '---'}
                  </span>
                </div>
              ) : (
                <div className="flex h-10 items-center gap-2 rounded-lg border border-dashed border-border bg-muted px-4">
                  <Car className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm text-muted-foreground">Digite a placa</span>
                </div>
              )}
            </div>
            <div className="flex items-center gap-2">
              <span className="rounded-full border border-border bg-muted px-3 py-1 text-[10px] font-bold tracking-[0.15em] text-muted-foreground">
                VEÍCULO Nº {veiculoNumero}
              </span>
              <button
                type="button"
                onClick={handleClose}
                className="rounded-full p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {/* Identificação do veículo */}
          <div className="mt-6">
            <h3 className="text-base font-semibold text-foreground">Identificação do veículo</h3>
            <p className="mt-0.5 text-sm text-muted-foreground">
              Comece pela placa e preencha os dados do veículo.
            </p>
          </div>

          <div className="mt-4 grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">PLACA</Label>
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
                className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                  errors.placa
                    ? 'border-red-500/60 bg-red-950/20'
                    : placaIsValid
                      ? 'border-green-500/60 bg-green-950/20'
                      : 'border-border bg-card'
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
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
                ANO <span className="font-normal tracking-normal text-muted-foreground">(opcional)</span>
              </Label>
              <Input
                value={ano}
                onChange={(e) => {
                  const val = e.target.value.replace(/\D/g, '').slice(0, 4);
                  setAno(val);
                }}
                placeholder="2024"
                maxLength={4}
                className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                  errors.ano
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-border bg-card'
                }`}
              />
              {errors.ano && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.ano}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
                FABRICANTE
              </Label>
              <Input
                value={fabricante}
                onChange={(e) => setFabricante(e.target.value.slice(0, 80))}
                placeholder="Porsche"
                maxLength={80}
                className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                  errors.fabricante
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-border bg-card'
                }`}
              />
              {errors.fabricante && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.fabricante}
                </p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">MODELO</Label>
              <Input
                value={modelo}
                onChange={(e) => setModelo(e.target.value.slice(0, 80))}
                placeholder="911 Carrera S"
                maxLength={80}
                className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                  errors.modelo
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-border bg-card'
                }`}
              />
              {errors.modelo && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.modelo}
                </p>
              )}
            </div>
          </div>

          {/* Visual */}
          <div className="mt-6">
            <h3 className="text-base font-semibold text-foreground">Visual</h3>
            <p className="mt-0.5 text-sm text-muted-foreground">
              Ajuda o atendente a identificar o carro na pista.
            </p>
          </div>

          <div className="mt-4">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
              COR PREDOMINANTE
            </Label>
            <div className="mt-2 flex flex-wrap gap-2">
              {PRESET_COLORS.map((c) => (
                <button
                  key={c.name}
                  type="button"
                  onClick={() => handleSelectColor(c.name, c.hex)}
                  className={`group flex flex-col items-center gap-1.5 rounded-xl p-2 transition-all ${
                    corHex === c.hex ? 'bg-muted ring-2 ring-red-500/60' : 'hover:bg-accent'
                  }`}
                  title={c.name}
                >
                  <div
                    className="h-8 w-8 rounded-full border-2 transition-all"
                    style={{
                      backgroundColor: c.hex,
                      borderColor: corHex === c.hex ? '#EF4444' : 'transparent',
                    }}
                  />
                  <span className="text-[9px] font-medium tracking-wider text-muted-foreground">
                    {c.name}
                  </span>
                </button>
              ))}
            </div>

            {corHex && (
              <div className="mt-3 flex items-center gap-2">
                <div
                  className="h-6 w-6 rounded-full border border-border"
                  style={{ backgroundColor: corHex }}
                />
                <div>
                  <p className="text-[9px] tracking-wider text-muted-foreground">SELECIONADA</p>
                  <p className="text-sm font-semibold text-foreground">
                    {corNomeDisplay} <span className="font-normal text-muted-foreground">· {corHex}</span>
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
        </div>

        {/* Footer */}
        <div className="mt-6 flex items-center justify-end gap-3 border-t border-border px-6 py-4">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            className="h-10 rounded-full border-border bg-transparent px-6 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
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
