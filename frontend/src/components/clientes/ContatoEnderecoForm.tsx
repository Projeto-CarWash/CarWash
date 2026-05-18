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
  } = useFormContext<ClienteFormData>();

  return (
    <div>
      <div className="mb-5">
        <h3 className="text-xl font-semibold text-zinc-100">Contato &amp; Endereço do Cliente</h3>
        <p className="mt-1 text-sm text-zinc-500">
          Celular é obrigatório. Telefone fixo e e-mail são opcionais.
        </p>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="celular" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CELULAR <span className="text-red-500">*</span>
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
                    aria-required="true"
                    className={`h-10 rounded-l-none rounded-r-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                      errors.celular
                        ? 'border-red-500/60 bg-red-950/20'
                        : 'border-zinc-700/60 bg-zinc-900/50'
                    }`}
                  />
                </div>
                {errors.celular && (
                  <p
                    id="celular-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.celular.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="phone" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            TELEFONE FIXO{' '}
            <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
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
                    value={field.value ?? ''}
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
                  <p
                    id="phone-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.telefone.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="col-span-2 space-y-1.5">
          <Label htmlFor="email" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            E-MAIL <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
          </Label>
          <Controller
            name="email"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="email"
                  type="email"
                  value={field.value ?? ''}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
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
                  <p
                    id="email-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
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
            CEP <span className="text-red-500">*</span>
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
                  <p
                    id="cep-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.cep.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="uf" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            UF <span className="text-red-500">*</span>
          </Label>
          <Controller
            name="uf"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="uf"
                  type="text"
                  value={field.value}
                  onChange={(e) => field.onChange(e.target.value.toUpperCase().slice(0, 2))}
                  onBlur={field.onBlur}
                  placeholder="SP"
                  maxLength={2}
                  aria-invalid={!!errors.uf}
                  aria-describedby={errors.uf ? 'uf-error' : undefined}
                  className={`h-10 rounded-xl text-sm uppercase text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.uf
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.uf && (
                  <p
                    id="uf-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.uf.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="cidade" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            CIDADE <span className="text-red-500">*</span>
          </Label>
          <Controller
            name="cidade"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="cidade"
                  type="text"
                  name={field.name}
                  value={field.value ?? ''}
                  onChange={(e) =>
                    field.onChange(
                      e.target.value.replace(/[^a-zA-ZáàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s-]/g, ''),
                    )
                  }
                  onBlur={field.onBlur}
                  placeholder="Ex: São Paulo"
                  aria-invalid={!!errors.cidade}
                  aria-describedby={errors.cidade ? 'cidade-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.cidade
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.cidade && (
                  <p
                    id="cidade-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.cidade.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="bairro" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            BAIRRO <span className="text-red-500">*</span>
          </Label>
          <Controller
            name="bairro"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="bairro"
                  type="text"
                  {...field}
                  placeholder="Ex: Bela Vista"
                  aria-invalid={!!errors.bairro}
                  aria-describedby={errors.bairro ? 'bairro-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.bairro
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.bairro && (
                  <p
                    id="bairro-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.bairro.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="col-span-2 space-y-1.5">
          <Label
            htmlFor="logradouro"
            className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
          >
            LOGRADOURO <span className="text-red-500">*</span>
          </Label>
          <Controller
            name="logradouro"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="logradouro"
                  type="text"
                  {...field}
                  placeholder="Ex: Av. Paulista"
                  aria-invalid={!!errors.logradouro}
                  aria-describedby={errors.logradouro ? 'logradouro-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.logradouro
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.logradouro && (
                  <p
                    id="logradouro-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.logradouro.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="numero" className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
            NÚMERO <span className="text-red-500">*</span>
          </Label>
          <Controller
            name="numero"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="numero"
                  type="text"
                  name={field.name}
                  value={field.value ?? ''}
                  onChange={(e) => field.onChange(e.target.value)}
                  onBlur={field.onBlur}
                  placeholder="Ex: 1000"
                  aria-invalid={!!errors.numero}
                  aria-describedby={errors.numero ? 'numero-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.numero
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.numero && (
                  <p
                    id="numero-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.numero.message}
                  </p>
                )}
              </>
            )}
          />
        </div>

        <div className="space-y-1.5">
          <Label
            htmlFor="complemento"
            className="text-[10px] font-bold tracking-[0.2em] text-zinc-500"
          >
            COMPLEMENTO{' '}
            <span className="font-normal tracking-normal text-zinc-600">(opcional)</span>
          </Label>
          <Controller
            name="complemento"
            control={control}
            render={({ field }) => (
              <>
                <Input
                  id="complemento"
                  type="text"
                  value={field.value ?? ''}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  placeholder="Ex: Apto 42"
                  aria-invalid={!!errors.complemento}
                  aria-describedby={errors.complemento ? 'complemento-error' : undefined}
                  className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                    errors.complemento
                      ? 'border-red-500/60 bg-red-950/20'
                      : 'border-zinc-700/60 bg-zinc-900/50'
                  }`}
                />
                {errors.complemento && (
                  <p
                    id="complemento-error"
                    role="alert"
                    className="flex items-center gap-1.5 text-xs text-red-500"
                  >
                    <X className="h-3.5 w-3.5" />
                    {errors.complemento.message}
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
