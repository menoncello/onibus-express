---
title: 'Esqueleto vivo — ambiente sobe com um comando e lista Rotas'
type: 'feature'
created: '2026-08-21'
status: 'done'
review_loop_iteration: 0
baseline_commit: '46a4a6d5dd6005c7f8253169ec6a7d27aed88639'
context: ['_bmad-output/implementation-artifacts/epic-1-context.md', '_bmad-output/planning-artifacts/architecture/architecture-ticket-busao-2026-08-18/ARCHITECTURE-SPINE.md']
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** O repositório é greenfield — não existe ambiente executável nem esqueleto de projetos. Sem isso, nenhuma story seguinte roda, e o avaliador técnico não pode verificar o critério binário SM-1 (`docker-compose up --build` funcional, sem passo manual).

**Approach:** Criar o esqueleto Docker Compose (web/api/db), os três projetos .NET (`OniBus.Domain` puro, `OniBus.Api`, `web` React) e a primeira migração EF Core já contendo os dois índices únicos de `reservas` (AD-6/AD-7), com um único endpoint ponta a ponta: `GET /rotas` (Api → Nginx → frontend).

## Boundaries & Constraints

**Always:**
- `OniBus.Domain.csproj` sem nenhum `PackageReference`/`ProjectReference` (AD-1)
- Primeira migração cria `reservas` já com `ux_reservas_codigo` e `ux_reservas_viagem_assento_confirmada` (parcial, `WHERE status = 'Confirmada'`), mesmo sem endpoint de reserva ainda
- `StatusReserva` persistido como string (`HasConversion<string>()`)
- `docker-compose.yml`: serviços `web`, `api`, `db`; `api` usa `depends_on: condition: service_healthy` sobre healthcheck `pg_isready` do `db`
- Migração roda no boot da Api, mesmo caminho de código com ou sem Docker
- Frontend nunca usa URL absoluta: sempre `/api/*` (Nginx faz proxy no Docker); frontend é servido por Nginx no ambiente Docker, nunca por servidor de dev
- `GET /rotas` é público (sem autenticação), retorna `200` com origem, destino e duração estimada
- Ao menos uma Rota semeada no banco para o endpoint ter o que listar (seed mínimo — o seed completo com casos de borda é Story 1.4)

**Ask First:**
- Se o seed mínimo desta story deve já usar a estrutura idempotente definitiva (reaproveitada por Story 1.4) ou um seed provisório mais simples — confirmar antes de investir na estrutura completa aqui.

**Never:**
- Criar tabela ou coluna de Assentos/contagem (AD-4 é de story futura, mas não violar a fronteira agora)
- Adicionar variável de ambiente de URL de API
- Usar SQLite in-memory em qualquer teste

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Clone limpo, `docker-compose up --build` | repositório limpo, sem passo manual anterior | 3 serviços sobem, migração aplica automaticamente | N/A |
| Api inicia antes do Postgres aceitar conexões | `db` ainda inicializando | Api aguarda `pg_isready` via `depends_on: service_healthy`, não tenta conectar antes | N/A |
| `GET /rotas` com Rotas semeadas | ≥1 Rota no banco | `200` com lista (origem, destino, duração estimada) | N/A |
| Página inicial do frontend | acesso via Nginx (`:80`) | lista de rotas carregada via `/api/rotas`, caminho relativo | falha de comunicação distinta de vazio |

</frozen-after-approval>

## Code Map

<!-- Greenfield: nenhum arquivo existe ainda. Árvore-alvo conforme ARCHITECTURE-SPINE.md. -->

