import { Check, X } from 'lucide-react';
import { useCallback, useRef, useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCpfCnpj, maskDate } from '@/lib/masks';

import type { ClienteFormData } from '@/schemas/clienteSchema';

export function IdentificacaoForm() {
  const {
    watch,
    control,
    formState: { errors },
  } = useFormContext<ClienteFormData>();

  const [isDocLocked, setIsDocLocked] = useState(false);
  const cpfInputRef = useRef<HTMLInputElement>(null);

  const cpfError = errors.cpfCnpj;
  const dateError = errors.dataNascimento;
  const nomeError = errors.nome;

  const handleDocBlur = useCallback((value: string, rhfOnBlur: () => void) => {
    rhfOnBlur();
    if (value.replace(/\D/g, '').length > 0) {
      setIsDocLocked(true);
    }
  }, []);

  const handleEditClick = useCallback(() => {
    setIsDocLocked(false);
    requestAnimationFrame(() => {
      cpfInputRef.current?.focus();
    });
  }, []);

  const cpfCnpjValue = watch('cpfCnpj');
  const [prevCpfValue, setPrevCpfValue] = useState(cpfCnpjValue);

  if (cpfCnpjValue !== prevCpfValue) {
    setPrevCpfValue(cpfCnpjValue);
    if (!cpfCnpjValue) {
      setIsDocLocked(false);
    }
  }

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-zinc-100">Identificação</h3>
        <p className="mt-1 text-sm text-zinc-500">
          Dados pessoais do titular. O CPF/CNPJ é único por cliente.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="cpf" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CPF / CNPJ
          </Label>
          <Controller
            name="cpfCnpj"
            control={control}
            render={({ field }) => {
              const digits = field.value.replace(/\D/g, '');
              const isValid = (digits.length === 11 || digits.length === 14) && !cpfError;

              return (
                <>
                  <div className="flex items-center gap-2">
                    <Input
                      ref={cpfInputRef}
                      id="cpf"
                      type="text"
                      value={field.value}
                      onChange={(e) => field.onChange(maskCpfCnpj(e.target.value))}
                      onBlur={() => handleDocBlur(field.value, field.onBlur)}
                      disabled={isDocLocked}
                      placeholder="000.000.000-00"
                      aria-invalid={!!cpfError}
                      aria-describedby={cpfError ? 'cpf-error' : undefined}
                      className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                        cpfError
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-900/50 focus-visible:border-zinc-600'
                      } ${isDocLocked ? 'opacity-70' : ''}`}
                    />
                    {isDocLocked && (
                      <button
                        type="button"
                        onClick={handleEditClick}
                        className="shrink-0 rounded-full border border-zinc-700/60 bg-zinc-800/50 px-3 py-1.5 text-[10px] font-bold tracking-[0.15em] text-zinc-300 transition-colors hover:bg-zinc-700/50 hover:text-zinc-100"
                      >
                        EDITAR
                      </button>
                    )}
                  </div>
                  {isValid && isDocLocked && (
                    <p className="flex items-center gap-1.5 text-xs text-green-500">
                      <Check className="h-3.5 w-3.5" />
                      Documento válido e único na base
                    </p>
                  )}
                  {cpfError && (
                    <p
                      id="cpf-error"
                      role="alert"
                      className="flex items-center gap-1.5 text-xs text-red-500"
                    >
                      <X className="h-3.5 w-3.5" />
                      {cpfError.message}
                    </p>
                  )}
                </>
              );
            }}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="birth" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            DATA DE NASCIMENTO
          </Label>
          <Controller
            name="dataNascimento"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="birth"
                  type="text"
                  value={field.value}
                  onChange={(e) => field.onChange(maskDate(e.target.value))}
                  onBlur={field.onBlur}
                  placeholder="DD/MM/AAAA"
                  aria-invalid={!!dateError}
                  aria-describedby={dateError ? 'birth-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    dateError
                      ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                      : 'border-zinc-700/60 bg-zinc-900/50 focus-visible:border-zinc-600'
                  }`}
                />
                {dateError && (
                  <p
                    id="birth-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {dateError.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="col-span-2 space-y-1.5">
          <Label htmlFor="name" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            NOME COMPLETO / RAZÃO SOCIAL
          </Label>
          <Controller
            name="nome"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="name"
                  type="text"
                  value={field.value}
                  onChange={(e) => {
                    const onlyLetters = e.target.value.replace(/[^a-zA-ZáàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s]/g, '');
                    field.onChange(onlyLetters);
                  }}
                  onBlur={field.onBlur}
                  placeholder="Ex: Helena Quintanilha Freitas"
                  aria-invalid={!!nomeError}
                  aria-describedby={nomeError ? 'name-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    nomeError
                      ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                      : 'border-zinc-700/60 bg-zinc-900/50 focus-visible:border-zinc-600'
                  }`}
                />
                {nomeError && (
                  <p
                    id="name-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {nomeError.message}
                  </p>
                )}
              </>
            )}
          />
        </div>
      </div>
    </div>
  );
}
