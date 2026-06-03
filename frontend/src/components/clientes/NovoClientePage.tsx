import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, Check, X } from 'lucide-react';
import { useCallback, useRef, useState } from 'react';
import { FormProvider, useForm, useWatch } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { clienteSchema } from '@/schemas/clienteSchema';
import { clienteService } from '@/services/clienteService';

import { ContatoEnderecoForm } from './ContatoEnderecoForm';
import { IdentificacaoForm } from './IdentificacaoForm';
import { PageHeader } from './PageHeader';
import { PreferenciasFidelidadeForm } from './PreferenciasFidelidadeForm';
import { Stepper } from './Stepper';
import { VeiculosClienteForm } from './VeiculosClienteForm';

import type { ClienteFormData } from '@/schemas/clienteSchema';
import type { ProblemDetails } from '@/types/auth';

const DRAFT_STORAGE_KEY = 'carwash-draft-novo-cliente';

const API_MESSAGES: Record<number, string> = {
  400: 'Dados do cliente inválidos. Verifique os campos e tente novamente.',
  401: 'Sessão expirada. Faça login novamente.',
  403: 'Você não possui permissão para cadastrar clientes.',
  409: 'Já existe cliente cadastrado com este documento.',
  500: 'Não foi possível concluir o cadastro no momento. Tente novamente.',
};

