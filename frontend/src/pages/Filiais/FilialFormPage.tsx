import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Building2, Check, Loader2, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { Controller, useForm, type Control } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { CelulasAtivasField } from '@/components/filiais/CelulasAtivasField';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useCriarFilial } from '@/hooks/useFilialQueries';
import { maskCep, maskCnpj } from '@/lib/masks';
import { filialSchema, type FilialFormData, type FilialFormInput } from '@/schemas/filialSchema';

import type { ProblemDetails } from '@/types/auth';
import type { CriarFilialRequest } from '@/types/filial';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados da filial inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para cadastrar filiais.',
  409: 'Já existe filial cadastrada com este identificador.',
  500: 'Não foi possível concluir o cadastro da filial no momento. Tente novamente.',
};

/** Mapeia o nome do campo retornado pelo backend (400) para o campo do formulário. */
function mapBackendField(field: string): keyof FilialFormInput | null {
  const map: Record<string, keyof FilialFormInput> = {
    nome: 'nome',
    codigo: 'codigo',
    cnpj: 'cnpj',
    celulasativas: 'celulasAtivas',
    'endereco.cep': 'cep',
    'endereco.logradouro': 'logradouro',
    'endereco.numero': 'numero',
    'endereco.complemento': 'complemento',
    'endereco.bairro': 'bairro',
    'endereco.cidade': 'cidade',
    'endereco.uf': 'uf',
  };
  return map[field.toLowerCase()] ?? null;
}

type FilialControl = Control<FilialFormInput, unknown, FilialFormData>;

/**
 * Formulário de cadastro de filial (RF017/RF018).
 *
 * <p>Validação local com Zod + React Hook Form (mensagens pt-BR) e tratamento
 * completo dos códigos HTTP. Em caso de sucesso, a mutation invalida as listas
 * de filiais — a unidade ativa fica disponível imediatamente para o seletor do
 * agendamento (RF019).</p>
 */
