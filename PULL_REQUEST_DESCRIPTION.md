# feat(shared): vinculação e exibição de veículos do cliente e restrição de caracteres especiais (RF004/RF005)

## Por quê
- Correção do fluxo onde os veículos vinculados aos clientes não apareciam após o salvamento, e o contador numérico de veículos na página de detalhes não era exibido.
- Implementação de restrições para caracteres especiais e números em nomes de clientes, nomes de veículos, placas, logradouros, bairros e cidades, tanto no frontend quanto no backend.

## O que muda

### Backend:
- **`ClienteResponse`**: Adicionada a propriedade `Veiculos` contendo `ClienteVeiculoResponse` (id, placa, modelo, fabricante, cor).
- **`ObterClientePorIdHandler`**: Injeta `ICarWashDbContext` para consultar os veículos ativos associados ao cliente.
- **`CriarClienteRequest` & `CriarClienteCommand` & `ClientesEndpoints`**: Atualizados para transitar a lista de veículos no fluxo de criação.
- **`CriarClienteHandler`**: Injeta `IVeiculoService` e persiste os veículos vinculados logo após salvar o cliente no banco.
- **`CriarClienteCommandValidator` & `AtualizarClienteCommandValidator`**: Incluem validações `Matches` de Regex para proibir números e caracteres especiais em nomes e campos de endereço (`Logradouro`, `Bairro`, `Cidade`).
- **`VeiculoService`**: Adiciona expressões regulares compiladas na validação local (`Validate`) para impedir caracteres especiais e números em `Fabricante` e `Cor`, e caracteres especiais em `Modelo`.

### Frontend:
- **`clienteSchema.ts` & `veiculoSchema.ts`**: Atualizados com expressões regulares estritas (`CLIENTE_NOME_PATTERN`, `LOGRADOURO_PATTERN`, `BAIRRO_PATTERN`, `CIDADE_PATTERN`, `VEICULO_TEXTO_PATTERN`, `FABRICANTE_PATTERN` e `COR_PATTERN`) rejeitando caracteres especiais nos campos.

### Testes:
- **`TestDbSetHelper`**: Novo helper unitário criado para mockar DbSets assíncronos do EF Core.
- **`ObterClientePorIdHandlerTests` & `CriarClienteHandlerTests`**: Atualizados e testados com cobertura para injeção de dependências e comportamentos de mapeamento.

## Como testei
- [x] `dotnet test` passou (291/291 unit tests aprovados)
- [x] Validação local do frontend validada via Zod.

## Rastreabilidade
- **Requisitos (DRP §3):** RF004 / RF005
- **Regras (DRP §4):** RN003

## Checklist de DoD
- [x] Conventional Commits respeitado em todos os commits e no título do PR
- [x] CI verde (lint, typecheck, build, test, docker validate)
- [x] Sem segredo, `.env`, certificado ou dump de banco no diff
- [x] Sem `--no-verify` no caminho

## Riscos / impactos
- Nenhum risco de quebra identificado. O fluxo existente foi estendido preservando as assinaturas dos endpoints e comportamento do banco de dados.

## Notas para o reviewer
- O helper `TestDbSetHelper` facilita muito o mock de consultas assíncronas do Entity Framework em testes unitários subsequentes.
