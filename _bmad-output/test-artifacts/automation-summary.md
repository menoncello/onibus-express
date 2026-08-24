---
stepsCompleted: ['step-01-preflight-and-context', 'step-02-identify-targets', 'step-03-generate-tests', 'step-03c-aggregate', 'step-04-validate-and-summarize']
lastStep: 'step-04-validate-and-summarize'
lastSaved: '2026-08-24'
inputDocuments:
  - '_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md'
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - '_bmad-output/planning-artifacts/architecture/architecture-ticket-busao-2026-08-18/ARCHITECTURE-SPINE.md'
  - '_bmad-output/test-artifacts/framework-setup-progress.md'
  - 'backend/OniBus.Api/Persistence/OniBusDbContext.cs'
  - 'backend/OniBus.Api/Features/Rotas/GetRotas.cs'
  - 'backend/OniBus.Api.Tests/Features/Rotas/GetRotasTests.cs'
  - 'backend/OniBus.Api.Tests/Persistence/Seed/RotaSeedTests.cs'
  - 'web/src/shared/api/client.ts'
  - 'web/src/shared/api/client.test.ts'
  - 'web/src/features/busca/ListaDeRotas.tsx'
  - 'web/src/features/busca/ListaDeRotas.test.tsx'
  - 'e2e/api/rotas.spec.ts'
  - 'e2e/ui/listar-rotas.spec.ts'
---

# Automation Summary — Story 1.1 (Esqueleto vivo)

## Step 1: Preflight & Context

- **Stack detectado:** `fullstack` (backend .NET em `backend/`, frontend React/Vite em `web/`, suíte E2E Playwright na raiz).
- **Framework verificado:** `playwright.config.ts` (raiz) + `package.json` com `@playwright/test`/`@seontechnologies/playwright-utils`; backend com `OniBus.Api.Tests`/`OniBus.Domain.Tests` (xUnit v3 + Testcontainers.PostgreSql). Sem HALT — framework já existe (`bmad-testarch-framework` já executado, ver `framework-setup-progress.md`).
- **Modo de execução:** BMad-Integrated — spec da story 1.1 encontrada em `_bmad-output/implementation-artifacts/spec-1-1-...md` (status `done`, já implementada e revisada). Sem test-design dedicado para esta story.
- **Flags TEA:** `tea_use_playwright_utils=true`, `tea_use_pactjs_utils=true` (gate de relevância fechado — sem Pact real neste run), `tea_pact_mcp=mcp`, `tea_browser_automation=auto`, `test_stack_type=auto`.
- **Conhecimento carregado:** tier core (`test-levels-framework`, `test-priorities-matrix`, `data-factories`, `selective-testing`, `ci-burn-in`, `test-quality`) + perfil Full UI+API do Playwright Utils (testes de UI já existem em `e2e/ui`).

## Step 2: Coverage Plan

**Cobertura já existente (não duplicar):**

| Nível | Arquivo | Cobre |
|---|---|---|
| E2E API (Playwright) | `e2e/api/rotas.spec.ts` | `[P0]` `GET /api/rotas` 200 + shape (FR-1) |
| E2E UI (Playwright) | `e2e/ui/listar-rotas.spec.ts` | `[P0]` lista renderiza; `[P2]` 5xx nunca vira "sem resultados" (AD-12) |
| Integração (.NET/xUnit) | `GetRotasTests.cs` | 200 com seed mínimo; ordenação com 2+ Rotas |
| Integração (.NET/xUnit) | `RotaSeedTests.cs` | idempotência do seed a nível de aplicação (chamar `SeedAsync` 2x) |
| Unit (Vitest) | `client.test.ts` | sucesso 200; 5xx → `ApiComunicacaoError`; 404 → `ApiRespostaInesperadaError` |
| Unit (Vitest) | `ListaDeRotas.test.tsx` | sucesso; falha de comunicação; lista vazia sem alerta |

**Gaps identificados (targets desta expansão):**

