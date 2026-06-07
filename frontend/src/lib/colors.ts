export function getCorCSS(cor: string | undefined): string | undefined {
  if (!cor) return undefined;

  const map: Record<string, string> = {
    branco: '#FFFFFF',
    preto: '#18181b', // Usar um tom bem escuro em vez de puro preto (#000) por causa do tema escuro
    prata: '#e4e4e7',
    cinza: '#71717a',
    chumbo: '#3f3f46',
    vermelho: '#ef4444',
    azul: '#3b82f6',
    amarelo: '#eab308',
    verde: '#22c55e',
    marrom: '#78350f',
    bege: '#fef3c7',
    bordo: '#7f1d1d',
    roxo: '#a855f7',
    laranja: '#f97316',
    dourado: '#ca8a04',
    rosa: '#ec4899',
    champagne: '#fde047',
  };

  const cleanCor = cor.toLowerCase().trim();
  // Retorna a cor mapeada ou, se não estiver no mapa, retorna o valor original (que pode ser um HEX válido ou nome CSS).
  return map[cleanCor] ?? cor;
}

export const PRESET_COLORS = [
  { name: 'VERMELHO', hex: '#E61F2D' },
  { name: 'PRETO', hex: '#1A1A1A' },
  { name: 'BRANCO', hex: '#F5F5F5' },
  { name: 'CINZA', hex: '#808080' },
  { name: 'AZUL', hex: '#2563EB' },
  { name: 'BEGE', hex: '#D4A843' },
  { name: 'VERDE', hex: '#22C55E' },
] as const;
