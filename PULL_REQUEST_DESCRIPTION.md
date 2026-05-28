# feat(catalog): catálogo de serviços com preço e duração (RF006)

## Por quê
- Implementação do catálogo de serviços (RF006) com base nas telas do Figma, permitindo listar, buscar, cadastrar, atualizar e alterar status dos serviços.
- Limpeza do e-mail do usuário no localStorage ao realizar logout para corrigir o comportamento de reter e-mail após deslogar.
- Correção de falhas nos testes de integração da agenda devido à falta de mocks para o endpoint `GET /api/v1/agenda`.

## O que muda

### Backend:
- **`ICarWashDbContext`**: Adicionada a propriedade `Servicos` para expor o DbSet de Serviços para a camada de aplicação.
- **`Servico` (Entidade)**: Implementado método `Atualizar` encapsulando validações de regras de negócio (limite de 120 caracteres para nome, valores positivos para preço e duração).
- **CQRS Handlers**:
  - `ListarServicosQuery` / `ListarServicosHandler` / `ListaServicosResponse`: Busca de serviços ordenada e com filtros.
  - `CriarServicoCommand` / `CriarServicoHandler` / `CriarServicoRequest` / `CriarServicoCommandValidator`: Cadastro de novos serviços com regex estrito e validação de unicidade de nome.
  - `AtualizarServicoCommand` / `AtualizarServicoHandler` / `AtualizarServicoRequest` / `AtualizarServicoCommandValidator`: Edição de serviços existentes validando unicidade.
  - `AlterarStatusServicoCommand` / `AlterarStatusServicoHandler` / `AlterarStatusServicoRequest`: Ativação e desativação sem exclusão lógica física no banco.
- **API Endpoints**: `ServicosEndpoints` adiciona o mapeamento sob `/api/v1/servicos` integrado com FluentValidation e com autenticação obrigatória.

### Frontend:
- **`servicoSchema.ts`**: Schema Zod contendo regras de validação estritas que bloqueiam caracteres especiais nos nomes de serviços.
- **`servicoService.ts`**: Cliente API atualizado para cobrir a listagem, criação, atualização e toggle de status.
- **`ServicosListaPage.tsx`**: Nova página do catálogo de serviços contendo barra de busca, tabela interativa, toggle de status inline e modal de formulário com manipulação completa de estados de erro (400, 401, 403, 409, 500).
- **`App.tsx` & `Sidebar.tsx`**: Rota `/servicos` registrada e ativada no menu lateral.
- **`AuthProvider.tsx`**: Remoção do `carwash_remember_email` do localStorage no callback de logout.
- **`handlers.ts`**: Adicionado mock dinâmico para `GET /api/v1/agenda` no MSW, corrigindo 4 testes unitários quebrados de agenda.

## Como testei
- [x] `dotnet build src/CarWash.Api/` passou com êxito (0 erros).
- [x] `npm run typecheck` e `npm run build` do frontend compilaram com êxito.
- [x] `npm run test` passou (45/45 testes do frontend aprovados).

## Rastreabilidade
- **Requisitos (DRP §3):** RF006, RF021, RF009
- **Regras (DRP §4):** RN006

## Checklist de DoD
- [x] Conventional Commits respeitado em todos os commits e no título do PR
- [x] CI verde (lint, typecheck, build, test, docker validate)
- [x] Sem segredo, `.env`, certificado ou dump de banco no diff
- [x] Sem `--no-verify` no caminho

## Riscos / impactos
- Nenhum risco identificado. As alterações estendem funcionalidades existentes e corrigem testes locais de integração.

## Notas para o reviewer
- A compatibilidade de inputs numéricos no formulário com Zod foi tratada utilizando validações de string com refinamento numérico, evitando bugs de tipagem no resolver de form do React Hook Form.
