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

interface StepperProps {
  currentStep?: number;
}

function getStatus(stepId: number, currentStep: number): StepStatus {
  if (stepId < currentStep) return 'complete';
  if (stepId === currentStep) return 'current';
  return 'upcoming';
}

export function Stepper({ currentStep = 1 }: StepperProps) {
  return (
    <aside className="rounded-xl border border-zinc-800/60 bg-zinc-900/40 p-6">
      <p className="mb-1 text-[10px] font-bold tracking-[0.2em] text-zinc-500">
        CADASTRO DE CLIENTE
      </p>
      <h2 className="mb-1 text-xl font-semibold text-zinc-100">Identifique-se</h2>
      <p className="mb-6 text-sm text-zinc-500">
        Preencha todos os dados para não ter erro na hora do agendamento.
      </p>

      <ol className="space-y-1">
        {steps.map((step, index) => {
          const status = getStatus(step.id, currentStep);
          return (
            <li key={step.id} className="relative flex items-start gap-3 py-2 pl-1">
              {index < steps.length - 1 && (
                <span className="absolute left-[0.85rem] top-[2.25rem] bottom-[-0.25rem] w-px bg-zinc-800" />
              )}
              <div
                className={`relative z-10 flex h-7 w-7 shrink-0 items-center justify-center rounded-full text-sm font-semibold transition-all duration-500 ${
                  status === 'complete'
                    ? 'bg-red-600 text-white shadow-lg shadow-red-600/30'
                    : status === 'current'
                      ? 'border-2 border-red-500 bg-transparent text-red-500 shadow-[0_0_0_4px_rgba(239,68,68,0.15)]'
                      : 'border border-zinc-700/80 bg-zinc-800/60 text-zinc-500'
                }`}
              >
                {status === 'complete' ? <Check className="h-3.5 w-3.5" /> : step.id}
              </div>
              <div className="min-w-0">
                <p
                  className={`text-sm font-semibold transition-colors duration-500 ${
                    status === 'complete' || status === 'current'
                      ? 'text-zinc-200'
                      : 'text-zinc-400'
                  }`}
                >
                  {step.title}
                </p>
                <p className="text-[10px] tracking-wider text-zinc-600">{step.caption}</p>
              </div>
            </li>
          );
        })}
      </ol>
    </aside>
  );
}
