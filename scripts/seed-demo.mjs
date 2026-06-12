#!/usr/bin/env node
/**
 * Seed de demonstração do CarWash — popula a stack de DEV via API.
 *
 * Cria filiais, serviços, clientes (com veículos e responsáveis) e uma
 * carga grande de agendamentos respeitando as regras de negócio (RN010,
 * RN011, capacidade por célula). Os agendamentos "históricos" são criados
 * no futuro (a API só aceita início futuro) e depois deslocados para o
 * passado via SQL (scripts/seed-demo-shift.sql) já como concluídos.
 *
 * Uso:
 *   node scripts/seed-demo.mjs
 *   BASE_URL=http://localhost:8080 ADMIN_EMAIL=... ADMIN_SENHA=... node scripts/seed-demo.mjs
 */

const BASE_URL = process.env.BASE_URL ?? 'http://localhost:8080';
const ADMIN_EMAIL = process.env.ADMIN_EMAIL ?? 'admin@carwash.local';
const ADMIN_SENHA = process.env.ADMIN_SENHA ?? 'TrocarEmCadaAmbiente!2026';

// ───────────────────────── util ─────────────────────────

let TOKEN = '';

async function api(method, path, body) {
  const res = await fetch(`${BASE_URL}/api/v1${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {}),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    /* corpo não-JSON */
  }
  return { status: res.status, json, text };
}

function exigir(resp, esperado, contexto) {
  if (resp.status !== esperado) {
    throw new Error(`${contexto}: esperado ${esperado}, veio ${resp.status} — ${resp.text?.slice(0, 300)}`);
  }
  return resp.json;
}

const rng = (() => {
  // PRNG determinístico (mulberry32) para o seed ser reproduzível.
  let s = 0xc0ffee;
  return () => {
    s |= 0;
    s = (s + 0x6d2b79f5) | 0;
    let t = Math.imul(s ^ (s >>> 15), 1 | s);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
})();

const intEntre = (min, max) => min + Math.floor(rng() * (max - min + 1));
const escolher = (arr) => arr[Math.floor(rng() * arr.length)];

function cpfValido() {
  const d = Array.from({ length: 9 }, () => intEntre(0, 9));
  const dv = (parcial, peso) => {
    const soma = parcial.reduce((acc, n, i) => acc + n * (peso - i), 0);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };
  d.push(dv(d, 10));
  d.push(dv(d, 11));
  return d.join('');
}

const LETRAS = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
function placaValida() {
  // ^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$ (Mercosul)
  const l = () => LETRAS[intEntre(0, 25)];
  return `${l()}${l()}${l()}${intEntre(0, 9)}${l()}${intEntre(0, 9)}${intEntre(0, 9)}`;
}

// ───────────────────────── dados base ─────────────────────────

const NOMES = [
  'Ana Beatriz Souza', 'Bruno Carvalho Lima', 'Camila Ferreira Rocha', 'Diego Martins Alves',
  'Elaine Cristina Costa', 'Fábio Henrique Dias', 'Gabriela Nunes Pereira', 'Heitor Almeida Santos',
  'Isabela Moraes Castro', 'João Pedro Oliveira', 'Karina Lopes Barbosa', 'Lucas Gabriel Ribeiro',
  'Mariana Teixeira Gomes', 'Nicolas Cardoso Melo', 'Olívia Fernandes Pinto', 'Paulo Roberto Araújo',
  'Queila Andrade Ramos', 'Rafael Augusto Moreira', 'Sabrina Duarte Freitas', 'Thiago Vinícius Correia',
  'Úrsula Mendes Batista', 'Vitor Hugo Cavalcanti', 'Wesley Santana Cunha', 'Yasmin Rodrigues Farias',
  'Adriano Silveira Campos', 'Bianca Monteiro Reis', 'Caio César Vasconcelos', 'Daniela Aparecida Borges',
  'Eduardo Tavares Macedo', 'Fernanda Siqueira Prado', 'Gustavo Henrique Sales', 'Helena Vieira Brandão',
  'Igor Nascimento Paiva', 'Júlia Antunes Sampaio', 'Kevin Barros Fontes', 'Larissa Queiroz Amaral',
];

const RESPONSAVEIS_NOMES = [
  'Marcos Vinícius Silva', 'Patrícia Helena Dias', 'Roberto Carlos Mota', 'Simone Aparecida Luz',
  'Tatiane Cristina Neves', 'Vanderlei Augusto Leme', 'Cláudia Regina Matos', 'Anderson Luiz Prado',
];

const CARROS = [
  ['Onix', 'Chevrolet', 'Prata'], ['HB20', 'Hyundai', 'Branco'], ['Argo', 'Fiat', 'Vermelho'],
  ['Corolla', 'Toyota', 'Preto'], ['Civic', 'Honda', 'Cinza'], ['Polo', 'Volkswagen', 'Branco'],
  ['Compass', 'Jeep', 'Azul'], ['Kicks', 'Nissan', 'Prata'], ['Creta', 'Hyundai', 'Cinza'],
  ['T-Cross', 'Volkswagen', 'Preto'], ['Tracker', 'Chevrolet', 'Branco'], ['Renegade', 'Jeep', 'Verde'],
  ['Strada', 'Fiat', 'Branco'], ['Hilux', 'Toyota', 'Prata'], ['Ranger', 'Ford', 'Azul'],
  ['Mobi', 'Fiat', 'Vermelho'], ['Gol', 'Volkswagen', 'Cinza'], ['Fit', 'Honda', 'Preto'],
];

const FILIAIS = [
  { nome: 'CarWash Centro', codigo: 'CW01CENTRO', celulasAtivas: 5, cidade: 'São Paulo', uf: 'SP', logradouro: 'Rua Barão de Itapetininga', numero: '255', bairro: 'República', cep: '01042001' },
  { nome: 'CarWash Zona Norte', codigo: 'CW02NORTE', celulasAtivas: 4, cidade: 'São Paulo', uf: 'SP', logradouro: 'Av. Engenheiro Caetano Álvares', numero: '1820', bairro: 'Limão', cep: '02546000' },
  { nome: 'CarWash Zona Sul', codigo: 'CW03SUL', celulasAtivas: 6, cidade: 'São Paulo', uf: 'SP', logradouro: 'Av. Interlagos', numero: '3400', bairro: 'Interlagos', cep: '04661100' },
  { nome: 'CarWash Alphaville', codigo: 'CW04ALPHA', celulasAtivas: 3, cidade: 'Barueri', uf: 'SP', logradouro: 'Alameda Rio Negro', numero: '111', bairro: 'Alphaville', cep: '06454000' },
];

const SERVICOS = [
  { nome: 'Lavagem Detalhada Premium', preco: 120.0, duracaoMin: 90 },
  { nome: 'Higienização Interna Completa', preco: 180.0, duracaoMin: 120 },
  { nome: 'Polimento Técnico', preco: 250.0, duracaoMin: 150 },
  { nome: 'Cristalização de Pintura', preco: 320.0, duracaoMin: 180 },
  { nome: 'Lavagem de Motor', preco: 90.0, duracaoMin: 45 },
  { nome: 'Cera Premium Carnaúba', preco: 75.0, duracaoMin: 40 },
  { nome: 'Oxi-Sanitização (Ozônio)', preco: 140.0, duracaoMin: 60 },
];

const OBSERVACOES = [
  'Cliente deixará a chave na recepção.',
  'Atenção: arranhão pré-existente na porta direita.',
  'Cliente aguarda no local.',
  'Retirada agendada para o fim do dia.',
  'Cuidado com o sensor de estacionamento.',
  null, null, null,
];

// ───────────────────────── etapas ─────────────────────────

async function login() {
  const resp = await api('POST', '/auth/login', { email: ADMIN_EMAIL, senha: ADMIN_SENHA });
  const body = exigir(resp, 200, 'login');
  TOKEN = body.accessToken;
  console.log(`✔ login como ${ADMIN_EMAIL}`);
}

async function criarFiliais() {
  const ids = [];
  // Aproveita as filiais já existentes (seed técnico + execuções anteriores).
  const lista = exigir(await api('GET', '/filiais?tamanhoPagina=100'), 200, 'listar filiais');
  const existentes = new Map((lista.itens ?? []).map((f) => [f.nome, f.id]));

  for (const f of FILIAIS) {
    if (existentes.has(f.nome)) {
      ids.push(existentes.get(f.nome));
      continue;
    }
    const resp = await api('POST', '/filiais', {
      nome: f.nome,
      codigo: f.codigo,
      celulasAtivas: f.celulasAtivas,
      timezone: 'America/Sao_Paulo',
      endereco: { cep: f.cep, logradouro: f.logradouro, numero: f.numero, bairro: f.bairro, cidade: f.cidade, uf: f.uf },
    });
    const body = exigir(resp, 201, `criar filial ${f.nome}`);
    ids.push(body.id);
  }
  // Inclui também as demais filiais ativas já cadastradas (ex.: Matriz do seed).
  for (const [nome, id] of existentes) {
    if (!ids.includes(id) && (lista.itens.find((x) => x.id === id)?.ativa ?? true)) {
      void nome;
      ids.push(id);
    }
  }
  console.log(`✔ filiais prontas: ${ids.length}`);
  return ids;
}

async function criarServicos() {
  const lista = exigir(await api('GET', '/servicos?tamanhoPagina=100'), 200, 'listar serviços');
  const existentes = new Map((lista.itens ?? []).map((s) => [s.nome, s.id]));
  const ids = [...existentes.values()];

  for (const s of SERVICOS) {
    if (existentes.has(s.nome)) continue;
    const resp = await api('POST', '/servicos', s);
    const body = exigir(resp, 201, `criar serviço ${s.nome}`);
    ids.push(body.id ?? body.data?.id);
  }
  console.log(`✔ serviços prontos: ${ids.length}`);
  // Mapa id → duração para calcular janelas sem conflito.
  const listaFinal = exigir(await api('GET', '/servicos?tamanhoPagina=100'), 200, 'relistar serviços');
  return (listaFinal.itens ?? []).filter((s) => s.ativo !== false).map((s) => ({ id: s.id, duracaoMin: s.duracaoMin ?? 60 }));
}

async function criarClientes() {
  const clientes = [];
  for (let i = 0; i < NOMES.length; i++) {
    const nome = NOMES[i];
    const qtdVeiculos = intEntre(1, 3);
    const veiculos = Array.from({ length: qtdVeiculos }, () => {
      const [modelo, fabricante, cor] = escolher(CARROS);
      return { placa: placaValida(), modelo, fabricante, cor };
    });

    const resp = await api('POST', '/clientes', {
      nome,
      dataNascimento: `${intEntre(1960, 2003)}-${String(intEntre(1, 12)).padStart(2, '0')}-${String(intEntre(1, 28)).padStart(2, '0')}`,
      cpf: cpfValido(),
      celular: `119${intEntre(10000000, 99999999)}`,
      email: `${nome.toLowerCase().normalize('NFD').replace(/[^a-z ]/g, '').trim().replace(/ +/g, '.')}.${i}@exemplo.com.br`,
      endereco: {
        cep: '01310100',
        logradouro: 'Av. Paulista',
        numero: String(intEntre(100, 2500)),
        bairro: 'Bela Vista',
        cidade: 'São Paulo',
        uf: 'SP',
      },
      veiculos,
    });
    if (resp.status === 409) {
      // duplicidade (re-execução) — ignora e segue.
      continue;
    }
    const body = exigir(resp, 201, `criar cliente ${nome}`);
    const clienteId = body.id;

    const detalhe = exigir(await api('GET', `/clientes/${clienteId}`), 200, `detalhe cliente ${nome}`);
    const veiculoIds = (detalhe.veiculos ?? []).map((v) => v.id);

    // RF024 exige responsável no agendamento — todo cliente ganha 1 (alguns 2).
    const responsavelIds = [];
    const qtdResp = rng() < 0.25 ? 2 : 1;
    for (let r = 0; r < qtdResp; r++) {
      const respResp = await api('POST', `/clientes/${clienteId}/responsaveis`, {
        nome: escolher(RESPONSAVEIS_NOMES),
        documento: cpfValido(),
        telefone: `119${intEntre(10000000, 99999999)}`,
        email: `resp.${i}.${r}@exemplo.com.br`,
        grauVinculo: escolher(['RESPONSAVEL_FINANCEIRO', 'PROCURADOR', 'CONJUGE', 'OUTRO']),
      });
      const respBody = exigir(respResp, 201, `criar responsável de ${nome}`);
      responsavelIds.push(respBody.data.responsavelId);
    }

    clientes.push({ id: clienteId, nome, veiculoIds, responsavelIds });
  }
  console.log(`✔ clientes criados: ${clientes.length} (com veículos e responsáveis)`);
  return clientes;
}

/**
 * Cria os agendamentos. Para evitar RN011 e estouro de capacidade:
 * cada veículo recebe no máximo 1 agendamento por dia, e cada slot
 * filial+dia+hora recebe no máximo 2 atendimentos.
 */
async function criarAgendamentos(filialIds, servicos, clientes) {
  const criados = [];
  const slotOcupacao = new Map(); // chave filial|dia|hora → contagem
  const veiculoDia = new Set(); // chave veiculo|dia

  const veiculosPool = clientes.flatMap((c) =>
    c.veiculoIds.map((v) => ({ clienteId: c.id, veiculoId: v, responsavelId: escolher(c.responsavelIds) })),
  );

  // dia 1..40 (futuro) — os primeiros ~60% serão deslocados para o passado via SQL.
  let tentativas = 0;
  const ALVO = 150;
  while (criados.length < ALVO && tentativas < ALVO * 6) {
    tentativas++;
    const alvo = escolher(veiculosPool);
    const dia = intEntre(1, 40);
    const hora = intEntre(8, 16);
    const filialId = escolher(filialIds);

    const chaveVeiculo = `${alvo.veiculoId}|${dia}`;
    const chaveSlot = `${filialId}|${dia}|${hora}`;
    if (veiculoDia.has(chaveVeiculo)) continue;
    if ((slotOcupacao.get(chaveSlot) ?? 0) >= 2) continue;

    const inicio = new Date();
    inicio.setUTCDate(inicio.getUTCDate() + dia);
    inicio.setUTCHours(hora, escolher([0, 0, 30]), 0, 0);

    const qtdServicos = intEntre(1, 3);
    const servicoIds = [...new Set(Array.from({ length: qtdServicos }, () => escolher(servicos).id))];

    const resp = await api('POST', '/agendamentos', {
      filialId,
      clienteId: alvo.clienteId,
      veiculoId: alvo.veiculoId,
      responsavelId: alvo.responsavelId,
      inicio: inicio.toISOString(),
      servicoIds,
      observacoes: escolher(OBSERVACOES),
    });

    if (resp.status === 409 || resp.status === 422) continue; // conflito/capacidade — sorteia outro slot
    const body = exigir(resp, 201, 'criar agendamento');
    veiculoDia.add(chaveVeiculo);
    slotOcupacao.set(chaveSlot, (slotOcupacao.get(chaveSlot) ?? 0) + 1);
    criados.push({ id: body.id, dia });
  }
  console.log(`✔ agendamentos criados: ${criados.length}`);
  return criados;
}

async function cancelarAlguns(agendamentos) {
  const motivos = [
    'Cliente desmarcou por telefone.',
    'Veículo indisponível na data.',
    'Reagendado a pedido do cliente.',
    'Chuva forte — cliente preferiu remarcar.',
  ];
  const alvos = agendamentos.filter(() => rng() < 0.08);
  for (const a of alvos) {
    const resp = await api('PATCH', `/agendamentos/${a.id}/cancelar`, {
      motivoCancelamento: escolher(motivos),
      origem: 'CLIENTE',
    });
    if (resp.status !== 200) console.warn(`  aviso: cancelar ${a.id} → ${resp.status}`);
    a.cancelado = true;
  }
  console.log(`✔ agendamentos cancelados: ${alvos.length}`);
}

async function iniciarAlgunsDeHoje(filialIds, servicos, clientes) {
  // Agendamentos para HOJE (hora futura) que ficam EM_ANDAMENTO — vivos na demo.
  const agora = new Date();
  let iniciados = 0;
  const candidatos = clientes.slice(0, 12);
  for (const c of candidatos) {
    if (iniciados >= 5) break;
    const inicio = new Date(agora.getTime() + (30 + iniciados * 10) * 60 * 1000);
    const resp = await api('POST', '/agendamentos', {
      filialId: escolher(filialIds),
      clienteId: c.id,
      veiculoId: c.veiculoIds[0],
      responsavelId: c.responsavelIds[0],
      inicio: inicio.toISOString(),
      servicoIds: [escolher(servicos).id],
      observacoes: 'Atendimento em andamento (demo).',
    });
    if (resp.status !== 201) continue;
    const ag = resp.json;
    const ini = await api('PATCH', `/agendamentos/${ag.id}/iniciar`, {});
    if (ini.status === 200) iniciados++;
  }
  console.log(`✔ atendimentos em andamento (hoje): ${iniciados}`);
}

// ───────────────────────── main ─────────────────────────

const inicio = Date.now();
await login();
const filialIds = await criarFiliais();
const servicos = await criarServicos();
const clientes = await criarClientes();
const agendamentos = await criarAgendamentos(filialIds, servicos, clientes);
await cancelarAlguns(agendamentos);
await iniciarAlgunsDeHoje(filialIds, servicos, clientes);

console.log(`\nSeed via API concluído em ${((Date.now() - inicio) / 1000).toFixed(1)}s.`);
console.log('Agora rode o passo SQL para gerar o histórico (passado/concluídos):');
console.log('  docker exec -i carwash-postgres psql -U carwash_owner -d carwash < scripts/seed-demo-shift.sql');
