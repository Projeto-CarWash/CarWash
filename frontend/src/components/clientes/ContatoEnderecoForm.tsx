import { X } from 'lucide-react';
import { Controller, useFormContext } from 'react-hook-form';

import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCelular, maskCep, maskPhone } from '@/lib/masks';

import type { ClienteFormData } from '@/schemas/clienteSchema';

export function ContatoEnderecoForm() {
  const {
    control,
    formState: { errors },
    watch,
  } = useFormContext<ClienteFormData>();

  const obsValue = watch('observacoes') ?? '';

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-zinc-100">Contato & Endereço do Cliente</h3>
        <p className="mt-1 text-sm text-zinc-500">
          Usamos telefone e e-mail para confirmações e lembretes de agendamento.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="phone" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            TELEFONE
          </Label>
          <Controller
            name="telefone"
            control={control}
            render={({ field }) => (
              <>
                <div className="flex items-center">
                  <span className="flex h-10 items-center rounded-l-xl border border-r-0 border-zinc-700/60 bg-zinc-800/40 px-3 text-sm text-zinc-500">
                    +55
                  </span>
                  <Input
                    id="phone"
                    type="text"
                    value={field.value}
                    onChange={(e) => field.onChange(maskPhone(e.target.value))}
                    onBlur={field.onBlur}
                    placeholder="(21) 3333-4444"
                    aria-invalid={!!errors.telefone}
                    aria-describedby={errors.telefone ? 'phone-error' : undefined}
                    className={`h-10 rounded-l-none rounded-r-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                      errors.telefone
                        ? 'border-red-500/60 bg-red-950/20'
                        : 'border-zinc-700/60 bg-zinc-900/50'
                    }`}
                  />
                </div>
                {errors.telefone && (
                  <p id="phone-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.telefone.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="celular" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CELULAR
          </Label>
          <Controller
            name="celular"
            control={control}
            render={({ field }) => (
              <>
                <div className="flex items-center">
                  <span className="flex h-10 items-center rounded-l-xl border border-r-0 border-zinc-700/60 bg-zinc-800/40 px-3 text-sm text-zinc-500">
                    +55
                  </span>
                  <Input
                    id="celular"
                    type="text"
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(maskCelular(e.target.value))}
                    onBlur={field.onBlur}
                    placeholder="(21) 99999-9999"
                    aria-invalid={!!errors.celular}
                    aria-describedby={errors.celular ? 'celular-error' : undefined}
                    className={`h-10 rounded-l-none rounded-r-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                      errors.celular
                        ? 'border-red-500/60 bg-red-950/20'
                        : 'border-zinc-700/60 bg-zinc-900/50'
                    }`}
                  />
                </div>
                {errors.celular && (
                  <p id="celular-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.celular.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="col-span-2 space-y-1.5">
          <Label htmlFor="email" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            E-MAIL
          </Label>
          <Controller
            name="email"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="email"
                  type="email"
                  {...field}
                  placeholder="email@exemplo.com"
                  aria-invalid={!!errors.email}
                  aria-describedby={errors.email ? 'email-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.email
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.email && (
                  <p id="email-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.email.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="cep" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CEP
          </Label>
          <Controller
            name="cep"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="cep"
                  type="text"
                  value={field.value}
                  onChange={(e) => field.onChange(maskCep(e.target.value))}
                  onBlur={field.onBlur}
                  placeholder="00000-000"
                  aria-invalid={!!errors.cep}
                  aria-describedby={errors.cep ? 'cep-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.cep
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.cep && (
                  <p id="cep-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.cep.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="city" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CIDADE / UF
          </Label>
          <Controller
            name="cidade"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="city"
                  type="text"
                  {...field}
                  placeholder="Ex: São Paulo - SP"
                  aria-invalid={!!errors.cidade}
                  aria-describedby={errors.cidade ? 'city-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.cidade
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.cidade && (
                  <p id="city-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.cidade.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="street" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            RUA
          </Label>
          <Controller
            name="rua"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="street"
                  type="text"
                  {...field}
                  placeholder="Ex: Av. Paulista"
                  aria-invalid={!!errors.rua}
                  aria-describedby={errors.rua ? 'street-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.rua
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.rua && (
                  <p id="street-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.rua.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="number" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            NÚMERO / COMPLEMENTO
          </Label>
          <Controller
            name="numero"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="number"
                  type="text"
                  {...field}
                  placeholder="Ex: 1000 - Apto 42"
                  aria-invalid={!!errors.numero}
                  aria-describedby={errors.numero ? 'number-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.numero
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.numero && (
                  <p id="number-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                    <X className="h-3.5 w-3.5" />
                    {errors.numero.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="col-span-2 space-y-1.5">
          <Label
            htmlFor="observacoes"
            className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
          >
            OBSERVAÇÕES{' '}
            <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
          </Label>
          <Controller
            name="observacoes"
            control={control}
            render={({ field }) => (
              <>
                <textarea
                  id="observacoes"
                  value={field.value ?? ''}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  placeholder="Informações adicionais sobre o cliente..."
                  rows={3}
                  maxLength={500}
                  aria-invalid={!!errors.observacoes}
                  aria-describedby={errors.observacoes ? 'obs-error' : undefined}
                  className={`w-full resize-none rounded-xl px-3 py-2.5 text-sm text-zinc-200 outline-none transition-colors placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.observacoes
                      ? 'border border-red-500/60 bg-red-950/20'
                      : 'border border-zinc-700/60 bg-zinc-900/50 focus:border-zinc-600'
                  }`}
                />
                <div className="flex items-center justify-between">
                  {errors.observacoes ? (
                    <p id="obs-error" role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
                      <X className="h-3.5 w-3.5" />
                      {errors.observacoes.message}
                    </p>
                  ) : (
                    <span />
                  )}
                  <span
                    className={`text-[10px] tabular-nums ${
                      obsValue.length > 450 ? 'text-amber-500' : 'text-zinc-600'
                    } ${obsValue.length >= 500 ? 'text-red-500' : ''}`}
                  >
                    {obsValue.length}/500
                  </span>
                </div>
              </>
            )}
          />
        </div>
      </div>
    </div>
  );
}
