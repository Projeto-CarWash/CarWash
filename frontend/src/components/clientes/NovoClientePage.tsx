import { zodResolver } from '@hookform/resolvers/zod';
import { AlertCircle, Check, ChevronRight, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { FormProvider, useForm, useWatch } from 'react-hook-form';

import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { onlyDigits } from '@/lib/masks';
import { clienteSchema } from '@/schemas/clienteSchema';

import { ContatoEnderecoForm } from './ContatoEnderecoForm';
import { IdentificacaoForm } from './IdentificacaoForm';
import { Stepper } from './Stepper';

import type { ClienteFormData } from '@/schemas/clienteSchema';

const API_MESSAGES: Record<number, string> = {
  400: 'Dados do cliente inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para cadastrar clientes.',
  409: 'Já existe cliente cadastrado com este documento.',
  500: 'Não foi possível concluir o cadastro no momento. Tente novamente.',
};

export function NovoClientePage() {
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const form = useForm<ClienteFormData>({
    resolver: zodResolver(clienteSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: {
      cpfCnpj: '',
      dataNascimento: '',
      nome: '',
      telefone: '',
      celular: '',
      email: '',
      cep: '',
      cidade: '',
      rua: '',
      numero: '',
      observacoes: '',
    },
  });

  const cpfCnpj = useWatch({ control: form.control, name: 'cpfCnpj' }) || '';
  const dataNascimento = useWatch({ control: form.control, name: 'dataNascimento' }) || '';
  const nome = useWatch({ control: form.control, name: 'nome' }) || '';

  const cpfDigits = cpfCnpj.replace(/\D/g, '');
  const dateDigits = dataNascimento.replace(/\D/g, '');
  const isCpfFilled = cpfDigits.length === 11 || cpfDigits.length === 14;
  const isDateFilled = dateDigits.length === 8;
  const isNameFilled = nome.trim().length >= 3;
  const isIdentificacaoComplete = isCpfFilled && isDateFilled && isNameFilled;

  const handleSubmit = useCallback(
    async (data: ClienteFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);
      setIsSubmitting(true);

      const payload = {
        nome: data.nome.trim(),
        documento: onlyDigits(data.cpfCnpj),
        telefone: onlyDigits(data.telefone),
        celular: data.celular ? onlyDigits(data.celular) : undefined,
        email: data.email.toLowerCase(),
        cep: onlyDigits(data.cep),
        cidade: data.cidade.trim(),
        rua: data.rua.trim(),
        numero: data.numero.trim(),
        dataNascimento: data.dataNascimento,
        observacoes: data.observacoes?.trim() ?? undefined,
      };

      try {
        const response = await fetch('/api/v1/clientes', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload),
        });

        if (response.status === 201) {
          setSuccessMsg('Cliente cadastrado com sucesso!');
          form.reset();
          setTimeout(() => {
            window.location.href = '/clientes';
          }, 2000);
          return;
        }

        if (response.status === 401) {
          setGlobalError(API_MESSAGES[401]!);
          return;
        }

        if (response.status === 409) {
          setGlobalError(API_MESSAGES[409]!);
          form.setError('cpfCnpj', {
            message: 'Já existe cliente cadastrado com este documento.',
          });
          return;
        }

        const msg = API_MESSAGES[response.status] ?? API_MESSAGES[500]!;
        setGlobalError(msg);

        if (response.status === 400) {
          try {
            const body = (await response.json()) as {
              details?: { field: string; message: string }[];
            };
            body.details?.forEach((detail) => {
              const fieldName = detail.field as keyof ClienteFormData;
              if (fieldName in clienteSchema.shape) {
                form.setError(fieldName, { message: detail.message });
              }
            });
          } catch {
            /* empty */
          }
        }
      } catch {
        setGlobalError(API_MESSAGES[500]!);
      } finally {
        setIsSubmitting(false);
      }
    },
    [form],
  );

  const handleCancel = useCallback(() => {
    form.reset();
    setGlobalError(null);
    setSuccessMsg(null);
  }, [form]);

  return (
    <FormProvider {...form}>
      <form
        onSubmit={form.handleSubmit(handleSubmit)}
        noValidate
        className="grid grid-cols-[minmax(240px,300px)_minmax(0,1fr)] gap-6 px-8"
      >
        <Stepper currentStep={isIdentificacaoComplete ? 2 : 1} />

        <div className="rounded-2xl border border-zinc-800/60 bg-zinc-900/30 p-8">
          {globalError && (
            <div
              role="alert"
              className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" />
              <div className="flex-1">
                <p className="text-sm font-medium text-red-400">{globalError}</p>
              </div>
              <button
                type="button"
                onClick={() => setGlobalError(null)}
                className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          )}

          {successMsg && (
            <div
              role="status"
              className="mb-6 flex items-start gap-3 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3"
            >
              <Check className="mt-0.5 h-4 w-4 shrink-0 text-green-500" />
              <p className="flex-1 text-sm font-medium text-green-400">{successMsg}</p>
              <button
                type="button"
                onClick={() => setSuccessMsg(null)}
                className="shrink-0 text-green-500/60 transition-colors hover:text-green-400"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          )}

          <IdentificacaoForm />

          <div
            className={`overflow-hidden transition-all duration-700 ease-out ${
              isIdentificacaoComplete
                ? 'mt-8 max-h-[1200px] translate-y-0 opacity-100'
                : 'mt-0 max-h-0 translate-y-4 opacity-0'
            }`}
          >
            <Separator className="mb-8 bg-zinc-800/50" />
            <ContatoEnderecoForm />
          </div>

          <div
            className={`flex items-center justify-end gap-3 overflow-hidden transition-all delay-200 duration-500 ease-out ${
              isIdentificacaoComplete
                ? 'mt-8 max-h-20 translate-y-0 opacity-100'
                : 'mt-0 max-h-0 translate-y-4 opacity-0'
            }`}
          >
            <Button
              type="button"
              variant="outline"
              onClick={handleCancel}
              className="h-10 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
            >
              Cancelar
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-50"
            >
              {isSubmitting ? (
                'Salvando...'
              ) : (
                <>
                  Avançar para Veículos
                  <ChevronRight className="ml-1 h-4 w-4" />
                </>
              )}
            </Button>
          </div>
        </div>
      </form>
    </FormProvider>
  );
}
