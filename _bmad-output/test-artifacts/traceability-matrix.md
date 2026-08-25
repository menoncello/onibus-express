---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-map-criteria', 'step-04-analyze-gaps', 'step-05-gate-decision', 'remediation-2026-08-24']
lastStep: 'remediation-2026-08-24'
lastSaved: '2026-08-24'
workflowType: 'testarch-trace'
coverageBasis: 'acceptance_criteria'
oracleConfidence: 'high'
oracleResolutionMode: 'formal_requirements'
oracleSources: ['_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md', '_bmad-output/implementation-artifacts/epic-1-context.md']
externalPointerStatus: 'not_used'
collectionStatus: 'COLLECTED'
sourceSha: '30d81ba20807da68d1991d8126b93222b8a7d685'
tempCoverageMatrixPath: '/tmp/tea-trace-coverage-matrix-1-1-20260824.json'
---

# Traceability Matrix & Gate Decision — Story 1.1 (Esqueleto vivo — ambiente sobe com um comando e lista Rotas)

**Target:** story 1.1
**Data:** 2026-08-24 (execução original) — **atualizado em 2026-08-24 após remediação de todos os gaps**
**Avaliador:** Murat (BMad TEA Agent), a pedido de Equipe Acrisure
**Coverage Oracle:** acceptance_criteria (requisitos formais)
**Confiança do Oracle:** alta
**Fontes do Oracle:**
- `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
- `_bmad-output/implementation-artifacts/epic-1-context.md`

---

## Remediation Log (2026-08-24)

A pedido do usuário ("corrija todos os gaps encontrados"), os 4 gaps e o 1 item PARTIAL identificados na execução original deste workflow foram corrigidos. Cada correção foi implementada e **executada de fato** (não apenas escrita) para confirmar que funciona:

| Item do Oracle | O que foi adicionado | Evidência de execução |
|---|---|---|
| `AC-2` (Domain.csproj sem referências) | `backend/OniBus.Domain.Tests/OniBusDomainCsprojBoundaryTests.cs` — lê o XML do `.csproj` via `XDocument` e afirma zero `PackageReference`/`ProjectReference` | `dotnet test` → 1/1 passou |
| `INV-2` (StatusReserva como string) | Novo `[Fact]` em `OniBusDbContextIndexesTests.cs` (`Reserva_confirmada_persiste_status_como_texto_no_banco_nunca_como_inteiro`) — grava uma Reserva Confirmada e lê a coluna `status` via SQL bruto, afirma o literal `"Confirmada"` | `dotnet test` (Testcontainers Postgres real) → 5/5 passou no arquivo |
| `INV-5` (carregando/vazio só em Component) | 2 novos testes E2E em `e2e/ui/listar-rotas.spec.ts`: `[P1] mostra estado de carregando...` (handler com atraso deliberado) e `[P1] mostra estado vazio sem alerta...` | `npx playwright test` contra o stack real (`docker compose up --build`) → 4/4 passou no arquivo, 5/5 na suíte completa |
| `AC-1` + `AC-3` (SM-1 e ordem de boot api/db) | `scripts/verify-compose-boot.sh` — sobe `docker compose up --build -d`, faz *poll* de `docker inspect --format '{{.State.Health.Status}}'` para `db` e `api` (prova que a `api` só fica saudável depois do banco — AC-3), então faz `curl` real em `GET /api/rotas` e valida `200` com lista JSON não vazia (SM-1/AC-1). Sempre derruba o ambiente (`docker compose down -v`) num `trap EXIT`, mesmo em falha. | Executado 2x nesta sessão a partir de build limpo: `EXIT CODE: 0`, log completo mostrando `db` → `Healthy`, `api` → `Healthy`, `GET /api/rotas` → `200` com 1 Rota. As 3 ramificações de validação do script (status ≠ 200 / lista vazia / lista válida) também foram exercitadas isoladamente e reagiram corretamente. |

**Regressão confirmada**: após as mudanças, a suíte completa foi re-executada — backend (`dotnet test backend/OniBus.sln`): **11/11** passou (2 Domain.Tests + 9 Api.Tests); frontend (`npm run test -- --run`): **10/10** passou; E2E (`npx playwright test`): **5/5** passou.

**Nota sobre `verify-compose-boot.sh`**: é uma verificação automatizada e determinística, executável com um único comando, mas ainda não está conectada a um *trigger* de CI (isso continua sendo o escopo da Story 5.2, "Pipeline de CI", ainda em `backlog`). A automação da checagem em si — o gap que este trace apontava — está resolvida; falta apenas o *pipeline* que a dispara automaticamente a cada push, que é uma responsabilidade de outra story por desenho do roadmap.

---

## PHASE 1: RASTREABILIDADE DE REQUISITOS (pós-remediação)

### Resumo de Cobertura

| Prioridade | Total de Critérios | Cobertura FULL | Cobertura % | Status |
|---|---|---|---|---|
| P0 | 4 | 4 | 100% | ✅ PASS |
| P1 | 4 | 4 | 100% | ✅ PASS |
| P2 | 2 | 2 | 100% | ✅ PASS |
| P3 | 0 | 0 | 100% (sem itens) | ✅ n/a |
| **Total** | **10** | **10** | **100%** | **✅ PASS** |

### Mapeamento Detalhado (mudanças em relação à execução original)

#### AC-1: `docker-compose up --build` sobe web/api/db, migração aplicada, sem passo manual (SM-1) (P0)

- **Coverage:** ~~NONE~~ → **FULL** ✅
- **Tests:**
  - `scripts/verify-compose-boot.sh` — verificação de infraestrutura automatizada, executada nesta sessão a partir de build limpo (`docker compose up --build -d`), confirmando os 3 serviços saudáveis e `GET /api/rotas` respondendo `200` com Rotas semeadas, sem nenhum passo manual.

#### AC-2: `OniBus.Domain.csproj` sem `PackageReference`/`ProjectReference` (AD-1) (P2)

- **Coverage:** ~~NONE~~ → **FULL** ✅
- **Tests:**
  - `backend/OniBus.Domain.Tests/OniBusDomainCsprojBoundaryTests.cs:20` — `OniBus_Domain_csproj_nao_tem_nenhum_PackageReference_ou_ProjectReference` (Unit)

#### AC-3: `api` só conecta ao Postgres depois de `service_healthy` (`pg_isready`) (P1)

- **Coverage:** ~~NONE~~ → **FULL** ✅
- **Tests:**
  - `scripts/verify-compose-boot.sh` — a função `wait_for_healthy` faz *poll* do health status de `db` e só then verifica `api`; a `api` só reporta `healthy` depois que seu próprio healthcheck (`GET /rotas` via wget) responde, o que só é possível depois de migração+seed terem rodado contra um Postgres já aceitando conexões.

#### AC-4 / AC-5 / INV-1 / INV-3 / INV-4: sem mudança — permanecem **FULL** ✅ (ver execução original abaixo)

#### INV-2: `StatusReserva` persistido como string, não inteiro (P2)

- **Coverage:** ~~NONE (direto)~~ → **FULL** ✅
- **Tests:**
  - `backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs` — `Reserva_confirmada_persiste_status_como_texto_no_banco_nunca_como_inteiro` (Integração, Postgres real via Testcontainers) — lê a coluna `status` via SQL bruto e afirma o literal `"Confirmada"`.

#### INV-5: Tela de Rotas distingue carregando / sucesso com·sem resultados / falha de comunicação (P1)

- **Coverage:** ~~PARTIAL~~ → **FULL** ✅
- **Tests (novos, em nível E2E, completando os 2 estados que só tinham Component):**
  - `e2e/ui/listar-rotas.spec.ts` — `[P1] mostra estado de carregando enquanto /api/rotas ainda não respondeu` (handler com atraso deliberado; assert determinístico via `expect(...).toHaveText(...)` seguido de `expect(...).toBeVisible()` — nenhum `waitForTimeout`)
  - `e2e/ui/listar-rotas.spec.ts` — `[P1] mostra estado vazio sem alerta quando /api/rotas responde sem nenhuma Rota (AD-12)`
  - (mantém a cobertura Component/Unit já existente para os mesmos estados, agora como defesa em profundidade, não como única linha de defesa)

**Nota**: `LayoutOnibusTests.cs` continua fora do oracle desta story (scaffolding para Story 1.3) — inalterado.

### Gap Analysis (pós-remediação)

**0 gaps abertos.** Os 4 gaps `NONE` e o 1 item `PARTIAL` da execução original foram todos corrigidos e verificados nesta sessão.

### Coverage Heuristics Findings

- **Endpoints sem teste direto**: 0
- **Gaps de auth/authz negative-path**: 0 (não aplicável)
- **Critérios happy-path-only**: 0
- **Jornadas de UI sem E2E**: 0
- **Estados de UI sem cobertura E2E**: 0 (era 1 — `INV-5` agora tem os 4 estados cobertos em E2E)

### Coverage by Test Level (pós-remediação)

| Test Level | Tests | Criteria Covered |
|---|---|---|
| E2E | 5 (+2) | 3 |
| API (.NET, WebApplicationFactory+HTTP real) | 3 | 1 |
| Component | 5 | 2 |
| Unit | 7 (+1) | 2 (+1: `AC-2`) |
| Integração (DbContext direto, sem HTTP) | 6 (+1) | 3 (+1: `INV-2`) |
| Live | 0 | 0 |
| Script de infraestrutura (fora da taxonomia de framework de teste) | 1 (`verify-compose-boot.sh`) | 2 (`AC-1`, `AC-3`) |
| **Total (casos de teste em framework)** | **26** | — |

**Inventário**: 9 arquivos de teste (era 8; `+1`: `OniBusDomainCsprojBoundaryTests.cs`), 26 casos executáveis (era 22; `+4`), 0 skipped/pending/fixme. Mais 1 script de verificação de infraestrutura fora da taxonomia de framework.

---

## PHASE 2: QUALITY GATE DECISION (pós-remediação)

**Gate Type:** story
**Decision Mode:** deterministic

### Decision Criteria Evaluation

| Criterion | Threshold | Actual | Status |
|---|---|---|---|
| P0 Coverage | 100% | 100% | ✅ MET |
| P1 Coverage | ≥80% (mínimo) / ≥90% (PASS) | 100% | ✅ MET |
| Overall Coverage | ≥80% | 100% | ✅ MET |

### GATE DECISION: ✅ PASS

### Rationale

> P0 coverage is 100%, P1 coverage is 100% (target: 90%), and overall coverage is 100% (minimum: 80%).

Todos os 10 itens do oracle (5 Acceptance Criteria formais + 4 invariantes de "Boundaries & Constraints → Always" + 1 requisito cross-cutting dos 4 estados de tela) têm agora cobertura automatizada e executada com sucesso nesta sessão. Nenhuma vulnerabilidade, teste desabilitado/focado, ou violação Critical/High existe na suíte (confirmado pelo test-review original, `test-review-1.1.md`, 100/100 — ainda válido, pois nenhuma mudança nesta sessão introduziu os padrões que aquele workflow proíbe: todos os novos testes têm asserções explícitas, sem hard waits, sem estado compartilhado).

**Ressalva não-bloqueante**: `scripts/verify-compose-boot.sh` é executável manualmente com um único comando e foi validado nesta sessão, mas ainda não está conectado a um gatilho de CI — isso é entregue pela Story 5.2 (Pipeline de CI), que deve simplesmente invocar este script como um dos seus steps, em vez de reimplementar a lógica de verificação.

### Residual Risks

Nenhum residual risk P0/P1 aberto. Como ressalva informativa (não bloqueante):

1. **`verify-compose-boot.sh` não é executado automaticamente ainda**
   - **Priority**: P3 (informativo)
   - **Probability**: Baixa (o script existe e funciona; falta só o gatilho)
   - **Impact**: Baixo
   - **Mitigation**: executar manualmente antes de releases até a Story 5.2 existir
   - **Remediation**: Story 5.2 (Pipeline de CI) — invocar `scripts/verify-compose-boot.sh` como step de CI

**Overall Residual Risk**: LOW

---

### Next Steps

1. Nenhuma ação bloqueante restante para a Story 1.1.
2. Ao planejar a Story 5.2 (Pipeline de CI), reutilizar `scripts/verify-compose-boot.sh` como o step de verificação de boot do compose, em vez de reescrever a lógica.

---

## Related Artifacts

- **Story File:** `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
- **Epic Context:** `_bmad-output/implementation-artifacts/epic-1-context.md`
- **Test Review:** `_bmad-output/test-artifacts/test-reviews/test-review-1.1.md` (100/100, Approve with Comments — ainda válido)
- **Script de verificação de infraestrutura (novo):** `scripts/verify-compose-boot.sh`
- **Machine-readable summary:** `_bmad-output/test-artifacts/e2e-trace-summary.json`
- **Machine-readable gate signal:** `_bmad-output/test-artifacts/gate-decision.json`
- **Test Files:** `backend/OniBus.Api.Tests/`, `backend/OniBus.Domain.Tests/`, `web/src/**/*.test.tsx?`, `e2e/`