export function FilialFormPage() {
  const navigate = useNavigate();
  const { mutate, isPending } = useCriarFilial();

  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const form = useForm<FilialFormInput, unknown, FilialFormData>({
    resolver: zodResolver(filialSchema),
    mode: 'onChange',
    shouldFocusError: true,
    defaultValues: {
      nome: '',
      codigo: '',
      cnpj: '',
      celulasAtivas: '',
      cep: '',
      logradouro: '',
      numero: '',
      complemento: '',
      bairro: '',
      cidade: '',
      uf: '',
    },
  });

  const onSubmit = useCallback(
    (data: FilialFormData) => {
      if (isPending) return;

      setGlobalError(null);
      setSuccessMsg(null);

      const payload: CriarFilialRequest = {
        nome: data.nome,
        codigo: data.codigo,
        cnpj: data.cnpj.length > 0 ? data.cnpj : null,
        celulasAtivas: data.celulasAtivas,
        endereco: {
          cep: data.cep.replace(/\D/g, ''),
          logradouro: data.logradouro,
          numero: data.numero,
          complemento: data.complemento?.length ? data.complemento : null,
          bairro: data.bairro,
          cidade: data.cidade,
          uf: data.uf,
        },
      };

      mutate(payload, {
        onSuccess: () => {
          setSuccessMsg('Filial cadastrada com sucesso.');
          setTimeout(() => {
            void navigate('/filiais');
          }, 1000);
        },
        onError: (err) => {
          if (!(err instanceof AxiosError) || !err.response) {
            if (
              err instanceof AxiosError &&
              (err.code === 'ECONNABORTED' || err.code === 'ERR_NETWORK')
            ) {
              setGlobalError('Não foi possível contatar o servidor. Verifique sua conexão.');
              return;
            }
            setGlobalError(HTTP_ERROR_MESSAGES[500]!);
            return;
          }

          const status = err.response.status;
          const problem = err.response.data as ProblemDetails | undefined;

          // 409 — identificador duplicado: destaca os campos conflitantes.
          if (status === 409) {
            setGlobalError(HTTP_ERROR_MESSAGES[409]!);
            form.setError('codigo', {
              type: 'manual',
              message: 'Já existe filial cadastrada com este código.',
            });
            form.setError('cnpj', {
              type: 'manual',
              message: 'Já existe filial cadastrada com este CNPJ.',
            });
            form.setFocus('codigo');
            return;
          }

          // 400 — mapeia erros por campo quando disponíveis.
          if (status === 400 && problem?.errors) {
            setGlobalError(HTTP_ERROR_MESSAGES[400]!);
            let firstFocused = false;
            for (const [field, messages] of Object.entries(problem.errors)) {
              const key = mapBackendField(field);
              if (key && messages?.[0]) {
                form.setError(key, { type: 'manual', message: messages[0] });
                if (!firstFocused) {
                  form.setFocus(key);
                  firstFocused = true;
                }
              }
            }
            return;
          }

          if (status === 401) {
            setGlobalError(HTTP_ERROR_MESSAGES[401]!);
            setTimeout(() => {
              void navigate('/login');
            }, 1500);
            return;
          }

          // 403, 500 e demais: preserva o formulário preenchido para nova tentativa.
          setGlobalError(HTTP_ERROR_MESSAGES[status] ?? HTTP_ERROR_MESSAGES[500]!);
        },
      });
    },
    [isPending, mutate, navigate, form],
  );

  const errors = form.formState.errors;
  const isSubmitDisabled = isPending || Object.keys(errors).length > 0 || !form.formState.isValid;

  return (
    <div className="px-8 py-8">
      {successMsg && (
        <div
          role="status"
          aria-live="polite"
          className="fixed right-5 top-5 z-[600] flex items-center gap-3 rounded-lg border border-green-500/30 bg-background px-4 py-3 text-sm text-green-400 shadow-xl shadow-black/60 duration-300 animate-in fade-in slide-in-from-top-5"
        >
          <span className="flex h-5 w-5 items-center justify-center rounded-full bg-green-500/20 text-green-500">
            <Check className="h-3 w-3" />
          </span>
          <span className="font-semibold">{successMsg}</span>
        </div>
      )}

      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <Building2 className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Nova filial</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Cadastre uma nova unidade operacional do CarWash.
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/filiais')}
          disabled={isPending}
          className="h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
          Voltar
        </Button>
      </div>

      <Card className="max-w-3xl border border-border bg-card">
        <CardHeader>
          <CardTitle className="text-lg text-foreground">Dados da filial</CardTitle>
          <CardDescription className="text-muted-foreground">
            A filial é considerada ativa logo após o cadastro e fica disponível para agendamentos.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {globalError && (
            <div
              role="alert"
              aria-live="assertive"
              className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
              <p className="flex-1 text-sm font-medium text-red-400">{globalError}</p>
              <button
                type="button"
                onClick={() => setGlobalError(null)}
                aria-label="Fechar mensagem de erro"
                className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          )}

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="grid grid-cols-1 gap-5 md:grid-cols-2"
            aria-busy={isPending}
            noValidate
          >
            <CampoFilial
              control={form.control}
              name="nome"
              id="filial-nome"
              label="Nome da filial"
              placeholder="Ex: Unidade Centro"
              maxLength={120}
              colSpan2
            />

            <CampoFilial
              control={form.control}
              name="codigo"
              id="filial-codigo"
              label="Código da filial"
              placeholder="Ex: CENTRO01"
              maxLength={20}
              transform={(v) => v.toUpperCase()}
              inputClassName="uppercase"
            />

            <CampoFilial
              control={form.control}
              name="cnpj"
              id="filial-cnpj"
              label="CNPJ"
              labelHint="(opcional)"
              placeholder="00.000.000/0000-00"
              inputMode="numeric"
              display={(v) => maskCnpj(v)}
              transform={(v) => maskCnpj(v)}
            />

            <Controller
              control={form.control}
              name="celulasAtivas"
              render={({ field, fieldState }) => (
                <CelulasAtivasField
                  id="filial-celulas"
                  value={field.value as string | number | undefined}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  inputRef={field.ref}
                  error={fieldState.error?.message}
                  disabled={isPending}
                />
              )}
            />

            <div className="mt-2 md:col-span-2">
              <p className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
                ENDEREÇO
              </p>
            </div>

            <CampoFilial
              control={form.control}
              name="cep"
              id="filial-cep"
              label="CEP"
              placeholder="00000-000"
              inputMode="numeric"
              display={(v) => maskCep(v)}
              transform={(v) => maskCep(v)}
            />

            <CampoFilial
              control={form.control}
              name="uf"
              id="filial-uf"
              label="UF"
              placeholder="Ex: SP"
              maxLength={2}
              transform={(v) => v.replace(/[^a-zA-Z]/g, '').toUpperCase()}
              inputClassName="uppercase"
            />

            <CampoFilial
              control={form.control}
              name="cidade"
              id="filial-cidade"
              label="Cidade"
              placeholder="Ex: São Paulo"
              maxLength={100}
            />

            <CampoFilial
              control={form.control}
              name="bairro"
              id="filial-bairro"
              label="Bairro"
              placeholder="Ex: Centro"
              maxLength={100}
            />

            <CampoFilial
              control={form.control}
              name="logradouro"
              id="filial-logradouro"
              label="Logradouro"
              placeholder="Ex: Av. Principal"
              maxLength={150}
            />

            <CampoFilial
              control={form.control}
              name="numero"
              id="filial-numero"
              label="Número"
              placeholder="Ex: 1000"
              maxLength={20}
            />

            <CampoFilial
              control={form.control}
              name="complemento"
              id="filial-complemento"
              label="Complemento"
              labelHint="(opcional)"
              placeholder="Ex: Bloco B, Sala 2"
              maxLength={100}
              colSpan2
            />

            <div className="mt-4 flex items-center justify-end gap-3 md:col-span-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate('/filiais')}
                disabled={isPending}
                className="h-10 rounded-full border-border bg-transparent px-5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={isSubmitDisabled}
                className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
              >
                {isPending ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                    Salvando...
                  </>
                ) : (
                  'Salvar filial'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

interface CampoFilialProps {
  control: FilialControl;
  name: keyof FilialFormInput;
  id: string;
  label: string;
  labelHint?: string;
  placeholder?: string;
  type?: string;
  inputMode?: React.ComponentProps<typeof Input>['inputMode'];
  maxLength?: number;
  hint?: string;
  colSpan2?: boolean;
  inputClassName?: string;
  /** Transforma o valor digitado antes de gravar no formulário (ex.: máscara, uppercase). */
  transform?: (value: string) => string;
  /** Formata o valor para exibição (ex.: aplica máscara sem alterar o que é gravado). */
  display?: (value: string) => string;
}

/** Campo de texto controlado, com rótulo, mensagem de erro e acessibilidade (RNF008). */
function CampoFilial({
  control,
  name,
  id,
  label,
  labelHint,
  placeholder,
  type = 'text',
  inputMode,
  maxLength,
  hint,
  colSpan2 = false,
  inputClassName = '',
  transform,
  display,
}: CampoFilialProps) {
  const errorId = `${id}-error`;
  return (
    <div className={`flex flex-col gap-2 ${colSpan2 ? 'md:col-span-2' : ''}`}>
      <Label htmlFor={id} className="text-foreground">
        {label} {labelHint && <span className="text-muted-foreground">{labelHint}</span>}
      </Label>
      <Controller
        control={control}
        name={name}
        render={({ field, fieldState }) => {
          const raw = (field.value ?? '') as string;
          return (
            <>
              <Input
                id={id}
                type={type}
                inputMode={inputMode}
                placeholder={placeholder}
                maxLength={maxLength}
                value={display ? display(raw) : raw}
                onChange={(e) =>
                  field.onChange(transform ? transform(e.target.value) : e.target.value)
                }
                onBlur={field.onBlur}
                ref={field.ref}
                aria-invalid={!!fieldState.error}
                aria-describedby={fieldState.error ? errorId : hint ? `${id}-hint` : undefined}
                className={`h-10 rounded-lg border px-3 text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                  fieldState.error
                    ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                    : 'border-border bg-background focus-visible:border-ring'
                } ${inputClassName}`}
              />
              {fieldState.error ? (
                <p id={errorId} role="alert" className="text-xs text-red-400">
                  {fieldState.error.message}
                </p>
              ) : (
                hint && (
                  <p id={`${id}-hint`} className="text-xs text-muted-foreground">
                    {hint}
                  </p>
                )
              )}
            </>
          );
        }}
      />
    </div>
  );
}
