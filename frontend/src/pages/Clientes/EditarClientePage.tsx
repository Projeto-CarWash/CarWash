import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Check, Loader2, UserCog, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCelular, maskCep, maskDate, maskPhone } from '@/lib/masks';
import { editarClienteSchema, type EditarClienteFormData } from '@/schemas/clienteSchema';
import { clienteService, isoParaDdmmaaaa } from '@/services/clienteService';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados do cliente inválidos. Verifique os campos e tente novamente.',
  401: 'Sessão expirada. Faça login novamente.',
  403: 'Você não possui permissão para editar clientes.',
  404: 'Cliente não encontrado.',
  409: 'Já existe outro cliente cadastrado com este e-mail.',
  500: 'Não foi possível concluir a atualização no momento. Tente novamente.',
};

function mapBackendFieldToFormField(field: string): keyof EditarClienteFormData | null {
  const lower = field.toLowerCase();
  const map: Record<string, keyof EditarClienteFormData> = {
    nome: 'nome',
    datanascimento: 'dataNascimento',
    celular: 'celular',
    telefone: 'telefone',
    email: 'email',
    'endereco.cep': 'cep',
    'endereco.logradouro': 'logradouro',
    'endereco.numero': 'numero',
    'endereco.complemento': 'complemento',
    'endereco.bairro': 'bairro',
    'endereco.cidade': 'cidade',
    'endereco.uf': 'uf',
  };
  return map[lower] ?? null;
}

function formatarDocumento(cpf?: string, cnpj?: string): string {
  if (cpf?.length === 11) {
    return `${cpf.slice(0, 3)}.${cpf.slice(3, 6)}.${cpf.slice(6, 9)}-${cpf.slice(9)}`;
  }
  if (cnpj?.length === 14) {
    return `${cnpj.slice(0, 2)}.${cnpj.slice(2, 5)}.${cnpj.slice(5, 8)}/${cnpj.slice(8, 12)}-${cnpj.slice(12)}`;
  }
  return cpf ?? cnpj ?? '—';
}