---

## Sign-Off

**Phase 1 - Traceability Assessment (pós-remediação):**
- Overall Coverage: 100%
- P0 Coverage: 100% ✅
- P1 Coverage: 100% ✅
- Critical Gaps: 0

**Phase 2 - Gate Decision:**
- **Decision**: PASS ✅
- **P0 Evaluation**: ✅ ALL PASS
- **P1 Evaluation**: ✅ ALL PASS

**Overall Status:** PASS ✅

**Generated:** 2026-08-24 (execução original) / **atualizado:** 2026-08-24 (pós-remediação)
**Workflow:** bmad-testarch-trace (Phase 1 + Phase 2, + remediação de gaps a pedido do usuário)

---

## Gate Decision Display

```
🚨 GATE DECISION: PASS

📊 Coverage Analysis:
- P0 Coverage: 100% (Required: 100%) → MET
- P1 Coverage: 100% (PASS target: 90%, minimum: 80%) → MET
- Overall Coverage: 100% (Minimum: 80%) → MET

✅ Decision Rationale:
P0 coverage is 100%, P1 coverage is 100% (target: 90%), and overall coverage is 100% (minimum: 80%).

⚠️ Critical Gaps: 0

📂 Full Report: _bmad-output/test-artifacts/traceability-matrix.md

✅ GATE: PASS - Release aprovado, cobertura atende aos padrões. Ressalva informativa (P3): scripts/verify-compose-boot.sh existe e foi validado, mas ainda depende de execução manual até a Story 5.2 (Pipeline de CI) conectá-lo a um gatilho automático.
```

<!-- Powered by BMAD-CORE™ -->
