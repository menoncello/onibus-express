---
stepsCompleted: ['step-01-preflight', 'step-02-select-framework', 'step-03-scaffold-framework', 'step-04-docs-and-scripts', 'step-05-validate-and-summary']
lastStep: 'step-05-validate-and-summary'
lastSaved: '2026-08-24'
---

# Framework Setup Progress — OniBus Express

## Step 1: Preflight

**Detected stack:** `fullstack` (backend .NET + frontend React/Vite)

**Prerequisites:**
- `web/package.json` existe — OK
- Nenhum `playwright.config.*`/`cypress.config.*`/`cypress.json` existente — OK
- Manifestos backend existentes (`backend/OniBus.Api/OniBus.Api.csproj`, `OniBus.Domain`, `OniBus.Api.Tests`, `OniBus.Domain.Tests`) — OK
- Backend já tem xUnit v3 configurado em duas camadas (unit puro em `OniBus.Domain.Tests`, integração com `WebApplicationFactory` + Testcontainers/PostgreSQL em `OniBus.Api.Tests`) — não conflita com E2E, mas é contexto relevante

**Contexto do projeto:**
- Backend: .NET 10 (`net10.0`, minimal APIs), EF Core 10, Npgsql, PostgreSQL 18. Testes: xunit.v3 3.1.0, Testcontainers.PostgreSql 4.12.0.
- Frontend (`web/`): React 19.2, Vite 8.2, TypeScript 5.9. Testes: Vitest 4.1 + Testing Library.
- Empacotamento: Docker Compose (web:80 nginx → api:8080 → db:5432 Postgres). Ambiente alternativo sem Docker (`vite dev` + `dotnet run` + Postgres local).
- Arquitetura: `_bmad-output/planning-artifacts/architecture/architecture-ticket-busao-2026-08-18/ARCHITECTURE-SPINE.md` (17 ADs). Fatias verticais: `OniBus.Domain` (puro) → `OniBus.Api` (fatias por capacidade) → `web/src/features`.
- AD-10 fixa **duas** camadas de teste (unit + integração), sem E2E, e nenhum SQLite. CI planejado (`.github/workflows/ci.yml`, ainda não criado) só teria jobs `dotnet test` e `vitest run`.
- AD-11: frontend nunca conhece URL absoluta da API — sempre `/api/*` relativo, com proxy (Nginx em Docker, Vite dev proxy fora de Docker).
- AD-12: quatro estados de tela (carregando/sucesso/erro de entrada/falha de comunicação).
- Vocabulário de erro fechado (AD-9): `ProblemDetails` RFC 9457 com `codigo` — `CPF_INVALIDO`, `CAMPO_INVALIDO`, `ASSENTO_OCUPADO`, `VIAGEM_REALIZADA`, `JANELA_CANCELAMENTO_FECHADA`, `RESERVA_NAO_ENCONTRADA`, `VIAGEM_NAO_ENCONTRADA`.
- Nenhum doc de arquitetura/PRD menciona E2E, Playwright ou Cypress.

**Decisão do usuário (checkpoint fora do fluxo padrão):** AD-10 define apenas duas camadas de teste e nenhum documento de arquitetura ou PRD menciona E2E — o usuário foi consultado explicitamente e confirmou **seguir com Playwright** como camada adicional acima das duas já definidas (ex.: smoke test do fluxo completo via Docker Compose), não como substituição das camadas existentes.

**Status:** Preflight concluído. Prosseguindo para seleção de framework.

## Step 2: Seleção de Framework

**Browser-based (E2E):** Playwright.
- Motivo: fullstack, integração pesada API+UI, velocidade/paralelismo de CI, projeto já usa toolchain Node/TS moderno (Vite, TS 5.9).
- Escopo confirmado pelo usuário: camada adicional acima das duas já fixadas por AD-10 (não substitui unit/integração existentes).

**Backend:** xUnit (já configurado, retido como está — nenhuma mudança necessária).

**Status:** Seleção concluída. Prosseguindo para scaffolding do framework.

## Step 3: Scaffold do Framework

**Modo de execução:** sequencial (execução direta, com contexto de arquitetura já carregado — mais confiável que delegar a subagentes sem esse contexto para este volume de trabalho).

