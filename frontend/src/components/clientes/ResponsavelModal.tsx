import axios from 'axios';
import { Loader2, X } from 'lucide-react';
import { useCallback, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCelular, maskCpfCnpj } from '@/lib/masks';
import { isValidCnpj, isValidCpf } from '@/lib/validators';
import { responsavelSchema } from '@/schemas/responsavelSchema';
import { responsavelService } from '@/services/responsavelService';
import { GRAUS_VINCULO, VINCULO_LABELS } from '@/types/responsavel';

import type { GrauVinculo, Responsavel } from '@/types/responsavel';

interface ResponsavelModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  clienteId: string;
  onSuccess: (newResponsavel: Responsavel) => void;
}

export function ResponsavelModal({
  open,
  onOpenChange,
  clienteId,
  onSuccess,
}: ResponsavelModalProps) {
  const [nome, setNome] = useState('');
  const [documento, setDocumento] = useState('');
  const [telefone, setTelefone] = useState('');
  const [email, setEmail] = useState('');
  const [grauVinculo, setGrauVinculo] = useState<GrauVinculo | ''>('');

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [globalError, setGlobalError] = useState<string | null>(null);

  const resetForm = useCallback(() => {
    setNome('');
    setDocumento('');
    setTelefone('');
    setEmail('');
    setGrauVinculo('');
    setErrors({});
    setGlobalError(null);
  }, []);

  const handleClose = useCallback(() => {
    resetForm();
    onOpenChange(false);
  }, [resetForm, onOpenChange]);

  const handleSave = useCallback(async () => {
    if (isSubmitting) return;

    setGlobalError(null);
    setErrors({});

    const rawData = {
      nome,
      documento,
      telefone: telefone === '' ? undefined : (telefone ?? undefined),
      email: email === '' ? undefined : (email ?? undefined),
      grauVinculo: grauVinculo === '' ? undefined : (grauVinculo ?? undefined),
    };

    const parseResult = responsavelSchema.safeParse(rawData);

    if (!parseResult.success) {
      const errMap: Record<string, string> = {};
      parseResult.error.issues.forEach((issue) => {
        if (issue.path[0]) {
          errMap[issue.path[0] as string] = issue.message;
        }
      });
      setErrors(errMap);
      return;
    }

    setIsSubmitting(true);

    try {
      const payload = {
        nome: parseResult.data.nome,
        documento: parseResult.data.documento.replace(/\D/g, ''),
        telefone: parseResult.data.telefone
          ? parseResult.data.telefone.replace(/\D/g, '')
          : null,
        email: parseResult.data.email ?? null,
        grauVinculo: parseResult.data.grauVinculo,
      };

      const newResponsavel = await responsavelService.criar(clienteId, payload);
      onSuccess(newResponsavel);
      handleClose();
    } catch (error: unknown) {
      if (axios.isAxiosError<{ message?: string; title?: string }>(error)) {
        const status = error.response?.status;
        const responseData = error.response?.data;
        const msg = responseData?.message ?? responseData?.title ?? null;

        if (status === 409) {
          // 409 Conflict: Already exists with this document
          setErrors((prev) => ({
            ...prev,
            documento: msg ?? 'Já existe responsável cadastrado com este documento.',
          }));
        } else if (status === 404) {
          setGlobalError('Cliente titular não encontrado.');
        } else if (status === 400) {
          setGlobalError(
            msg ?? 'Dados de cadastro inválidos. Verifique os campos e tente novamente.',
          );
        } else {
          setGlobalError(msg ?? 'Erro ao salvar responsável. Tente novamente mais tarde.');
        }
      } else {
        setGlobalError('Ocorreu um erro inesperado.');
      }
    } finally {
      setIsSubmitting(false);
    }
  }, [
    nome,
    documento,
    telefone,
    email,
    grauVinculo,
    clienteId,
    onSuccess,
    handleClose,
    isSubmitting,
  ]);

  const docDigits = documento.replace(/\D/g, '');
  const docIsComplete = docDigits.length === 11 || docDigits.length === 14;
  const docIsValid =
    (docDigits.length === 11 && isValidCpf(docDigits)) ||
    (docDigits.length === 14 && isValidCnpj(docDigits));

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen && !isSubmitting) handleClose();
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="!max-w-md overflow-y-auto max-h-[90vh] rounded-2xl border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900 p-0 text-foreground dark:text-zinc-100 shadow-2xl sm:!max-w-md"
      >
        <DialogTitle className="sr-only">Cadastrar responsável</DialogTitle>
        <DialogDescription className="sr-only">
          Formulário para adicionar um responsável vinculado a este cliente.
        </DialogDescription>

        <div className="p-6 pb-0 space-y-5">
          {/* Header */}
          <div className="flex items-start justify-between">
            <div>
              <h3 className="text-base font-semibold text-foreground dark:text-zinc-100">
                Novo Responsável
              </h3>
              <p className="mt-0.5 text-sm text-muted-foreground">
                Cadastre um responsável legal ou pessoa autorizada para este cliente.
              </p>
            </div>
            <button
              type="button"
              disabled={isSubmitting}
              onClick={handleClose}
              className="rounded-full p-1 text-muted-foreground dark:text-zinc-500 transition-colors hover:bg-accent dark:hover:bg-muted hover:text-foreground dark:hover:text-foreground disabled:opacity-50"
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          {/* Global Error Alert */}
          {globalError && (
            <div
              role="alert"
              className="flex items-start gap-2.5 rounded-xl border border-red-200 dark:border-red-500/30 bg-red-50 dark:bg-red-950/20 px-4 py-3 text-sm text-red-600 dark:text-red-400"
            >
              <span className="font-semibold">Erro:</span>
              <span className="flex-1">{globalError}</span>
            </div>
          )}

          {/* Nome completo */}
          <div className="space-y-1.5">
            <Label
              htmlFor="resp-nome"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
            >
              NOME COMPLETO *
            </Label>
            <Input
              id="resp-nome"
              value={nome}
              disabled={isSubmitting}
              onChange={(e) => {
                const sanitized = e.target.value.replace(
                  /[^a-zA-Z0-9áàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s.&/'-]/g,
                  '',
                );
                setNome(sanitized);
              }}
              placeholder="Digite o nome completo"
              className={`h-10 rounded-xl text-sm border bg-muted dark:bg-zinc-950/40 text-foreground dark:text-zinc-200 placeholder:text-muted-foreground dark:placeholder:text-muted-foreground focus-visible:ring-0 ${
                errors.nome
                  ? 'border-red-500/60 bg-red-950/20'
                  : 'border-border dark:border-zinc-700/60'
              }`}
            />
            {errors.nome && (
              <p className="text-xs text-red-500 flex items-center gap-1">✕ {errors.nome}</p>
            )}
          </div>

          {/* CPF / CNPJ */}
          <div className="space-y-1.5">
            <Label
              htmlFor="resp-doc"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
            >
              DOCUMENTO (CPF/CNPJ) *
            </Label>
            <Input
              id="resp-doc"
              value={documento}
              disabled={isSubmitting}
              onChange={(e) => setDocumento(maskCpfCnpj(e.target.value))}
              placeholder="000.000.000-00 ou 00.000.000/0000-00"
              className={`h-10 rounded-xl text-sm border bg-muted dark:bg-zinc-950/40 text-foreground dark:text-zinc-200 placeholder:text-muted-foreground dark:placeholder:text-muted-foreground focus-visible:ring-0 ${
                errors.documento || (docIsComplete && !docIsValid)
                  ? 'border-red-500/60 bg-red-950/20'
                  : docIsComplete && docIsValid
                    ? 'border-green-500/60 bg-green-950/20'
                    : 'border-border dark:border-zinc-700/60'
              }`}
            />
            {errors.documento && (
              <p className="text-xs text-red-500 flex items-center gap-1">✕ {errors.documento}</p>
            )}
            {!errors.documento && docIsComplete && !docIsValid && (
              <p className="text-xs text-red-500 flex items-center gap-1">✕ Documento inválido.</p>
            )}
            {docIsComplete && docIsValid && !errors.documento && (
              <p className="text-xs text-green-500">✓ Documento válido</p>
            )}
          </div>

          {/* Grau de Vínculo */}
          <div className="space-y-1.5">
            <Label
              htmlFor="resp-vinculo"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
            >
              GRAU DE VÍNCULO *
            </Label>
            <select
              id="resp-vinculo"
              value={grauVinculo}
              disabled={isSubmitting}
              onChange={(e) => setGrauVinculo(e.target.value as GrauVinculo)}
              className={`h-10 w-full cursor-pointer appearance-none rounded-xl border bg-muted dark:bg-zinc-950/40 px-3 text-sm text-foreground dark:text-zinc-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 dark:[color-scheme:dark] ${
                errors.grauVinculo
                  ? 'border-red-500/60 bg-red-950/20'
                  : 'border-border dark:border-zinc-700/60'
              }`}
            >
              <option value="" disabled className="text-muted-foreground dark:text-zinc-600">
                Selecione o vínculo
              </option>
              {GRAUS_VINCULO.map((grau) => (
                <option key={grau} value={grau}>
                  {VINCULO_LABELS[grau]}
                </option>
              ))}
            </select>
            {errors.grauVinculo && (
              <p className="text-xs text-red-500 flex items-center gap-1">✕ {errors.grauVinculo}</p>
            )}
          </div>

          {/* Contato Section */}
          <div className="pt-2">
            <h4 className="text-sm font-semibold text-foreground dark:text-zinc-200">
              Informações de Contato
            </h4>
            <p className="mt-0.5 text-xs text-muted-foreground">
              Campos opcionais para notificação direta do responsável.
            </p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label
                htmlFor="resp-telefone"
                className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
              >
                TELEFONE
              </Label>
              <div className="flex items-center">
                <span className="flex h-10 items-center rounded-l-xl border border-r-0 border-border dark:border-zinc-700/60 bg-muted dark:bg-zinc-800/40 px-3 text-sm text-muted-foreground">
                  +55
                </span>
                <Input
                  id="resp-telefone"
                  value={telefone}
                  disabled={isSubmitting}
                  onChange={(e) => setTelefone(maskCelular(e.target.value))}
                  placeholder="(21) 99999-9999"
                  className="h-10 rounded-l-none rounded-r-xl border border-border dark:border-zinc-700/60 bg-muted dark:bg-zinc-950/40 text-sm text-foreground dark:text-zinc-200 placeholder:text-muted-foreground dark:placeholder:text-foreground focus-visible:ring-0"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label
                htmlFor="resp-email"
                className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground"
              >
                E-MAIL
              </Label>
              <Input
                id="resp-email"
                value={email}
                disabled={isSubmitting}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="email@exemplo.com"
                className={`h-10 rounded-xl text-sm border bg-muted dark:bg-zinc-950/40 text-foreground dark:text-zinc-200 placeholder:text-muted-foreground dark:placeholder:text-muted-foreground focus-visible:ring-0 ${
                  errors.email
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-border dark:border-zinc-700/60'
                }`}
              />
              {errors.email && (
                <p className="text-xs text-red-500 flex items-center gap-1">✕ {errors.email}</p>
              )}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="mt-6 flex items-center justify-end gap-3 border-t border-border dark:border-zinc-800/60 px-6 py-4">
          <Button
            type="button"
            variant="outline"
            disabled={isSubmitting}
            onClick={handleClose}
            className="h-10 rounded-full border-border dark:border-zinc-700/60 bg-transparent px-6 text-sm text-muted-foreground hover:bg-accent dark:hover:bg-muted hover:text-foreground dark:hover:text-foreground"
          >
            Cancelar
          </Button>
          <Button
            type="button"
            disabled={isSubmitting}
            onClick={handleSave}
            className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="mr-1.5 h-4 w-4 animate-spin" />
                Salvando...
              </>
            ) : (
              'Salvar responsável'
            )}
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}
