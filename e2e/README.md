# E2E — OniBus Express

Suíte Playwright que exercita o sistema completo (frontend + API + Postgres) rodando de verdade, como uma camada **adicional** acima das duas já fixadas pela arquitetura (AD-10: `OniBus.Domain.Tests` puro e `OniBus.Api.Tests` com `WebApplicationFactory`/Testcontainers). Não substitui nenhuma das duas.

## Setup

```bash
nvm use          # Node 24 (.nvmrc)
npm install
docker compose up --build -d   # sobe web:80, api:8080, db:5432
cp .env.example .env           # opcional — os defaults já funcionam
```

## Rodando os testes

```bash
npm run test:e2e            # headless, contra http://localhost (BASE_URL)
npm run test:e2e:ui         # Playwright UI mode (interativo, útil pra debugar)
npx playwright test --headed        # com navegador visível
npx playwright test --debug         # inspector passo a passo
npm run test:e2e:report     # reabre o último relatório HTML
```

`BASE_URL` tem fallback para `http://localhost` (porta 80 do serviço `web` no compose). Sobrescreva via env var para apontar a outra instância (ex.: ambiente sem Docker do DR-8, `http://localhost:5173`).

## Arquitetura da suíte

```
e2e/
  api/               # testes que só chamam a API (apiRequest, sem browser)
  ui/                 # testes que dirigem o browser
  support/
    merged-fixtures.ts   # único ponto de import de `test`/`expect` — nunca `@playwright/test` direto
```

- **Fixtures**: `merged-fixtures.ts` combina `apiRequest`, `interceptNetworkCall`, `network-error-monitor` e `recurse` de `@seontechnologies/playwright-utils` via `mergeTests`. **Sem fixture de auth**: o sistema não tem autenticação, contas nem sessão (Non-Goal do PRD) — o Código de Reserva é identificador de negócio, não credencial.
- **Sem data factories ainda**: os endpoints de Reserva/Passageiro (FR-6, FR-7) ainda não existem no código — só `GET /rotas` (Story 1.1). Geradores de CPF válido e fábrica de Passageiro entram quando essa Story chegar, via `bmad-testarch-atdd`, para não criar abstração sem teste que a exercite.
- **Seletores**: por `role`/`aria-label`/texto visível, seguindo o padrão que o próprio frontend já usa (`ListaDeRotas.tsx` não tem `data-testid`). Se o frontend adotar `data-testid` no futuro, os testes migram junto.

## Boas práticas usadas aqui

- `interceptNetworkCall` **antes** de `page.goto`, nunca `page.route`/`page.waitForResponse` direto numa chamada da própria aplicação (mandate do `playwright-utils`).
- `apiRequest` para chamadas HTTP em teste — nunca `request.get/post` cru nem `fetch` manual.
- Nenhum `page.waitForTimeout`/`sleep` como sincronização — usar `recurse` ou aguardar a resposta interceptada.
- Um teste por regra de negócio, nomeado pela regra e sua fronteira (convenção do `ARCHITECTURE-SPINE.md`), não pelo método.
- Todo teste tem tag de prioridade (`[P0]`, `[P1]`, `[P2]`) no título.

## CI

Ainda não existe `.github/workflows/ci.yml` no repositório (é escopo de uma story futura / do skill `bmad-testarch-ci`). Quando for criado, o job de E2E deve:

1. `docker compose up --build -d` e esperar os três serviços saudáveis (o `healthcheck` de cada um já existe no compose).
2. `npm ci && npx playwright install --with-deps chromium`.
3. `npm run test:e2e`.
4. Publicar `playwright-report/` e `test-results/results.xml` como artifact.

Isso é **adicional** ao pipeline já planejado em AD-10 (`dotnet test` + `vitest run`), não o substitui.

## Hook de enforcement (write-time)

`.claude/hooks/tea-enforce.cjs` roda em `PreToolUse`/`PostToolUse`/`Stop` (configurado em `.claude/settings.json`) e bloqueia padrões proibidos (`.only` commitado, `waitForTimeout`/sleep como sincronização, `page.route` em chamada própria da app, etc.) no momento da escrita, antes de chegar a code review. A severidade de cada regra vem do `criteria-registry.md` do skill `bmad-testarch-test-review`, não do hook em si. Os globs cobertos estão em `.tea/enforce-config.json` (`testGlobs`): a suíte E2E (`e2e/**/*.spec.{ts,js}`) e os testes de componente do frontend (`web/src/**/*.test.{ts,tsx,js,jsx}`).

Para desligar uma regra específica: adicione o id em `disabledRules` em `.tea/enforce-config.json` **e explique o porquê no commit** — o hook falha aberto (nunca bloqueia por erro interno seu), mas uma regra desligada sem explicação é a primeira coisa a ser deletada por quem não sabe por que ela existe.

## Base de conhecimento

Fragments usados neste scaffold, em `.claude/skills/bmad-testarch-framework/resources/knowledge/`: `playwright-utils-mandate.md`, `overview.md`, `fixtures-composition.md`, `api-request.md`, `intercept-network-call.md`, `network-error-monitor.md`, `recurse.md`, `log.md`, `playwright-config.md`, `data-factories.md`, `pactjs-utils-mandate.md` (gate de relevância, fechado neste projeto).