**Gate de relevância Pact/contract testing (`pactjs-utils-mandate.md` § Relevance Before Scaffolding):** fechado — `web` chama sua própria `api`, mesmo repositório, mesmo `docker-compose.yml`, deploy conjunto. Nenhum artefato Pact criado. Pode ser adicionado depois se um consumidor externo real aparecer.

**Playwright Utils:** usuário confirmou instalação de `@seontechnologies/playwright-utils@4.4.0` (peer `@playwright/test@1.62.1`).

**Arquivos criados:**
- `package.json` (raiz, novo — escopo só do pacote E2E; `@playwright/test`, `@seontechnologies/playwright-utils`, `@faker-js/faker` pinados)
- `.nvmrc` (raiz) — Node 24, alinhado ao `Dockerfile` do `web` (`node:24-alpine`)
- `playwright.config.ts` (raiz) — `testDir: ./e2e`, timeouts (action 15s/nav 30s/test 60s), `baseURL` via `BASE_URL` (default `http://localhost`, porta 80 do serviço `web` no compose), trace/screenshot/video em falha, reporters HTML+JUnit+list. Um único config (sem `local/staging/production`): a arquitetura declara explicitamente que não há staging/produção. Um só projeto `chromium` — nenhum FR pede cobertura cross-browser.
- `.env.example` (raiz, editado) — adicionado `TEST_ENV`, `BASE_URL`. **Sem `API_URL`**: AD-11 proíbe URL absoluta de API; os testes chamam `/api/*` relativo a `BASE_URL`, igual ao frontend.
- `.gitignore` (raiz, editado) — `node_modules/`, `test-results/`, `playwright-report/`, `blob-report/`, `.env`, `.DS_Store`
- `e2e/support/merged-fixtures.ts` — `mergeTests(apiRequestFixture, interceptFixture, networkErrorFixture, recurseFixture)`. **Desvio registrado:** sem `authFixture` — o sistema não tem autenticação/contas/sessão (Non-Goal explícito do PRD e da ARCHITECTURE-SPINE); o Código de Reserva é identificador de negócio, não credencial.
- `e2e/api/rotas.spec.ts` — sample de API para `GET /api/rotas` (FR-1), via `apiRequest`. Sem schema formal ainda (nenhum existe no projeto); asserção cobre só os campos de FR-1 e é resiliente à troca de seed da Story 1.4 (procura a Rota conhecida em vez de fixar contagem total).
- `e2e/ui/listar-rotas.spec.ts` — sample de UI: (1) lista renderiza via `interceptNetworkCall`, seletores por `role`/`aria-label` (o código já usa esse padrão, não `data-testid`); (2) 5xx nunca renderiza como "sem resultados" (AD-12), stub via `interceptNetworkCall({ fulfillResponse: { status: 500 } })` — nunca `page.route` direto, conforme o mandate.

**Escopo deliberadamente não coberto agora (não é omissão):**
- **Sem fixtures/factories de Passageiro/CPF** (`FR-6`, `FR-7`, `AD-14`): os endpoints de Reserva ainda não existem no código (só `GET /rotas` está implementado — Story 1.1). Criar geradores de CPF válido agora seria abstração sem teste que a exercite — o mesmo padrão que a própria arquitetura evita (seção "Deferred" do `ARCHITECTURE-SPINE.md`). Adicionar quando a Story de Reserva chegar (via `bmad-testarch-atdd`).
- **Sem `global-setup.ts`**: só existiria para wiring de auth, que não se aplica.
- Nenhum teste de fluxo completo (busca → assentos → reserva) ainda — endpoints não existem.

**Status:** Scaffold concluído. Prosseguindo para documentação e scripts.

## Step 4: Documentação & Scripts

- `e2e/README.md` criado — setup, execução (local/UI mode/headed/debug), arquitetura da suíte, boas práticas, notas de CI (pipeline ainda não existe — `.github/workflows/ci.yml` é escopo futuro), seção do hook de enforcement, referências à base de conhecimento.
- Scripts em `package.json` (raiz): `test:e2e`, `test:e2e:ui`, `test:e2e:report` (já adicionados no Step 3).
- Backend: nenhum script novo — `dotnet test` a partir de `backend/OniBus.sln` já é o comando padrão e já está documentado em `AGENTS.md`; criar um `Makefile` só para embrulhar um único comando já simples seria abstração não pedida.