| # | Nível | Prioridade | Alvo | Por quê é um gap real |
|---|---|---|---|---|
| 1 | Integração (.NET, `OniBus.Api.Tests`) | P0 | `ux_reservas_codigo` rejeita `Codigo` duplicado no banco | AD-6: índice existe desde a 1ª migração, sem endpoint de reserva para exercitá-lo ainda — só um teste direto contra o banco pega uma regressão na migração |
| 2 | Integração (.NET) | P0 | `ux_reservas_viagem_assento_confirmada` (parcial) rejeita 2ª `Confirmada` no mesmo `(ViagemId, NumeroAssento)`, mas permite quando status ≠ `Confirmada` | AD-7: o filtro parcial é o ponto inteiro do índice; um índice único sem o filtro seria uma mudança de comportamento silenciosa |
| 3 | Integração (.NET) | P0 | `ux_rotas_origem_destino` rejeita insert duplicado direto (bypass do check de `RotaSeed`) | Prova a garantia do banco citada no Spec Change Log, não só a lógica de `SeedAsync` (que já tem teste próprio) |
| 4 | Integração (.NET) | P1 | `GetRotas.HandleAsync` arredonda minutos (`Math.Round`), não trunca | Regressão do fix do Spec Change Log item 5; hoje só durações exatas (330, 1200 min) são testadas |
| 5 | Unit (Vitest, `client.ts`) | P1 | `fetch` lançando (rede/DNS fora) → `ApiComunicacaoError` | Caminho `try/catch` ao redor do próprio `fetch` (linhas 31-36 de `client.ts`) nunca é exercitado — só os caminhos por status (200/5xx/404) são |
| 6 | Unit (Vitest, `client.ts`) | P1 | 2xx com corpo JSON inválido → `ApiComunicacaoError`, não `SyntaxError` cru | Defeito corrigido no Spec Change Log item 6, sem teste até agora |
| 7 | Unit (Vitest, `ListaDeRotas.tsx`) | P2 | Estado `carregando` (`role="status"`, "Carregando rotas…") antes do fetch resolver | Um dos 4 estados do AD-12; hoje é o único nunca testado |
| 8 | Unit (Vitest, `ListaDeRotas.tsx` via render) | P2 | `formatarDuracao`: hora exata (`60`→`1h`) e sub-hora (`45`→`45min`) | Só o ramo misto (`330`→`5h30`) é exercitado hoje |

**Decisão de escopo — E2E/API Playwright:** zero targets novos. As duas jornadas críticas do I/O matrix da story (lista com sucesso; falha de comunicação nunca é "vazio") já estão cobertas em `e2e/`. Os gaps 1-8 são todos de nível unit/integração — duplicá-los em Playwright violaria a regra "avoid duplicate coverage across test levels" (Step 2, regra mandatória) e o próprio registro de decisão do usuário em `framework-setup-progress.md` (E2E é camada de smoke acima de AD-10, não espelho das duas camadas já fixadas).

**Justificativa da prioridade:** P0 nos três índices únicos porque são invariantes de schema referenciadas como "frozen intent" na story (AD-6/AD-7) e citadas na Suggested Review Order — uma regressão aqui só apareceria muito mais tarde, quando a story de Reserva introduzir `POST /reservas`. P1 nos caminhos de erro de `client.ts` e no arredondamento porque são defeitos que já ocorreram uma vez (ambos citados no Spec Change Log) e não têm teste de regressão. P2 nos dois itens de UI puramente de apresentação (estado de carregamento, formatação de duração) — comportamento correto, mas menor risco/impacto.

**Worker dispatch (Step 3):** subagent backend (.NET/xUnit) para os targets 1-4. Targets 5-8 (Vitest) não têm um worker dedicado na matriz desta skill (subagent E2E é escopo Playwright-only; subagent backend é escopo do serviço backend) — tratados diretamente pelo orquestrador nesta run, mesmo padrão de transparência de escopo já usado no projeto.

## Step 3/3C: Geração e Agregação

**Resolução de modo de execução:**
- Requested: `auto` (config `tea_execution_mode`)
- Probe enabled: `true`
- Resolved: `subagent` (runtime suporta subagent dispatch via Agent tool; sem suporte a agent-team neste ambiente)

**Subagentes despachados:**
- ✅ Subagent B-backend (.NET/xUnit) — targets 1-4. Rodou `dotnet build` + `dotnet test backend/OniBus.Api.Tests` com Testcontainers reais (Docker disponível), corrigiu e confirmou tudo verde antes de reportar.
- ⏭️ Subagent A (API/Playwright) e Subagent B (E2E/Playwright) — **não despachados nesta run.** Coverage plan (Step 2) concluiu zero targets novos em nível Playwright: as duas jornadas críticas do I/O matrix já estão cobertas por `e2e/api/rotas.spec.ts` e `e2e/ui/listar-rotas.spec.ts`. Despachar workers sem alvo violaria a regra "avoid duplicate coverage across test levels" do Step 2. Decisão documentada aqui em vez de silenciosa.
- 🧑‍💻 Targets 5-8 (Vitest, frontend) — sem worker dedicado na matriz desta skill para testes de componente/unit do frontend (subagent E2E é Playwright-only; subagent backend é escopo do serviço backend/.NET). Implementados diretamente pelo orquestrador nesta run.