export function NovoClientePage() {
  const navigate = useNavigate();
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isResettingRef = useRef(false);

  const getDefaultValues = useCallback(
    (): ClienteFormData => ({
      cpfCnpj: '',
      dataNascimento: '',
      nome: '',
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
      veiculos: [],
      lembretes: [],
      canaisPreferenciais: [],
      observacoesGerais: '',
      filiados: [],
    }),
    [],
  );

  const loadDraft = useCallback((): ClienteFormData | null => {
    try {
      const raw = localStorage.getItem(DRAFT_STORAGE_KEY);
      if (!raw) return null;
      return JSON.parse(raw) as ClienteFormData;
    } catch {
      localStorage.removeItem(DRAFT_STORAGE_KEY);
      return null;
    }
  }, []);

  const form = useForm<ClienteFormData>({
    resolver: zodResolver(clienteSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: loadDraft() ?? getDefaultValues(),
  });

  // Safe reset that prevents rapid consecutive calls from corrupting RHF state
  const safeReset = useCallback(() => {
    if (isResettingRef.current) return;
    isResettingRef.current = true;
    const defaults = getDefaultValues();
    form.reset(defaults, { keepErrors: false, keepDirty: false, keepTouched: false });
    // Allow next reset only after RHF finishes its internal state reconciliation
    requestAnimationFrame(() => {
      isResettingRef.current = false;
    });
  }, [form, getDefaultValues]);

  // ── Reactive watches for sidebar + step validation ─────────────────────────
  const cpfCnpj = useWatch({ control: form.control, name: 'cpfCnpj' }) || '';
  const dataNascimento = useWatch({ control: form.control, name: 'dataNascimento' }) || '';
  const nome = useWatch({ control: form.control, name: 'nome' }) || '';
  const celular = useWatch({ control: form.control, name: 'celular' }) || '';
  const cep = useWatch({ control: form.control, name: 'cep' }) || '';
  const veiculos = useWatch({ control: form.control, name: 'veiculos' }) || [];

  const logradouro = useWatch({ control: form.control, name: 'logradouro' }) || '';
  const numero = useWatch({ control: form.control, name: 'numero' }) || '';
  const bairro = useWatch({ control: form.control, name: 'bairro' }) || '';
  const cidade = useWatch({ control: form.control, name: 'cidade' }) || '';
  const uf = useWatch({ control: form.control, name: 'uf' }) || '';

  // ── Step 1 validation ──────────────────────────────────────────────────────
  const docDigits = cpfCnpj.replace(/\D/g, '');
  const dateDigits = dataNascimento.replace(/\D/g, '');
  const isDocFilled = docDigits.length === 11 || docDigits.length === 14;
  const isDateFilled = dateDigits.length === 8;
  const isNameFilled = nome.trim().length >= 3;

  const { errors } = form.formState;
  const isDocValid = isDocFilled && !errors.cpfCnpj;
  const isDateValid = isDateFilled && !errors.dataNascimento;
  const isNameValid = isNameFilled && !errors.nome;
  const isIdentificacaoComplete = isDocValid && isDateValid && isNameValid;

  // ── Step 2 validation (contato & endereço) ─────────────────────────────────
  const celularDigits = celular.replace(/\D/g, '');
  const cepDigits = cep.replace(/\D/g, '');
  const isContatoComplete =
    celularDigits.length === 11 &&
    !errors.celular &&
    cepDigits.length === 8 &&
    !errors.cep &&
    logradouro.trim().length > 0 &&
    !errors.logradouro &&
    numero.trim().length > 0 &&
    !errors.numero &&
    bairro.trim().length > 0 &&
    !errors.bairro &&
    cidade.trim().length > 0 &&
    !errors.cidade &&
    uf.trim().length > 0 &&
    !errors.uf;

  // ── Step 3 validation ──────────────────────────────────────────────────────
  const isVeiculosComplete = veiculos.length > 0;

  let currentStep = 1;
  if (isIdentificacaoComplete && isContatoComplete && isVeiculosComplete) currentStep = 4;
  else if (isIdentificacaoComplete && isContatoComplete) currentStep = 3;
  else if (isIdentificacaoComplete) currentStep = 2;

  // ── Submit ─────────────────────────────────────────────────────────────────
  const handleSubmit = useCallback(
    async (data: ClienteFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);
      setIsSubmitting(true);

      try {
        const resp = await clienteService.criar(data);
        setSuccessMsg('Cliente cadastrado com sucesso! Redirecionando…');
        localStorage.removeItem(DRAFT_STORAGE_KEY);
        safeReset();
        setTimeout(() => {
          void navigate(`/clientes/${resp.id}`, { replace: true });
        }, 800);
      } catch (error) {
        if (!(error instanceof AxiosError) || !error.response) {
          setGlobalError(API_MESSAGES[500]!);
          return;
        }

        const status = error.response.status;
        const problem = error.response.data as ProblemDetails | undefined;

        if (status === 401) {
          setGlobalError(API_MESSAGES[401]!);
          setTimeout(() => {
            void navigate('/login', { replace: true });
          }, 800);
          return;
        }

        if (status === 409) {
          // 409 pode vir de documento duplicado ou placa duplicada
          const detail = problem?.detail ?? '';
          const isPlaca =
            detail.toLowerCase().includes('placa') ||
            detail.toLowerCase().includes('veículo') ||
            detail.toLowerCase().includes('veiculo');
          if (isPlaca) {
            setGlobalError(
              'Já existe veículo cadastrado com esta placa. Verifique a lista de veículos.',
            );
          } else {
            setGlobalError(API_MESSAGES[409]!);
            form.setError('cpfCnpj', {
              message: 'Já existe cliente cadastrado com este documento.',
            });
          }
          return;
        }

        if (status === 400 && problem?.errors) {
          setGlobalError(problem.title ?? API_MESSAGES[400]!);
          for (const [field, messages] of Object.entries(problem.errors)) {
            const key = mapBackendFieldToFormField(field);
            if (key && messages?.[0]) {
              form.setError(key, { message: messages[0] });
            }
          }
          return;
        }

        setGlobalError(API_MESSAGES[status] ?? API_MESSAGES[500]!);
      } finally {
        setIsSubmitting(false);
      }
    },
    [form, navigate, safeReset],
  );

  const handleClearForm = useCallback(() => {
    safeReset();
    setGlobalError(null);
    setSuccessMsg(null);
    localStorage.removeItem(DRAFT_STORAGE_KEY);
  }, [safeReset]);

  const handleSaveDraft = useCallback(() => {
    const currentValues = form.getValues();
    try {
      localStorage.setItem(DRAFT_STORAGE_KEY, JSON.stringify(currentValues));
    } catch (e) {
      console.warn('Failed to save draft:', e);
    }
  }, [form]);

  return (
    <>
      <PageHeader onClearForm={handleClearForm} onSaveDraft={handleSaveDraft} step={currentStep} />
      <FormProvider {...form}>
        <form
          onSubmit={(e) => {
            void form.handleSubmit(handleSubmit)(e);
          }}
          noValidate
          className="grid grid-cols-[minmax(240px,300px)_minmax(0,1fr)] gap-6 px-8"
        >
          <Stepper currentStep={currentStep} />

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
                  aria-label="Fechar mensagem de erro"
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
                  aria-label="Fechar mensagem de sucesso"
                >
                  <X className="h-4 w-4" />
                </button>
              </div>
            )}
            <IdentificacaoForm />

            <div
              className={`overflow-hidden transition-all duration-700 ease-out ${
                isIdentificacaoComplete
                  ? 'mt-8 max-h-[3000px] translate-y-0 opacity-100'
                  : 'mt-0 max-h-0 translate-y-4 opacity-0'
              }`}
            >
              <div className="border-t border-zinc-800/60 pt-8">
                <ContatoEnderecoForm />
              </div>
            </div>

            {/* ── Step 3: Veículos ───────────────────────────────────────────── */}
            <div
              className={`overflow-hidden transition-all duration-700 ease-out ${
                isIdentificacaoComplete && isContatoComplete
                  ? 'mt-8 max-h-[3000px] translate-y-0 opacity-100'
                  : 'mt-0 max-h-0 translate-y-4 opacity-0'
              }`}
            >
              <div className="border-t border-zinc-800/60 pt-8">
                <VeiculosClienteForm />
              </div>
            </div>

            {/* ── Step 4: Preferências & Fidelidade ──────────────────────────── */}
            <div
              className={`overflow-hidden transition-all duration-700 ease-out ${
                isIdentificacaoComplete && isContatoComplete && isVeiculosComplete
                  ? 'mt-8 max-h-[3000px] translate-y-0 opacity-100'
                  : 'mt-0 max-h-0 translate-y-4 opacity-0'
              }`}
            >
              <div className="border-t border-zinc-800/60 pt-8">
                <PreferenciasFidelidadeForm isSubmitting={isSubmitting} />
              </div>
            </div>
          </div>
        </form>
      </FormProvider>
    </>
  );
}

function mapBackendFieldToFormField(field: string): keyof ClienteFormData | null {
  const lower = field.toLowerCase();
  const map: Record<string, keyof ClienteFormData> = {
    nome: 'nome',
    datanascimento: 'dataNascimento',
    cpf: 'cpfCnpj',
    cnpj: 'cpfCnpj',
    documento: 'cpfCnpj',
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