**Hook de enforcement (write-time), plataforma Claude Code:**
- `.claude/hooks/tea-enforce.cjs` copiado byte a byte do skill (`sha256`: `f2ede9d399358d8810c660a29590f0c1683f7164bb3199038d26787f8324491c`).
- `.tea/enforce-config.json` criado: `testGlobs` = `e2e/**/*.spec.{ts,js}` (Playwright, escopo deste run) + `web/src/**/*.test.{ts,tsx,js,jsx}` (Vitest já existente no projeto — incluído para que os testes de componente já escritos também fiquem sob o mesmo enforcement). Sem Pact, sem Maestro, sem k6 — `pactConfigGlobs`/`excludeGlobs` vazios. `hookSha256` preenchido.
- `.claude/settings.json` criado (não existia) com os três hooks (`PreToolUse`, `PostToolUse`, `Stop`) apontando para o script.

**Status:** Documentação e scripts concluídos. Prosseguindo para validação e resumo final.

## Step 5: Validação & Resumo

**Validação executada de ponta a ponta (não só leitura de checklist):**
- `npm install` na raiz — OK, sem erros de resolução.
- `npx playwright install chromium --with-deps` — OK.
- `npx playwright test --list` — 3 testes reconhecidos, config e specs sem erro de sintaxe/tipos.
- `docker compose up --build -d` — os três serviços (`db`, `api`, `web`) subiram e ficaram `healthy`.
- `npx playwright test` contra o stack real: 1ª rodada pegou um problema real — o teste de 5xx falhava porque o fixture `network-error-monitor` auto-falha em qualquer 4xx/5xx por padrão, e esse teste provoca um 500 de propósito. Corrigido com a annotation `{ type: 'skipNetworkMonitoring' }` (padrão documentado em `network-error-monitor.md` § Opt-Out for Validation Tests). Após o fix: **3/3 testes passando** contra o stack real.
- `docker compose down` — ambiente desfeito ao final da validação.

**Checklist (`checklist.md`) — itens que não se aplicam a este projeto, com justificativa (não são lacunas):**
- Fixtures/factories de dados (`support/fixtures/factories/`, `auth-fixture.ts`, `global-setup.ts` de auth): sistema sem autenticação (Non-Goal do PRD); factories de Passageiro/CPF adiadas até a Story de Reserva existir (evita abstração sem teste que a exercite).
- Seletores `data-testid`: o frontend já usa `role`/`aria-label`, os testes seguem o mesmo padrão em vez de introduzir uma segunda convenção de seleção.
- Seções do checklist de Pact/contract testing (CDC): gate de relevância fechado — sem fronteira consumer-provider real neste repositório.

**Itens confirmados:**
- Stack detectado, prerequisites, sem conflito de framework existente.
- Framework escolhido e justificado (Playwright + xUnit retido), com confirmação explícita do usuário para o desvio de AD-10.
- Estrutura de diretórios (`e2e/api`, `e2e/ui`, `e2e/support`), config (`playwright.config.ts`) com timeouts/trace/screenshot/video/reporters conforme especificado, `.env.example`/`.nvmrc`/`.gitignore` atualizados.
- `merged-fixtures.ts` com `mergeTests`, `expect` e `log` re-exportados; nenhum sample importa de `@playwright/test` direto.
- Sample de API usa `apiRequest`; sample de UI usa `interceptNetworkCall` antes de `page.goto`.
- `@seontechnologies/playwright-utils` instalado e em `devDependencies` (`4.4.0`), nome de pacote correto em todo lugar.
- Documentação (`e2e/README.md`) com setup, execução, arquitetura, boas práticas, CI, seção do hook.
- Hook de enforcement instalado e registrado, script validado com `node -c` (sintaxe OK) e uma chamada `--pre` de sanidade (sai com código 0).
- Sem credenciais reais em nenhum arquivo criado; `.env.example` só tem placeholders óbvios (AD-16).

**Status:** Validação concluída. Framework de testes E2E pronto para uso.