- `docker-compose.yml` -- orquestra web/api/db; healthcheck `pg_isready` no `db`; `depends_on: condition: service_healthy` na `api`
- `.env.example` -- credenciais locais óbvias, sem segredo real
- `backend/OniBus.sln` -- solução com os 4 projetos (Domain, Api, Domain.Tests, Api.Tests)
- `backend/OniBus.Domain/OniBus.Domain.csproj` -- projeto puro, zero referências (AD-1); inclui `Viagens/LayoutOnibus` (constante 44 assentos, usada só por stories futuras)
- `backend/OniBus.Api/Persistence/` -- `DbContext`, mapeamento de `Reserva`/`Viagem`/`Rota`, `Migrations/` com a primeira migração (os dois índices únicos)
- `backend/OniBus.Api/Persistence/Seed/` -- seed mínimo (≥1 Rota) executado no boot
- `backend/OniBus.Api/Features/Rotas/` -- fatia vertical do endpoint `GET /rotas`
- `backend/OniBus.Api/Dockerfile`
- `web/src/features/busca/` -- página inicial que lista rotas via cliente tipado
- `web/src/shared/api/` -- cliente HTTP tipado, `fetch` sobre `/api/*`
- `web/nginx.conf` -- serve a SPA + proxy `/api` → `api:8080`
- `web/Dockerfile`

## Tasks & Acceptance

**Execution:**
- [x] `backend/OniBus.sln`, `backend/OniBus.Domain/OniBus.Domain.csproj` -- criar solução e projeto Domain vazio, sem nenhuma referência -- base do AD-1
- [x] `backend/OniBus.Api/` -- criar projeto minimal API (`net10.0`) referenciando só `OniBus.Domain` -- estabelece a fatia vertical
- [x] `backend/OniBus.Api/Persistence/OniBusDbContext.cs` + `Migrations/` -- modelar `reservas`/`viagens`/`rotas` e gerar a primeira migração com os dois índices únicos e `StatusReserva` como string -- pré-condição de FR-8/FR-9 mesmo sem endpoint de reserva
- [x] `backend/OniBus.Api/Program.cs` -- aplicar `Database.MigrateAsync()` + seed mínimo no boot, antes de aceitar requisições
- [x] `backend/OniBus.Api/Persistence/Seed/` -- seed mínimo com ≥1 Rota (natural key, sem literal de calendário)
- [x] `backend/OniBus.Api/Features/Rotas/GetRotas.cs` -- endpoint `GET /rotas`, público, retorna origem/destino/duração
- [x] `backend/OniBus.Api/Dockerfile`, `web/Dockerfile`, `docker-compose.yml`, `.env.example` -- três serviços, healthcheck `pg_isready`, `depends_on: condition: service_healthy`
- [x] `web/` -- inicializar projeto Vite + React 19 + TypeScript; `web/src/shared/api/` cliente tipado; `web/src/features/busca/` página inicial consumindo `GET /api/rotas`
- [x] `web/nginx.conf` -- servir SPA + proxy `/api` → `api:8080`, sem CORS

**Acceptance Criteria:**
- Given um clone limpo do repositório, when executo `docker-compose up --build`, then os três serviços sobem, a migração é aplicada, e a aplicação está pronta sem nenhum passo manual adicional
- Given `OniBus.Domain.csproj`, when inspeciono o arquivo, then ele não contém nenhum `PackageReference` nem `ProjectReference`
- Given o serviço `api` com `depends_on: condition: service_healthy`, when o compose sobe, then a Api não tenta conectar ao banco antes dele aceitar conexões
- Given ao menos uma Rota semeada, when faço `GET /rotas`, then recebo `200` com origem, destino e duração estimada de cada rota
- Given o frontend servido por Nginx, when acesso a página inicial, then vejo a lista de rotas carregada via `/api/rotas`, caminho relativo, sem URL absoluta

### Review Findings

Revisão de código (2026-08-25) sobre o diff de remediação de gaps do trace (2 arquivos modificados, 2 arquivos novos: `OniBusDbContextIndexesTests.cs`, `listar-rotas.spec.ts`, `OniBusDomainCsprojBoundaryTests.cs`, `scripts/verify-compose-boot.sh`).