export function EditarClientePage() {
  const { id = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [carregando, setCarregando] = useState(true);
  const [documento, setDocumento] = useState('—');
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const form = useForm<EditarClienteFormData>({
    resolver: zodResolver(editarClienteSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: {
      nome: '',
      dataNascimento: '',
      telefone: '',
      celular: '',
      email: '',
      cep: '',
      logradouro: '',
      numero: '',
      complemento: '',
      bairro: '',
      cidade: '',
      uf: '',
    },
  });

  // ── Carrega o cliente e pré-preenche o formulário ──────────────────────────
  useEffect(() => {
    if (!id) return;
    let cancelado = false;

    const carregar = async () => {
      setCarregando(true);
      setGlobalError(null);
      try {
        const c = await clienteService.obterPorId(id);
        if (cancelado) return;
        setDocumento(formatarDocumento(c.cpf, c.cnpj));
        form.reset({
          nome: c.nome,
          dataNascimento: isoParaDdmmaaaa(c.dataNascimento),
          telefone: c.telefone ? maskPhone(c.telefone) : '',
          celular: maskCelular(c.celular),
          email: c.email ?? '',
          cep: maskCep(c.endereco.cep),
          logradouro: c.endereco.logradouro,
          numero: c.endereco.numero,
          complemento: c.endereco.complemento ?? '',
          bairro: c.endereco.bairro,
          cidade: c.endereco.cidade,
          uf: c.endereco.uf,
        });
      } catch (error) {
        if (cancelado) return;
        if (error instanceof AxiosError && error.response) {
          const status = error.response.status;
          if (status === 401) {
            void navigate('/login', { replace: true });
            return;
          }
          setGlobalError(HTTP_ERROR_MESSAGES[status] ?? HTTP_ERROR_MESSAGES[500]!);
          if (status === 404) {
            setTimeout(() => void navigate('/clientes'), 1500);
          }
        } else {
          setGlobalError(HTTP_ERROR_MESSAGES[500]!);
        }
      } finally {
        if (!cancelado) setCarregando(false);
      }
    };

    void carregar();
    return () => {
      cancelado = true;
    };
  }, [id, form, navigate]);

  // ── Submit ─────────────────────────────────────────────────────────────────
  const onSubmit = useCallback(
    async (data: EditarClienteFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);
      try {
        await clienteService.atualizar(id, data);
        setSuccessMsg('Cliente atualizado com sucesso! Redirecionando…');
        setTimeout(() => {
          void navigate(`/clientes/${id}`, { replace: true });
        }, 900);
      } catch (error) {
        if (!(error instanceof AxiosError) || !error.response) {
          setGlobalError(HTTP_ERROR_MESSAGES[500]!);
          return;
        }
        const status = error.response.status;
        const problem = error.response.data as ProblemDetails | undefined;

        if (status === 401) {
          setGlobalError(HTTP_ERROR_MESSAGES[401]!);
          setTimeout(() => void navigate('/login', { replace: true }), 900);
          return;
        }

        if (status === 409) {
          setGlobalError(HTTP_ERROR_MESSAGES[409]!);
          form.setError('email', { message: 'Já existe outro cliente com este e-mail.' });
          form.setFocus('email');
          return;
        }

        if (status === 400 && problem?.errors) {
          setGlobalError(problem.title ?? HTTP_ERROR_MESSAGES[400]!);
          let firstFocused = false;
          for (const [field, messages] of Object.entries(problem.errors)) {
            const key = mapBackendFieldToFormField(field);
            if (key && messages?.[0]) {
              form.setError(key, { message: messages[0] });
              if (!firstFocused) {
                form.setFocus(key);
                firstFocused = true;
              }
            }
          }
          return;
        }

        if (status === 404) {
          setGlobalError(HTTP_ERROR_MESSAGES[404]!);
          setTimeout(() => void navigate('/clientes'), 1500);
          return;
        }

        setGlobalError(HTTP_ERROR_MESSAGES[status] ?? HTTP_ERROR_MESSAGES[500]!);
      }
    },
    [form, id, navigate],
  );

  const isSubmitting = form.formState.isSubmitting;
  const {
    control,
    formState: { errors },
  } = form;

  if (carregando) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" />
        Carregando cliente…
      </div>
    );
  }

  return (
    <div className="px-8 py-8">
      {successMsg && (
        <div
          role="status"
          aria-live="polite"
          className="fixed right-5 top-5 z-[600] flex items-center gap-3 rounded-lg border border-green-500/30 bg-background px-4 py-3 text-sm text-green-400 shadow-xl shadow-black/60 animate-in fade-in slide-in-from-top-5 duration-300"
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
            <UserCog className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar cliente</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Atualize os dados cadastrais do cliente. CPF/CNPJ e veículos não são alterados aqui.
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate(`/clientes/${id}`)}
          disabled={isSubmitting}
          className="h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" /> Voltar
        </Button>
      </div>

      <Card className="max-w-4xl border border-border bg-card">
        <CardHeader>
          <CardTitle className="text-lg text-foreground">Dados do cliente</CardTitle>
          <CardDescription className="text-muted-foreground">
            Identificação, contato e endereço. Campos marcados com{' '}
            <span className="text-red-500">*</span> são obrigatórios.
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
            onSubmit={(e) => {
              void form.handleSubmit(onSubmit)(e);
            }}
            noValidate
            aria-busy={isSubmitting}
            className="grid grid-cols-2 gap-4"
          >
            {/* CPF/CNPJ — somente leitura (não editável via PUT) */}
            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
                CPF / CNPJ
              </Label>
              <Input
                type="text"
                value={documento}
                readOnly
                disabled
                aria-label="CPF ou CNPJ (não editável)"
                className="h-10 cursor-not-allowed rounded-xl border-border bg-background text-sm text-muted-foreground"
              />
              <p className="text-[11px] text-muted-foreground">Documento não pode ser alterado.</p>
            </div>

            <Field
              control={control}
              name="dataNascimento"
              label="DATA DE NASCIMENTO"
              required
              error={errors.dataNascimento?.message}
              placeholder="DD/MM/AAAA"
              mask={maskDate}
            />

            <div className="col-span-2">
              <Field
                control={control}
                name="nome"
                label="NOME COMPLETO / RAZÃO SOCIAL"
                required
                error={errors.nome?.message}
                placeholder="Ex: Helena Quintanilha Freitas"
              />
            </div>

            <Field
              control={control}
              name="celular"
              label="CELULAR"
              required
              error={errors.celular?.message}
              placeholder="(21) 99999-9999"
              mask={maskCelular}
            />

            <Field
              control={control}
              name="telefone"
              label="TELEFONE FIXO"
              optional
              error={errors.telefone?.message}
              placeholder="(21) 3333-4444"
              mask={maskPhone}
            />

            <div className="col-span-2">
              <Field
                control={control}
                name="email"
                label="E-MAIL"
                optional
                type="email"
                error={errors.email?.message}
                placeholder="email@exemplo.com"
              />
            </div>

            <Field
              control={control}
              name="cep"
              label="CEP"
              required
              error={errors.cep?.message}
              placeholder="00000-000"
              mask={maskCep}
            />

            <Field
              control={control}
              name="uf"
              label="UF"
              required
              error={errors.uf?.message}
              placeholder="SP"
              maxLength={2}
              mask={(v) =>
                v
                  .toUpperCase()
                  .replace(/[^A-Z]/g, '')
                  .slice(0, 2)
              }
            />

            <Field
              control={control}
              name="cidade"
              label="CIDADE"
              required
              error={errors.cidade?.message}
              placeholder="Ex: São Paulo"
            />

            <Field
              control={control}
              name="bairro"
              label="BAIRRO"
              required
              error={errors.bairro?.message}
              placeholder="Ex: Bela Vista"
            />

            <div className="col-span-2">
              <Field
                control={control}
                name="logradouro"
                label="LOGRADOURO"
                required
                error={errors.logradouro?.message}
                placeholder="Ex: Av. Paulista"
              />
            </div>

            <Field
              control={control}
              name="numero"
              label="NÚMERO"
              required
              error={errors.numero?.message}
              placeholder="Ex: 1000"
            />

            <Field
              control={control}
              name="complemento"
              label="COMPLEMENTO"
              optional
              error={errors.complemento?.message}
              placeholder="Ex: Apto 42"
            />

            <div className="col-span-2 mt-4 flex items-center justify-end gap-3">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate(`/clientes/${id}`)}
                disabled={isSubmitting}
                className="h-10 rounded-full border-border bg-transparent px-5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={isSubmitting}
                className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                    Salvando…
                  </>
                ) : (
                  'Salvar alterações'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

interface FieldProps {
  control: ReturnType<typeof useForm<EditarClienteFormData>>['control'];
  name: keyof EditarClienteFormData;
  label: string;
  placeholder?: string;
  type?: string;
  required?: boolean;
  optional?: boolean;
  maxLength?: number;
  error?: string;
  mask?: (value: string) => string;
}

function Field({
  control,
  name,
  label,
  placeholder,
  type = 'text',
  required,
  optional,
  maxLength,
  error,
  mask,
}: FieldProps) {
  const errorId = `${name}-error`;
  return (
    <div className="space-y-1.5">
      <Label htmlFor={name} className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
        {label} {required && <span className="text-red-500">*</span>}
        {optional && <span className="font-normal tracking-normal text-muted-foreground">(opcional)</span>}
      </Label>
      <Controller
        control={control}
        name={name}
        render={({ field }) => (
          <Input
            id={name}
            type={type}
            value={field.value ?? ''}
            onChange={(e) => field.onChange(mask ? mask(e.target.value) : e.target.value)}
            onBlur={field.onBlur}
            ref={field.ref}
            maxLength={maxLength}
            placeholder={placeholder}
            aria-invalid={!!error}
            aria-required={required}
            aria-describedby={error ? errorId : undefined}
            className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
              error
                ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                : 'border-border bg-card focus-visible:border-ring'
            }`}
          />
        )}
      />
      {error && (
        <p id={errorId} role="alert" className="flex items-center gap-1.5 text-xs text-red-500">
          <X className="h-3.5 w-3.5" />
          {error}
        </p>
      )}
    </div>
  );
}
