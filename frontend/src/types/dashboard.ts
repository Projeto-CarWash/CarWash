export interface DashboardFiltros {
  inicio: string; // ISO date string (YYYY-MM-DD)
  fim: string; // ISO date string (YYYY-MM-DD)
  filialId?: string;
  status?: string;
}

export interface DashboardMetricas {
  total: number;
  pendentes: number;
  concluidos: number;
  cancelados: number;
  ocupacao: number; // Porcentagem (0-100)
  tempoMedio: number; // Minutos
  faturamento: number; // Em centavos ou reais? Vamos usar reais (number)
  ticketMedio: number; // Em reais (number)
}