- [x] [Review][Patch] `verify-compose-boot.sh` não tem nenhum gatilho automatizado/descobrível — adicionar entrada no `package.json` [scripts/verify-compose-boot.sh:1]
- [x] [Review][Patch] `OniBusDomainCsprojBoundaryTests` só verifica `PackageReference`/`ProjectReference`, não `Reference`/`FrameworkReference` [backend/OniBus.Domain.Tests/OniBusDomainCsprojBoundaryTests.cs:20]
- [x] [Review][Patch] Mesmo teste não protege contra mudança do atributo `Sdk` do csproj [backend/OniBus.Domain.Tests/OniBusDomainCsprojBoundaryTests.cs:20]
- [x] [Review][Patch] Teste de persistência de `StatusReserva` como string só cobre `Confirmada`, não `Cancelada` [backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs:132]
- [x] [Review][Patch] Comentário do script cita `docker-compose` (v1) mas o comando executado é `docker compose` (v2) [scripts/verify-compose-boot.sh:28]
- [x] [Review][Patch] `curl` sem timeout; com `set -e`, falha de conexão aborta antes da mensagem de erro própria do script imprimir [scripts/verify-compose-boot.sh:76]
- [x] [Review][Patch] `docker compose down -v` no cleanup destrói o volume `db-data` sem aviso [scripts/verify-compose-boot.sh:22]
- [x] [Review][Patch] Teste E2E de carregando usa atraso fixo de 500ms — risco de fragilidade de tempo sob CI lento [e2e/ui/listar-rotas.spec.ts:35]
- [x] [Review][Patch] Título do teste E2E de carregando não tem a tag `(AD-12)` que os outros 3 testes do arquivo têm [e2e/ui/listar-rotas.spec.ts:35]
- [x] [Review][Defer] Porta 80 fixa sem mecanismo de override, em `docker-compose.yml` e no script novo [docker-compose.yml:45] — deferred, pre-existing

## Spec Change Log

- 2026-08-21: Implementação. O item "Ask First" sobre a estrutura do seed mínimo foi resolvido a favor da opção provisória e simples: `RotaSeed.SeedAsync` checa existência por chave natural (origem, destino) antes de inserir, sem construir a infraestrutura reutilizável de seed (múltiplas Rotas/Viagens, geração de Reservas via `IGeradorCodigoReserva`) que a Story 1.4 introduzirá. Ainda assim é idempotente por chave natural, não por contagem — sobrevive a um segundo `docker-compose up` sem duplicar. `Reserva` foi modelada com o mínimo que os dois índices únicos exigem (`ViagemId`, `NumeroAssento`, `Codigo`, `Status`); os campos de Passageiro inline (AD-5) ficam para a story que introduz `POST /reservas`. Nenhuma decisão de arquitetura (AD-1 a AD-17) foi violada ou renegociada.
- 2026-08-21: Aplicados os 9 findings de patch da revisão adversarial (3 camadas) sobre o diff desta story: (1) `docker-compose.yml` — healthcheck na `api` via `wget` contra `GET /rotas` (imagem base já tem busybox wget; sem dependência nova) e `web` agora depende de `api: condition: service_healthy`, fechando a janela de 502 logo após o `up`; (2) criado `web/.dockerignore` (node_modules, dist); (3) `appsettings.json` não carrega mais a connection string — o valor de desenvolvimento óbvio ficou só em `appsettings.Development.json`, produção depende exclusivamente da env var `ConnectionStrings__Default`; (4) migração `InitialCreate` regenerada com índice único `ux_rotas_origem_destino` em `(origem, destino)` — a idempotência do seed agora tem garantia do banco, não só da aplicação — e novo teste `RotaSeedTests` chama `SeedAsync` duas vezes e afirma 1 linha; (5) `GetRotas.cs` usa `Math.Round` em vez de cast truncando na conversão de duração para minutos; (6) `client.ts` captura erro de parse de JSON com status 2xx e relança como `ApiComunicacaoError`; (7) novo `client.test.ts` faz stub direto de `global.fetch` (não mocka o módulo `rotas`) cobrindo sucesso, 5xx e 404; (8) `ListaDeRotas.test.tsx` ganhou caso de lista vazia sem `role="alert"` — e, ao escrevê-lo, corrigido um vazamento de estado entre testes (faltava `cleanup()` do Testing Library, já que `globals: false` no Vitest desativa o auto-cleanup); (9) `GetRotasTests.cs` ganhou caso com duas Rotas afirmando a ordenação por origem/destino. Todos os comandos da seção Verification e as três suítes de teste (`dotnet test` Domain+Api, `npm run test`, `npm run build`) foram re-executados e passam.