**Arquivos criados:**
- `backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs` — 4 testes de integração (Testcontainers Postgres real, sem SQLite) provando que os três índices únicos vivem no schema, não só na aplicação: `ux_rotas_origem_destino`, `ux_reservas_codigo`, `ux_reservas_viagem_assento_confirmada` (rejeita duplicata Confirmada, permite duplicata quando uma das duas não é Confirmada — prova o filtro parcial).

**Arquivos modificados:**
- `backend/OniBus.Api.Tests/Features/Rotas/GetRotasTests.cs` — `+1` teste: duração fracionária (330min40s) arredonda para 331, nunca trunca para 330 (regressão do item 5 do Spec Change Log). Os dois testes pré-existentes não foram tocados.
- `web/src/shared/api/client.test.ts` — `+2` testes: `fetch` rejeitado (rede/DNS) → `ApiComunicacaoError`; 200 com corpo JSON inválido → `ApiComunicacaoError` (não `SyntaxError` cru, regressão do item 6 do Spec Change Log).
- `web/src/features/busca/ListaDeRotas.test.tsx` — `+2` testes: estado `carregando` (`role="status"`, "Carregando rotas…") antes do fetch resolver; `formatarDuracao` em hora exata (`60`→`1h`) e sub-hora (`45`→`45min`).

**Fixtures/infra:** nenhuma nova necessária — todos os testes reaproveitam os fixtures/padrões já existentes (`PostgreSqlContainer` + `WebApplicationFactory` no backend; `vi.spyOn`/`vi.stubGlobal` + `cleanup()` no frontend). Nenhum `merged-fixtures.ts` alterado (sem Playwright novo).

**Playwright Utils deviations:** Nenhuma (sem geração Playwright nesta run).
**Pact.js Utils deviations:** Nenhuma (gate de relevância permanece fechado).

## Step 4: Validação Final

**Execução real (não só leitura de checklist):**
- `dotnet test backend/OniBus.sln` → `OniBus.Domain.Tests`: 1/1 ✅; `OniBus.Api.Tests`: 8/8 ✅ (3 pré-existentes + 4 índices + 1 arredondamento). Testcontainers usou Docker real, sem SQLite.
- `npm run test -- --run` (dentro de `web/`) → 10/10 ✅ (6 pré-existentes + 2 `client.test.ts` + 2 `ListaDeRotas.test.tsx`).
- `npm run lint` (oxlint) → sem erros.
- `npm run build` (`tsc -b && vite build`) → build limpo.
- Suíte E2E Playwright (`e2e/`) — não alterada nesta run (zero targets novos identificados no Step 2); não re-executada porque nenhum arquivo sob `e2e/` mudou.

**Resumo por nível e prioridade:**

| Nível | Novos testes | P0 | P1 | P2 |
|---|---|---|---|---|
| Integração .NET (xUnit) | 5 | 3 | 2 | 0 |
| Unit Vitest (`client.ts`) | 2 | 0 | 2 | 0 |
| Unit Vitest (`ListaDeRotas.tsx`) | 2 | 0 | 0 | 2 |
| **Total** | **9** | **3** | **4** | **2** |

**Riscos/suposições residuais:**
- `StatusReserva` só tem dois valores (`Confirmada`, `Cancelada`) hoje — o teste do índice parcial usa `Cancelada` como "status diferente"; se um terceiro status for adicionado depois, o teste continua válido mas não cobre esse novo valor especificamente.
- Nenhuma decisão de arquitetura (AD-1 a AD-17) foi tocada; nenhum código de produção foi alterado, só testes.

**Próximo workflow recomendado:** `bmad-testarch-test-review` (revisar qualidade dos testes novos) ou `bmad-testarch-trace` (matriz de rastreabilidade agora que os três índices AD-6/AD-7/AD-15 têm cobertura formal) antes da Story 1.4 (seed completo) ou da story que introduz `POST /reservas` (vai exercitar `ux_reservas_*` via HTTP pela primeira vez).
