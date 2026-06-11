import { X } from 'lucide-react';
import { Controller, useFormContext } from 'react-hook-form';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCpfCnpj, maskDate } from '@/lib/masks';

import type { ClienteFormData } from '@/schemas/clienteSchema';

export function IdentificacaoForm() {
  const {
    control,
    formState: { errors },
  } = useFormContext<ClienteFormData>();

  const cpfError = errors.cpfCnpj;
  const dateError = errors.dataNascimento;
  const nomeError = errors.nome;

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-foreground">Identificação</h3>
        <p className="mt-1 text-sm text-muted-foreground">
          Dados pessoais do titular. O CPF/CNPJ é único por cliente.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="cpf" className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
            CPF / CNPJ
          </Label>
          <Controller
            name="cpfCnpj"
            control={control}
            render={({ field, fieldState }) => {
              const digits = field.value.replace(/\D/g, '');
              const isComplete = digits.length === 11 || digits.length === 14;
              const isValid = fieldState.isTouched && !fieldState.invalid && isComplete;

              return (
                <>
                  <Input
                    id="cpf"
                    type="text"
                    value={field.value}
                    onChange={(e) => field.onChange(maskCpfCnpj(e.target.value))}
                    onBlur={field.onBlur}
                    placeholder="000.000.000-00"
                    aria-invalid={!!cpfError}
                    aria-describedby={cpfError ? 'cpf-error' : undefined}
                    className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                      cpfError
                        ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                        : isValid
                          ? 'border-green-500/60 bg-green-950/20 focus-visible:border-green-500'
                          : 'border-border bg-card focus-visible:border-ring'
                    }`}
                  />
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
          <Label htmlFor="birth" className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
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
                  className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                    dateError
                      ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                      : 'border-border bg-card focus-visible:border-ring'
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
          <Label htmlFor="name" className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
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
                    const sanitizedName = e.target.value.replace(
                      /[^a-zA-Z0-9áàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s.&/'-]/g,
                      '',
                    );
                    field.onChange(sanitizedName);
                  }}
                  onBlur={field.onBlur}
                  placeholder="Ex: Helena Quintanilha Freitas"
                  aria-invalid={!!nomeError}
                  aria-describedby={nomeError ? 'name-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                    nomeError
                      ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                      : 'border-border bg-card focus-visible:border-ring'
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
