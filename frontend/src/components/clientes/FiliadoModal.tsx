import { X } from 'lucide-react';
import { useCallback, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Dialog, DialogContent, DialogDescription, DialogTitle } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { maskCpfCnpj, maskCelular } from '@/lib/masks';
import { isValidCpf, isValidCnpj } from '@/lib/validators';
import { filiadoSchema, type FiliadoFormData } from '@/schemas/clienteSchema';

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

    const result = filiadoSchema.safeParse(data);

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
      setErrors({ cpf: 'Este CPF/CNPJ já foi adicionado entre os filiados.' });
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

  const docDigits = cpf.replace(/\D/g, '');
  const docIsComplete = docDigits.length === 11 || docDigits.length === 14;
  const docIsValid =
    (docDigits.length === 11 && isValidCpf(docDigits)) ||
    (docDigits.length === 14 && isValidCnpj(docDigits));
  const docIsUnique = !existingCpfs.includes(docDigits);

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) handleClose();
      }}
    >
      <DialogContent
        showCloseButton={false}
        className="!max-w-md overflow-y-auto max-h-[90vh] rounded-2xl border border-border bg-card p-0 text-foreground shadow-2xl sm:!max-w-md"
      >
        <DialogTitle className="sr-only">Adicionar filiado</DialogTitle>
        <DialogDescription className="sr-only">
          Formulário para adicionar uma pessoa filiada ao cliente.
        </DialogDescription>

        <div className="p-6 pb-0">
          {/* Header */}
          <div className="flex items-start justify-between">
            <div>
              <h3 className="text-base font-semibold text-foreground">Filiação</h3>
              <p className="mt-0.5 text-sm text-muted-foreground">
                Dados pessoais do filiado. O CPF/CNPJ é único por pessoa vinculada.
              </p>
            </div>
            <button
              type="button"
              onClick={handleClose}
              className="rounded-full p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground"
            >
              <X className="h-4 w-4" />
            </button>
          </div>

          {/* CPF */}
          <div className="mt-5 space-y-1.5">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
              CPF/CNPJ
            </Label>
            <Input
              value={cpf}
              onChange={(e) => setCpf(maskCpfCnpj(e.target.value))}
              placeholder="123.567.891-23 ou 12.345.678/0001-90"
              className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                errors.cpf || (docIsComplete && !docIsValid)
                  ? 'border-red-500/60 bg-red-950/20'
                  : docIsComplete && docIsValid && docIsUnique && !errors.cpf
                    ? 'border-green-500/60 bg-green-950/20'
                    : 'border-border bg-card'
              }`}
            />
            {errors.cpf && (
              <p className="flex items-center gap-1 text-xs text-red-500">
                <X className="h-3 w-3" /> {errors.cpf}
              </p>
            )}
            {!errors.cpf && docIsComplete && !docIsValid && (
              <p className="flex items-center gap-1 text-xs text-red-500">
                <X className="h-3 w-3" /> Documento inválido.
              </p>
            )}
            {docIsComplete && docIsValid && docIsUnique && !errors.cpf && (
              <p className="text-xs text-green-500">✓ Documento válido</p>
            )}
          </div>

          {/* Nome completo */}
          <div className="mt-4 space-y-1.5">
            <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
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
              className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                errors.nome ? 'border-red-500/60 bg-red-950/20' : 'border-border bg-card'
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
            <h3 className="text-base font-semibold text-foreground">Contato</h3>
            <p className="mt-0.5 text-sm text-muted-foreground">
              Usamos telefone e e-mail para confirmações e lembretes de agendamento. Deixe vazio
              caso não o filiado não queira receber notificações.
            </p>
          </div>

          <div className="mt-4 grid grid-cols-2 gap-4">
            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
                TELEFONE
              </Label>
              <div className="flex items-center">
                <span className="flex h-10 items-center rounded-l-xl border border-r-0 border-border bg-muted px-3 text-sm text-muted-foreground">
                  +55
                </span>
                <Input
                  value={telefone}
                  onChange={(e) => setTelefone(maskCelular(e.target.value))}
                  placeholder="(21) 99999-9999"
                  className="h-10 rounded-l-none rounded-r-xl border-border bg-card text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0"
                />
              </div>
            </div>

            <div className="space-y-1.5">
              <Label className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
                E-MAIL
              </Label>
              <Input
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="email@exemplo.com"
                className={`h-10 rounded-xl text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
                  errors.email ? 'border-red-500/60 bg-red-950/20' : 'border-border bg-card'
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
        <div className="mt-6 flex items-center justify-end gap-3 border-t border-border px-6 py-4">
          <Button
            type="button"
            variant="outline"
            onClick={handleClose}
            className="h-10 rounded-full border-border bg-transparent px-6 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
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
