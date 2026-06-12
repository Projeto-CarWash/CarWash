import { Check } from 'lucide-react';

type StepStatus = 'complete' | 'current' | 'upcoming';

interface Step {
  id: number;
  title: string;
  caption: string;
}

const steps: Step[] = [
  { id: 1, title: 'Identificação', caption: 'CPF · NOME · NASCIMENTO' },
  { id: 2, title: 'Contato & endereço', caption: 'E-MAIL · TELEFONE · CEP' },
  { id: 3, title: 'Veículos', caption: 'VINCULE UM OU MAIS' },
  { id: 4, title: 'Preferências', caption: 'LEMBRETES · FIDELIDADE' },
];

// ── Dados reativos ─────────────────────────────────────────────────────────────
interface StepperProps {
  currentStep?: number;
}

function getStatus(stepId: number, currentStep: number): StepStatus {
  if (stepId < currentStep) return 'complete';
  if (stepId === currentStep) return 'current';
  return 'upcoming';
}

const STEP_TITLES: Record<number, string> = {
  1: 'Identifique-se',
  2: 'Contato & endereço',
  3: 'Crie uma ficha completa.',
  4: 'Quase lá!',
};

export function Stepper({ currentStep = 1 }: StepperProps) {
  return (
    <aside className="sticky top-6 self-start rounded-xl border border-border bg-card p-6">
      <p className="mb-1 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">
        CADASTRO DE CLIENTE
      </p>
      <h2 className="mb-1 text-xl font-semibold text-foreground">
        {STEP_TITLES[currentStep] ?? 'Identifique-se'}
      </h2>
      <p className="mb-6 text-sm text-muted-foreground">
        Preencha todos os dados para não ter erro na hora do agendamento.
      </p>

      <ol className="space-y-1">
        {steps.map((step, index) => {
          const status = getStatus(step.id, currentStep);
          return (
            <li key={step.id} className="relative flex items-start gap-3 py-2 pl-1">
              {index < steps.length - 1 && (
                <span className="absolute left-[0.85rem] top-[2.25rem] bottom-[-0.25rem] w-px bg-muted" />
              )}
              <div
                className={`relative z-10 flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-sm font-semibold transition-all duration-500 ${
                  status === 'complete'
                    ? 'bg-red-600 text-white shadow-lg shadow-red-600/30'
                    : status === 'current'
                      ? 'border-2 border-red-500 bg-transparent text-red-500 shadow-[0_0_0_4px_rgba(239,68,68,0.15)]'
                      : 'border border-border bg-muted text-muted-foreground'
                }`}
              >
                {status === 'complete' ? <Check className="h-3.5 w-3.5" /> : step.id}
              </div>
              <div className="min-w-0">
                <p
                  className={`text-sm font-semibold transition-colors duration-500 ${
                    status === 'complete' || status === 'current'
                      ? 'text-foreground'
                      : 'text-muted-foreground'
                  }`}
                >
                  {step.title}
                </p>
                <p className="text-[10px] tracking-wider text-muted-foreground">{step.caption}</p>
              </div>
            </li>
          );
        })}
      </ol>
    </aside>
  );
}
