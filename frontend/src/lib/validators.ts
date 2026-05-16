export function isValidCpf(raw: string): boolean {
  const d = raw.replace(/\D/g, '');
  if (d.length !== 11) return false;
  if (/^(\d)\1{10}$/.test(d)) return false;

  let sum = 0;
  for (let i = 0; i < 9; i++) sum += Number(d[i]) * (10 - i);
  let check = 11 - (sum % 11);
  if (check >= 10) check = 0;
  if (Number(d[9]) !== check) return false;

  sum = 0;
  for (let i = 0; i < 10; i++) sum += Number(d[i]) * (11 - i);
  check = 11 - (sum % 11);
  if (check >= 10) check = 0;
  return Number(d[10]) === check;
}

export function isValidCnpj(raw: string): boolean {
  const d = raw.replace(/\D/g, '');
  if (d.length !== 14) return false;
  if (/^(\d)\1{13}$/.test(d)) return false;

  const w1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
  const w2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

  let sum = 0;
  for (let i = 0; i < 12; i++) sum += Number(d[i]) * w1[i]!;
  let check = sum % 11 < 2 ? 0 : 11 - (sum % 11);
  if (Number(d[12]) !== check) return false;

  sum = 0;
  for (let i = 0; i < 13; i++) sum += Number(d[i]) * w2[i]!;
  check = sum % 11 < 2 ? 0 : 11 - (sum % 11);
  return Number(d[13]) === check;
}

export function isValidEmail(email: string): boolean {
  return /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/.test(email);
}
