import { X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { z } from 'zod';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCpfCnpj, maskCelular } from '@/lib/masks';
import { isValidCpf } from '@/lib/validators';

import type { FiliadoFormData } from '@/schemas/clienteSchema';

const modalFiliadoSchema = z.object({
  cpf: z
    .string()
    .min(1, 'CPF é obrigatório.')
    .refine((val) => val.replace(/\D/g, '').length === 11, {
      message: 'CPF deve conter 11 dígitos.',
    })
    .superRefine((val, ctx) => {
      const d = val.replace(/\D/g, '');
      if (d.length === 11 && !isValidCpf(d)) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'CPF inválido.' });
      }
    }),
  nome: z
    .string()
    .min(1, 'Nome é obrigatório.')
    .refine((val) => val.trim().length >= 3, {
      message: 'Nome deve ter no mínimo 3 caracteres.',
    }),
  telefone: z.string().optional(),
  email: z
    .string()
    .optional()
    .refine(
      (val) => {
        if (!val || val.trim().length === 0) return true;
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
      },
      { message: 'E-mail inválido.' },
    ),
});

interface FiliadoModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  existingCpfs: string[];
  onSave: (filiado: FiliadoFormData) => void;
}

export function FiliadoModal({ open, onOpenChange, existingCpfs, onSave }: FiliadoModalProps) {
  const [cpf, setCpf] = useState('');
  const [nome, setNome] = useState('');
  const [telefone, setTelefone] = useState('');
  const [email, setEmail] = useState('');
  const [errors, setErrors] = useState<Record<string, string>>({});

  const resetForm = useCallback(() => {
    setCpf('');
    setNome('');
    setTelefone('');
    setEmail('');
    setErrors({});
  }, []);

  const handleClose = useCallback(() => {
    resetForm();
    onOpenChange(false);
  }, [resetForm, onOpenChange]);

  const handleSave = useCallback(() => {
    const data = {
      cpf,
      nome,
      telefone: telefone || undefined,
      email: email || undefined,
    };

    const result = modalFiliadoSchema.safeParse(data);

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

    const cpfDigits = cpf.replace(/\D/g, '');
    if (existingCpfs.includes(cpfDigits)) {
      setErrors({ cpf: 'Este CPF já foi adicionado entre os filiados.' });
      return;
    }

    setErrors({});
    onSave({
      cpf: result.data.cpf,
      nome: result.data.nome,
      telefone: result.data.telefone,
      email: result.data.email,
    });
    resetForm();
    onOpenChange(false);
  }, [cpf, nome, telefone, email, existingCpfs, onSave, onOpenChange, resetForm]);

  const cpfDigits = cpf.replace(/\D/g, '');
  const cpfIsComplete = cpfDigits.length === 11;
  const cpfIsUnique = !existingCpfs.includes(cpfDigits);

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent
        showCloseButton={false}
        className="!max-w-md overflow-y-auto max-h-[90vh] rounded-2xl border border-zinc-800/60 bg-zinc-900 p-0 text-zinc-100 shadow-2xl sm:!max-w-md"
      >
        <DialogTitle className="sr-only">Adicionar filiado</DialogTitle>
        <DialogDescription className="sr-only">
          Formulário para adicionar uma pessoa filiada ao cliente.
        </DialogDescription>

        <div className="p-6 pb-0">
          {/* Header */}
          <div className="flex items-start justify-between">
            <div>
              <h3 className="text-base font-semibold text-zinc-100">Filiação</h3>
              <p className="mt-0.5 text-sm text-zinc-500">
                Dados pessoais do filiado. O CPF é único por pessoa vinculada.
              </p>
            </div>
            <button
              type="button"
              onClick={handleClose}
              className="rounded-full p-1 text-zinc-500 transition-colors hover:bg-zinc-800 hover:text-zinc-300"
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          {/* CPF */}
          <div className="mt-5 space-y-1.5">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">CPF</Label>
            <Input
              value={cpf}
              onChange={(e) => setCpf(maskCpfCnpj(e.target.value))}
              placeholder="123.567.891-23"
              className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                errors.cpf
                  ? 'border-red-500/60 bg-red-950/20'
                  : cpfIsComplete && cpfIsUnique && !errors.cpf
                    ? 'border-green-500/60 bg-green-950/20'
                    : 'border-zinc-700/60 bg-zinc-900/50'
              }`}
            />
            {errors.cpf && (
              <p className="flex items-center gap-1 text-xs text-red-500">
                <X className="h-3 w-3" /> {errors.cpf}
              </p>
            )}
            {cpfIsComplete && cpfIsUnique && !errors.cpf && (
              <p className="text-xs text-green-500">✓ CPF válido</p>
            )}
          </div>

          {/* Nome completo */}
          <div className="mt-4 space-y-1.5">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
              NOME COMPLETO
            </Label>
            <Input
              value={nome}
              onChange={(e) => {
                const sanitized = e.target.value.replace(
                  /[^a-zA-Z0-9áàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s.&/'-]/g,
                  '',
                );
                setNome(sanitized);
              }}
              placeholder="Helena Quintanilha Freitas"
              className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                errors.nome
                  ? 'border-red-500/60 bg-red-950/20'
                  : 'border-zinc-700/60 bg-zinc-900/50'
              }`}
            />
            {errors.nome && (
              <p className="flex items-center gap-1 text-xs text-red-500">
                <X className="h-3 w-3" /> {errors.nome}
              </p>
            )}
          </div>

          {/* Contato */}
          <div className="mt-6">
            <h3 className="text-base font-semibold text-zinc-100">Contato</h3>
            <p className="mt-0.5 text-sm text-zinc-500">
              Usamos telefone e e-mail para confirmações e lembretes de agendamento. Deixe vazio
              caso não o filiado não queira receber notificações.
            </p>
          </div>

          <div className="mt-4 grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">
                TELEFONE
              </Label>
              <div className="flex items-center">
                <span className="flex h-10 items-center rounded-l-xl border border-r-0 border-zinc-700/60 bg-zinc-800/40 px-3 text-sm text-zinc-500">
                  +55
                </span>
                <Input
                  value={telefone}
                  onChange={(e) => setTelefone(maskCelular(e.target.value))}
                  placeholder="(21) 99999-9999"
                  className="h-10 rounded-l-none rounded-r-xl border-zinc-700/60 bg-zinc-900/50 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">E-MAIL</Label>
              <Input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="email@exemplo.com"
                className={`h-10 rounded-xl text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0 ${
                  errors.email
                    ? 'border-red-500/60 bg-red-950/20'
                    : 'border-zinc-700/60 bg-zinc-900/50'
                }`}
              />
              {errors.email && (
                <p className="flex items-center gap-1 text-xs text-red-500">
                  <X className="h-3 w-3" /> {errors.email}
                </p>
              )}
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="mt-6 flex items-center justify-end gap-3 border-t border-zinc-800/60 px-6 py-4">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            className="h-10 rounded-full border-zinc-700/60 bg-transparent px-6 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
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