## Verification

**Commands:**
- `docker-compose up --build` -- expected: os três serviços sobem sem erro; sem intervenção manual
- `curl -s http://localhost/api/rotas` -- expected: `200` com JSON contendo ao menos uma rota
- `dotnet build backend/OniBus.sln` -- expected: build sem erros
- `dotnet list backend/OniBus.Domain/OniBus.Domain.csproj package` e `reference` -- expected: saída vazia em ambos

**Manual checks (if no CLI):**
- Abrir `http://localhost` e confirmar que a lista de rotas aparece sem erro no console do navegador

## Suggested Review Order

**Schema e unicidade**

- Entrada: índice único de `rotas` que sustenta a idempotência do seed a nível de banco.
  [`OniBusDbContext.cs:28`](../../backend/OniBus.Api/Persistence/OniBusDbContext.cs#L28)

- Os dois índices únicos de `reservas` (AD-6/AD-7) já existem antes de qualquer endpoint de reserva.
  [`OniBusDbContext.cs:69`](../../backend/OniBus.Api/Persistence/OniBusDbContext.cs#L69)

- `StatusReserva` persistido como string, não como inteiro — o filtro do índice parcial cita o literal.
  [`OniBusDbContext.cs:58`](../../backend/OniBus.Api/Persistence/OniBusDbContext.cs#L58)

**Boot e prontidão do ambiente (SM-1)**

- Migração e seed rodam no boot, mesmo caminho de código com ou sem Docker.
  [`Program.cs:20`](../../backend/OniBus.Api/Program.cs#L20)

- Seed idempotente por chave natural, não por contagem — sobrevive a um segundo boot.
  [`RotaSeed.cs:26`](../../backend/OniBus.Api/Persistence/Seed/RotaSeed.cs#L26)

- `api` só é considerada saudável depois que o próprio endpoint responde; `web` espera essa saúde antes de proxy-ar.
  [`docker-compose.yml:31`](../../docker-compose.yml#L31)

- `db` usa `pg_isready`; `api` só inicia depois que o Postgres aceita conexões.
  [`docker-compose.yml:8`](../../docker-compose.yml#L8)

**Endpoint `GET /rotas`**

- Fatia vertical do endpoint: rota pública, sem autenticação.
  [`GetRotas.cs:15`](../../backend/OniBus.Api/Features/Rotas/GetRotas.cs#L15)

- Duração arredondada (não truncada) na conversão para minutos.
  [`GetRotas.cs:26`](../../backend/OniBus.Api/Features/Rotas/GetRotas.cs#L26)

**Consumo no frontend**

- Estado de sucesso distingue lista vazia de falha de comunicação (AD-12).
  [`ListaDeRotas.tsx:55`](../../web/src/features/busca/ListaDeRotas.tsx#L55)

- Cliente HTTP tipado nunca deixa escapar `SyntaxError` cru de um `200` com corpo inválido.
  [`client.ts:43`](../../web/src/shared/api/client.ts#L43)

- Nginx faz proxy de `/api/*` para a Api, sem CORS e sem URL absoluta no frontend (AD-11).
  [`nginx.conf:9`](../../web/nginx.conf#L9)

**Periféricos**

- `OniBus.Domain.csproj` sem nenhuma referência — o arquivo é a fronteira, não a disciplina (AD-1).
  [`OniBus.Domain.csproj:1`](../../backend/OniBus.Domain/OniBus.Domain.csproj#L1)

- Regressão coberta: dois boots seguidos do seed não duplicam a Rota.
  [`RotaSeedTests.cs`](../../backend/OniBus.Api.Tests/Persistence/Seed/RotaSeedTests.cs)

- Regressão coberta: `client.ts` agora executa de fato em teste (stub de `fetch`, não do módulo `rotas`).
  [`client.test.ts`](../../web/src/shared/api/client.test.ts)

- Regressão coberta: ordenação de `GET /rotas` com mais de uma Rota semeada.
  [`GetRotasTests.cs`](../../backend/OniBus.Api.Tests/Features/Rotas/GetRotasTests.cs)
