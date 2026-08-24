---
stepsCompleted: ['step-01-load-context', 'step-02-discover-tests', 'step-03-quality-evaluation', 'step-03f-aggregate-scores', 'step-04-generate-report']
lastStep: 'step-04-generate-report'
lastSaved: '2026-08-24'
workflowType: 'testarch-test-review'
inputDocuments:
  - '_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md'
  - '_bmad-output/implementation-artifacts/epic-1-context.md'
  - '.claude/skills/bmad-testarch-test-review/steps-c/criteria-registry.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/data-factories.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-levels-framework.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/selective-testing.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/test-healing-patterns.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/selector-resilience.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/timing-debugging.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/playwright-utils-mandate.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/overview.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/api-request.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/network-recorder.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/auth-session.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/intercept-network-call.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/recurse.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/log.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/file-utils.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/burn-in.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/network-error-monitor.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/fixtures-composition.md'
  - '.claude/skills/bmad-testarch-test-review/resources/knowledge/playwright-cli.md'
---

# Test Quality Review: Story 1.1 — Esqueleto vivo (suite completa)

**Quality Score**: 100/100 (A — Excelente)
**Review Date**: 2026-08-24
**Review Scope**: suite (toda a suíte de testes do repositório — primeira story, "esqueleto vivo")
**Reviewer**: Murat (BMad TEA Agent), a pedido de Equipe Acrisure

---

Nota: esta revisão audita os testes existentes; não gera novos testes.
Mapeamento e gates de cobertura estão fora do escopo aqui — use o workflow `trace` para decisões de cobertura.

## Nota Metodológica (Assunções Registradas)

Como esta é a Story 1.1 (primeira story do projeto), **toda a suíte de testes existente está dentro do conjunto revisado** — não há corpus fora do conjunto para servir de baseline de convenção (ver `criteria-registry.md` § Convention). As seguintes decisões foram tomadas por julgamento, na ausência do usuário, e registradas aqui em vez de bloquear a execução:

1. **Escopo = suite completa**, confirmado pelo usuário antes da execução.
2. **Contexto = spec da story 1.1** (`spec-1-1-esqueleto-vivo...md`) e `epic-1-context.md`, confirmado pelo usuário.
3. **Convention Baseline indisponível** (`baselineUnavailable: true`): não existem arquivos de teste fora do conjunto revisado para amostrar. Todas as linhas de Convenção (`priorityMarkers`, `testIds`, `bddNaming`, `assertionStyle`, `playwrightUtils`) recebem status `unknown` e pontuam `✅ PASS (n/a)` — nenhuma dedução foi aplicada a elas, conforme a tabela de status do registro.
4. **Coleta de evidência via CLI/MCP (step-02 §3) foi pulada**: não há uma URL/fluxo ao vivo sob revisão — esta é uma auditoria estática de arquivos de teste já commitados, não uma investigação de falha em execução.
5. **Execução sequencial das 4 dimensões de qualidade** (determinism/isolation/maintainability/performance): esta revisão rodou como um único agente (fork), sem permissão para invocar subagentes; as quatro dimensões foram avaliadas sequencialmente pelo mesmo revisor, aplicando o `criteria-registry.md` exatamente como cada worker especializado faria.

## Executive Summary

**Overall Assessment**: Excellent

**Recommendation**: Approve with Comments

**Context Basis**: pr_diff

**Context Waivers Applied**: 0

### Key Strengths

✅ Zero violações Absolute/Critical/High em toda a suíte — nenhum `.skip`/`.only`, nenhuma asserção tautológica, nenhum hard wait, nenhuma race condition de rede, nenhum estado compartilhado entre testes.
✅ Padrão network-first aplicado corretamente nos dois testes E2E de UI (`interceptNetworkCall` registrado *antes* de `page.goto`) — exatamente o padrão que `network-first.md` recomenda.
✅ Rastreabilidade explícita a decisões de arquitetura: comentários nos testes citam AD-6, AD-7, AD-10, AD-12, AD-15 e FR-1 diretamente, e um teste de regressão documenta um bug real já corrigido (arredondamento de duração, `GetRotasTests.cs:112-141`).
✅ Higiene de isolamento demonstrada por correção ativa: o teste de frontend registra em comentário que um vazamento de estado entre testes (`cleanup()` ausente) foi encontrado e corrigido durante esta própria story.
✅ Uso disciplinado de `playwright-utils`: fixtures compostas via `mergeTests` em `merged-fixtures.ts`, com um desvio documentado via `// playwright-utils deviation:` exatamente na forma que `playwright-utils-mandate.md` exige.

### Key Weaknesses

❌ `OniBusDbContextIndexesTests.cs` repete o literal `new Reserva { ... }` 6 vezes ao longo do arquivo, sem uma factory/helper equivalente ao `SemearViagemAsync` já existente no mesmo arquivo para `Rota`+`Viagem`.
❌ `ListaDeRotas.test.tsx` repete o mesmo formato de objeto `Rota` (`{ id, origem, destino, duracaoEstimadaMinutos }`) inline em 3 pontos do arquivo.
❌ Nenhuma convenção de Priority Marker, Test ID ou BDD naming pôde ser avaliada contra um padrão de casa — a suíte é pequena demais e é, ela mesma, a totalidade do corpus (ver Nota Metodológica acima). Não é uma falha dos testes, mas significa que a nota "A" desta revisão description carrega menos peso estatístico do que uma revisão feita sobre um repositório maduro.

### Summary

A suíte de testes da Story 1.1 é tecnicamente sólida: nenhuma violação Critical ou High foi encontrada em nenhuma das 9 unidades revisadas (4 arquivos de teste .NET/xUnit, 2 arquivos de teste Vitest, 2 specs Playwright E2E e 1 arquivo de fixtures compartilhadas). Os testes exercitam consistentemente o comportamento descrito na spec (índices únicos, idempotência do seed, arredondamento de duração, distinção entre lista vazia e falha de comunicação) e citam as decisões de arquitetura (AD-*) que justificam cada caso. As duas únicas violações encontradas — repetição de literais de domínio (`M2`, MEDIUM) em `OniBusDbContextIndexesTests.cs` e em `ListaDeRotas.test.tsx` — são de baixo risco e não ameaçam a confiabilidade da suíte; a recomendação é "Approve with Comments" porque a pontuação é > 0 apenas nessas duas linhas MEDIUM.

---

## Quality Criteria Assessment

| Criterion                             | Status              | Violations | Basis                                        | Notes                                                                              |
| -------------------------------------- | ------------------- | ---------- | --------------------------------------------- | ----------------------------------------------------------------------------------- |
| BDD Format (Given-When-Then)           | ✅ PASS (n/a)        | 0          | Convention: bddNaming (0 of 0 sampled)         | Baseline indisponível (suite = 100% do corpus); nomes observados são orientados a comportamento em toda a suíte |
| Test IDs                               | ✅ PASS (n/a)        | 0          | Convention: testIds (0 of 0 sampled)           | Baseline indisponível; locators usam role/text, não test-id                        |
| Priority Markers (P0/P1/P2/P3)         | ✅ PASS (n/a)        | 0          | Convention: priorityMarkers (0 of 0 sampled)   | Baseline indisponível; E2E já usa `[P0]`/`[P2]` no nome do teste, mas não há corpus externo para confirmar convenção de casa |
| Disabled or Focused Tests              | ✅ PASS              | 0          | Absolute                                       | Nenhum `.skip`, `xit`, `.only`, `fit` ou equivalente encontrado                     |
| Hard Waits (sleep, waitForTimeout)     | ✅ PASS              | 0          | Absolute                                       | Nenhum `waitForTimeout`/`sleep`/`cy.wait(n)` encontrado                             |
| Determinism (no conditionals)          | ✅ PASS              | 0          | Absolute + Applicability                       | Nenhum `if`/`try-catch` decidindo asserção; `DateTimeOffset.UtcNow` usado como dado opaco, nunca como limite de expiração |
| Isolation (cleanup, no shared state)   | ✅ PASS              | 0          | Absolute                                       | `afterEach` com `cleanup()`/`restoreAllMocks()`/`unstubAllGlobals()`; container Postgres fresco por teste (xUnit instancia a classe por `[Fact]`) |
| Fixture Patterns                       | ⚠️ WARN              | 2          | Applicability                                  | Ver M2 em Recommendations                                                          |
| Data Factories                         | ⚠️ WARN              | 2          | Applicability                                  | Mesmos 2 achados M2 (payload de domínio repetido sem factory)                      |
| Network-First Pattern                  | ✅ PASS              | 0          | Applicability                                  | `interceptNetworkCall` registrado antes de `page.goto` nos dois specs de UI E2E    |
| Playwright Utils Adoption              | ✅ PASS (n/a)        | 0          | Convention: playwrightUtils (0 of 0 sampled)   | Precondição ativa (flag `true` + `@seontechnologies/playwright-utils@4.4.0` instalado), mas baseline indisponível — ver nota abaixo |
| Pact.js Utils Adoption                 | ✅ PASS (n/a)        | 0          | Applicability                                  | Nenhum artefato Pact no repositório; `@seontechnologies/pactjs-utils` não está instalado |
| Explicit Assertions                    | ✅ PASS              | 0          | Absolute                                       | Toda `it`/`test`/`[Fact]` tem ao menos uma asserção alcançável                      |
| Test Length (≤1000 lines)              | ✅ PASS              | 0 (max 184)| Absolute                                       | Maior arquivo: `OniBusDbContextIndexesTests.cs`, 184 linhas                         |
| Test Duration (≤1.5 min)               | ✅ PASS              | 0          | Absolute                                       | Nenhuma evidência de hard wait ou violação de network-first; duração real não medida (revisão estática) |
| Flakiness Patterns                     | ✅ PASS              | 0          | Absolute + Applicability                       | Nenhum dos padrões de instabilidade do registro (H1–H4, M1, M6) encontrado          |

**Total Violations**: 0 Critical, 0 High, 2 Medium, 0 Low

**Convention Baseline**: unavailable: review scope is `suite` and covers 100% of the existing test corpus (Story 1.1 is the first story); no files exist outside the reviewed set to sample.

> **Nota run-level sobre Playwright Utils (obrigatória por `criteria-registry.md` § Mandate-backed keys):** `tea_use_playwright_utils` é `true` e `@seontechnologies/playwright-utils` (4.4.0) está instalado no `package.json` da raiz. A convenção de adoção não pôde ser medida (0 de 0 arquivos amostrados fora do conjunto revisado — a suíte inteira é o conjunto revisado). Dentro do próprio conjunto revisado, a adoção é completa: os dois specs E2E importam `test`/`expect` de `../support/merged-fixtures` (nunca de `@playwright/test` diretamente) e `merged-fixtures.ts` compõe `apiRequestFixture`, `interceptFixture`, `networkErrorFixture` e `recurseFixture` via `mergeTests`.

---

## Quality Score Breakdown

```
Starting Score:          100
Critical Violations:     -0 × 10 = -0
High Violations:         -0 × 5 = -0
Medium Violations:       -2 × 2 = -4
Low Violations:          -0 × 1 = -0

Bonus Points:
  Excellent BDD:         +5
  Comprehensive Fixtures: +0
  Data Factories:        +0
  Network-First:         +5
  Perfect Isolation:     +5
  All Test IDs:          +0
                         --------
Total Bonus:             +15

Final Score:             100/100
Grade:                   A
```

<!-- Nota de transparência: 100 - 4 + 15 = 111, clamped para 100 por Math.min(100, ...) conforme step-03f-aggregate-scores.md §3. O clamp é a razão pela qual as linhas acima somam 111 em vez de bater diretamente com o Final Score — a fórmula publicada é max(0, min(100, ...)), não uma soma sem teto. -->

**Bonus rationale**:
- **Excellent BDD (+5)**: nenhum nome de teste, em nenhum dos 9 arquivos, descreve implementação em vez de comportamento (nomes como `distingue falha de comunicacao de lista vazia`, `[P0] lista as Rotas semeadas (FR-1)`).
- **Comprehensive Fixtures (+0)** e **Data Factories (+0)**: os 2 achados M2 (payload de domínio repetido sem factory) impedem a bonificação — o critério exige que a propriedade valha para *todo* arquivo revisado.
- **Network-First (+5)**: os dois testes E2E de UI registram a interceptação antes de navegar; nenhuma outra parte da suíte navega em browser.
- **Perfect Isolation (+5)**: zero violações H4/C5; limpeza explícita (`cleanup`, `restoreAllMocks`, `unstubAllGlobals`) e containers de teste isolados por `[Fact]`.
- **All Test IDs (+0)**: a suíte usa locators de role/texto (`getByRole`, `getByText`, `screen.findByText`) em vez de `data-testid` — uma escolha legítima (não fere L1: role/label satisfaz o critério), mas não satisfaz literalmente o bônus, que exige `data-testid` em todo lookup.

---

## Critical Issues (Must Fix)

No critical issues detected. ✅

---

## Recommendations (Should Fix)

### 1. Extrair factory para o literal `Reserva` repetido

**Severity**: P2 (Medium)
**Location**: `backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs:95` (e também nas linhas 107, 125, 137, 155, 165)
**Row**: M2
**Criterion**: Data Factories / Fixture Patterns
**Knowledge Base**: [data-factories.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/data-factories.md)

**Issue Description**:
O mesmo formato de payload (`new Reserva { Id, ViagemId, NumeroAssento, Codigo, Status }`) é construído inline 6 vezes ao longo do arquivo, em 3 métodos de teste diferentes. O próprio arquivo já demonstra o padrão correto para `Rota`+`Viagem` via o helper privado `SemearViagemAsync` (linhas 40-62), mas não estende essa disciplina para `Reserva`.

**Current Code**:

```csharp
// ⚠️ Repetido 6x com pequenas variações de NumeroAssento/Codigo/Status
db.Reservas.Add(new Reserva
{
    Id = Guid.NewGuid(),
    ViagemId = viagem.Id,
    NumeroAssento = 1,
    Codigo = "AAA-00001",
    Status = StatusReserva.Confirmada,
});
```

**Recommended Improvement**:

```csharp
// ✅ Um helper local, no mesmo espírito de SemearViagemAsync
private static Reserva CriarReserva(Guid viagemId, int numeroAssento, string codigo, StatusReserva status) =>
    new()
    {
        Id = Guid.NewGuid(),
        ViagemId = viagemId,
        NumeroAssento = numeroAssento,
        Codigo = codigo,
        Status = status,
    };

// uso:
db.Reservas.Add(CriarReserva(viagem.Id, 1, "AAA-00001", StatusReserva.Confirmada));
```

**Benefits**:
Se um campo obrigatório for adicionado a `Reserva` no futuro (ex.: dados de Passageiro inline, mencionados no Spec Change Log como pendentes para a story de `POST /reservas`), apenas o helper precisa mudar — hoje, seriam 6 pontos de edição manual.

**Priority**:
P2: não é uma falha de comportamento nem gera flakiness; é uma dívida de manutenibilidade que cresce proporcionalmente ao número de testes de índice único que a Story 1.4+ provavelmente adicionará a este mesmo arquivo.

---

### 2. Extrair factory para o literal `Rota` repetido em `ListaDeRotas.test.tsx`

**Severity**: P2 (Medium)
**Location**: `web/src/features/busca/ListaDeRotas.test.tsx:21` (e também nas linhas 66-67)
**Row**: M2
**Criterion**: Data Factories / Fixture Patterns
**Knowledge Base**: [data-factories.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/data-factories.md)

**Issue Description**:
O formato `{ id, origem, destino, duracaoEstimadaMinutos }` é escrito inline 3 vezes no arquivo (uma vez no primeiro teste, duas vezes no teste de formatação de duração).

**Current Code**:

```typescript
// ⚠️ Repetido com formato idêntico, valores diferentes
vi.spyOn(rotasApi, 'getRotas').mockResolvedValue([
  { id: '1', origem: 'São Paulo', destino: 'Rio de Janeiro', duracaoEstimadaMinutos: 330 },
])
```

**Recommended Improvement**:

```typescript
// ✅ Uma factory local com overrides, no padrão de data-factories.md
const criarRota = (overrides: Partial<rotasApi.Rota> = {}): rotasApi.Rota => ({
  id: '1',
  origem: 'São Paulo',
  destino: 'Rio de Janeiro',
  duracaoEstimadaMinutos: 330,
  ...overrides,
})
```

**Benefits**:
Reduz o custo de adicionar um novo campo obrigatório ao tipo `Rota` no futuro (hoje, 3 pontos de edição manual neste único arquivo, mais os pontos equivalentes em `client.test.ts` e nos specs E2E).

**Priority**:
P2: arquivo pequeno e literal simples (DTO plano de 4 campos primitivos); o risco real é baixo, mas o padrão vale a pena estabelecer cedo, antes que a Story 1.2 (busca de Viagens) multiplique esse mesmo tipo de literal.

---

## Best Practices Found

### 1. Network-first corretamente aplicado nos testes E2E de UI

**Location**: `e2e/ui/listar-rotas.spec.ts:5-9` e `e2e/ui/listar-rotas.spec.ts:21-27`
**Pattern**: Network-First / `interceptNetworkCall`
**Knowledge Base**: [network-first.md tópicos em overview.md / intercept-network-call.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/intercept-network-call.md)

**Why This Is Good**:
A interceptação é registrada *antes* de `page.goto('/')`, eliminando a race condition onde a navegação dispara a chamada de rede antes que o teste esteja pronto para observá-la — exatamente o padrão que evita flakiness em testes de UI orientados a rede.

**Code Example**:

```typescript
// ✅ Intercepta antes de navegar
const rotasCall = interceptNetworkCall({ url: '**/api/rotas' });
await page.goto('/');
const { status, responseJson } = await rotasCall;
```

**Use as Reference**: Use este mesmo padrão em qualquer novo spec E2E que dependa de uma chamada de API disparada por navegação (ex.: Story 1.2, busca de Viagens).

### 2. Desvio de fixture documentado explicitamente

**Location**: `e2e/support/merged-fixtures.ts:8-10`
**Pattern**: `// playwright-utils deviation:` comment
**Knowledge Base**: [playwright-utils-mandate.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/playwright-utils-mandate.md)

**Why This Is Good**:
Em vez de simplesmente omitir `authFixture` (o que pareceria uma lacuna de cobertura), o arquivo documenta a decisão de não usá-lo com uma justificativa de negócio verificável ("o sistema não tem autenticação... Non-Goal explícito do PRD"). É exatamente o mecanismo de exceção que o mandate de playwright-utils prevê.

**Use as Reference**: Continue essa disciplina para qualquer futuro desvio de utilitário mandatado.

### 3. Correção ativa de vazamento de estado documentada no próprio teste

**Location**: `web/src/features/busca/ListaDeRotas.test.tsx:10-16`
**Pattern**: Isolamento explícito com `cleanup()` + `vi.restoreAllMocks()`
**Knowledge Base**: [test-quality.md](.claude/skills/bmad-testarch-test-review/resources/knowledge/test-quality.md)

**Why This Is Good**:
O comentário explica *por que* o `cleanup()` explícito é necessário (`globals: false` no Vitest desativa o auto-cleanup do Testing Library) em vez de apenas colar o código — isso evita que um mantenedor futuro remova o `afterEach` por parecer redundante.

---

## Test File Analysis

### Suite Overview

| File                                                                  | Lines | Framework      | Tests | Grouping                          |
| ---------------------------------------------------------------------- | ----: | -------------- | ----: | ---------------------------------- |
| `backend/OniBus.Api.Tests/Features/Rotas/GetRotasTests.cs`             |   142 | xUnit v3        |     3 | classe `GetRotasTests`             |
| `backend/OniBus.Api.Tests/Persistence/Seed/RotaSeedTests.cs`           |    44 | xUnit v3        |     1 | classe `RotaSeedTests`             |
| `backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs`  |   184 | xUnit v3        |     4 | classe `OniBusDbContextIndexesTests` |
| `backend/OniBus.Domain.Tests/Viagens/LayoutOnibusTests.cs`             |    12 | xUnit v3        |     1 | classe `LayoutOnibusTests`         |
| `web/src/features/busca/ListaDeRotas.test.tsx`                        |    76 | Vitest + RTL    |     5 | `describe('ListaDeRotas')`         |
| `web/src/shared/api/client.test.ts`                                   |    67 | Vitest          |     5 | `describe('getRotas ...')`         |
| `e2e/api/rotas.spec.ts`                                                |    29 | Playwright      |     1 | `test.describe('GET /api/rotas')`  |
| `e2e/ui/listar-rotas.spec.ts`                                          |    34 | Playwright      |     2 | `test.describe('Lista de Rotas')`  |
| `e2e/support/merged-fixtures.ts`                                       |    14 | Playwright (fixture, sem testes) |     0 | n/a — arquivo de composição de fixtures |

**Total**: 9 arquivos revisados, 22 testes executáveis, 602 linhas.

### Test Scope

- **Test IDs**: nenhum ID formal no padrão `1.1-E2E-001`; E2E usa `[P0]`/`[P2]` como prioridade inline no nome do teste.
- **Priority Distribution** (apenas onde marcado explicitamente, E2E): P0: 2 testes, P2: 1 teste. Demais 19 testes: sem marcador (convenção não estabelecida — ver Nota Metodológica).

### Assertions Analysis

- Backend (.NET): `Assert.*` (xUnit) usado de forma consistente em todos os 4 arquivos — nenhuma mistura de estilo.
- Frontend/E2E (TS): `expect(...)` (Vitest/Playwright) usado de forma consistente em todos os 5 arquivos — nenhuma mistura de estilo.
- Nenhuma asserção implícita (sem `expect`/`Assert`) encontrada; todo teste alcança pelo menos uma asserção verificável.

---

## Context and Integration

### What the Context Said

O contexto lido (`spec-1-1-esqueleto-vivo...md` e `epic-1-context.md`) estabelece um conjunto fechado de Acceptance Criteria e Architecture Decisions (AD-1, AD-6, AD-7, AD-10, AD-11, AD-12, AD-15) para esta story. Cruzando cada AD/critério citado no texto da spec com os testes revisados:

- **AD-6/AD-7 (índices únicos de reservas)** → cobertos por `OniBusDbContextIndexesTests.cs` (3 dos 4 testes exercitam exatamente os dois índices e o índice parcial).
- **AD-15 (idempotência do seed por chave natural)** → coberto por `RotaSeedTests.cs` (chama `SeedAsync` duas vezes, afirma 1 linha) e reforçado pelo índice único `ux_rotas_origem_destino` testado em `OniBusDbContextIndexesTests.cs`.
- **AD-10 (Testcontainers Postgres real, nunca SQLite in-memory)** → confirmado por leitura direta: todos os 4 arquivos de teste .NET usam `Testcontainers.PostgreSql`, nenhum usa provider in-memory.
- **AD-12 (estado vazio distinto de falha de comunicação, nunca vazio como erro)** → coberto tanto em `ListaDeRotas.test.tsx` (`mostra estado vazio sem alerta`, `distingue falha de comunicacao de lista vazia`) quanto em `e2e/ui/listar-rotas.spec.ts` (cenário 5xx com anotação explícita `skipNetworkMonitoring`).
- **FR-1 (GET /rotas lista as Rotas semeadas)** → coberto em `e2e/api/rotas.spec.ts` e `GetRotasTests.cs`, incluindo o caso de regressão de arredondamento de duração (330min40s → 331, não 330).
- **`OniBus.Domain.csproj` sem nenhuma referência (AD-1)** → não é testável por um teste de comportamento; a spec já documenta a verificação via `dotnet list ... package`/`reference` na seção Verification, fora do escopo desta revisão de testes automatizados.

Nenhuma contradição entre a spec e os testes foi encontrada. Um ponto notado sem ser uma falha: `LayoutOnibusTests.cs` testa uma constante (`LayoutOnibus.TotalAssentos == 44`) que nenhum endpoint desta story ainda consome — o próprio Code Map da spec já registra isso como "usada só por stories futuras", então não é uma lacuna, é scaffolding intencional para o Epic 1 (Stories 1.2–1.4).

### Related Artifacts

- **Story File**: [spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md](../../implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md)
- **Epic Context**: [epic-1-context.md](../../implementation-artifacts/epic-1-context.md)
- **Test Design**: nenhum documento de test design formal foi encontrado para esta story; não incorporado.

---

## Knowledge Base References

Esta revisão consultou os seguintes fragmentos da base de conhecimento:

- **test-quality.md** — Definition of Done (sem hard waits, ≤1000 linhas, auto-limpeza)
- **data-factories.md** — Factories com overrides, disciplina de setup via API
- **test-levels-framework.md** — Adequação de nível E2E vs API vs Componente vs Unidade
- **selective-testing.md** — Detecção de cobertura duplicada
- **test-healing-patterns.md** — Padrões de falha e correção
- **selector-resilience.md** — Estratégias de locator resiliente
- **timing-debugging.md** — Race conditions, wait determinístico, fake timers
- **playwright-utils-mandate.md**, **overview.md**, **api-request.md**, **intercept-network-call.md**, **recurse.md**, **auth-session.md**, **network-recorder.md**, **network-error-monitor.md**, **log.md**, **file-utils.md**, **burn-in.md**, **fixtures-composition.md** — perfil completo de `@seontechnologies/playwright-utils`
- **playwright-cli.md** — evidência de automação de browser (não aplicada nesta revisão estática)
- **criteria-registry.md** — registro único de critérios, severidades e gates

Não foram carregados fragmentos de Pact/contract-testing nem de Maestro/mobile: nenhum artefato dessas categorias existe no repositório.

Para mapeamento de cobertura, consulte a saída do workflow `trace`.

---

## Next Steps

### Immediate Actions (Before Merge)

Nenhuma ação bloqueante — não há violações Critical ou High. As duas ações abaixo são opcionais antes do merge e recomendadas para as próximas stories do épico:

1. **Extrair `CriarReserva` helper** — ver Recommendation 1
   - Priority: P2
   - Owner: time de backend
   - Estimated Effort: 10 minutos

2. **Extrair `criarRota` factory** — ver Recommendation 2
   - Priority: P2
   - Owner: time de frontend
   - Estimated Effort: 10 minutos

### Follow-up Actions (Future PRs)

1. **Estabelecer convenção de Priority Marker fora do E2E** — hoje só os specs Playwright usam `[P0]`/`[P2]`; à medida que a suíte cresce (Story 1.2+), decidir se essa convenção se estende a xUnit/Vitest e registrar a decisão para que a próxima revisão tenha um corpus externo para medir a convenção.
   - Priority: P3
   - Target: próxima story do épico

2. **Reavaliar `LayoutOnibusTests.cs`** quando a Story 1.3 (mapa de assentos) começar a consumir `LayoutOnibus.EstadoDosAssentos` — hoje o teste cobre só a constante `TotalAssentos`.
   - Priority: P3
   - Target: Story 1.3

### Re-Review Needed?

✅ No re-review needed - approve as-is (com os 2 comentários acima como melhoria opcional)

---

## Decision

**Recommendation**: Approve with Comments

**Rationale**:
A suíte não apresenta nenhuma violação Critical ou High — nenhum teste pode passar enquanto o comportamento estiver quebrado, nenhum teste está desabilitado ou focado, e o padrão network-first está corretamente aplicado onde se aplica. As duas violações MEDIUM (M2, payload de domínio repetido) são reais e valem a pena corrigir, mas não representam risco de flakiness, de falso positivo ou de manutenção urgente — daí "Approve with Comments" em vez de "Approve" puro, computado mecanicamente pela presença de `MEDIUM + LOW > 0` com score ≥ 70.

**For Approve with Comments**:

> Test quality is acceptable with 100/100 score. Duas recomendações P2 (extração de factories para `Reserva` e `Rota`) devem ser endereçadas quando conveniente, mas não bloqueiam o merge. Nenhuma issue crítica foi encontrada; os testes cobrem de forma rastreável as decisões de arquitetura da story (AD-6, AD-7, AD-10, AD-12, AD-15, FR-1).

---

## Appendix

### Violation Summary by Location

| Line                                  | Severity | Criterion               | Issue                                                    | Fix                                                       |
| -------------------------------------- | -------- | ------------------------ | --------------------------------------------------------- | ------------------------------------------------------------ |
| `OniBusDbContextIndexesTests.cs:95`    | P2       | M2 — Repeated literal    | `new Reserva{...}` repetido 6x (95, 107, 125, 137, 155, 165) | Extrair helper `CriarReserva(...)`                         |
| `ListaDeRotas.test.tsx:21`             | P2       | M2 — Repeated literal    | Literal `Rota` repetido 3x (21, 66, 67)                    | Extrair factory `criarRota(overrides)`                     |

### Related Reviews

| File                                                                  | Score       | Grade   | Critical | Status   |
| ------------------------------------------------------------------------ | ----------- | ------- | -------- | -------- |
| `backend/OniBus.Api.Tests/Features/Rotas/GetRotasTests.cs`                | 100/100     | A       | 0        | Approved |
| `backend/OniBus.Api.Tests/Persistence/Seed/RotaSeedTests.cs`              | 100/100     | A       | 0        | Approved |
| `backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs`     | 96/100*     | A       | 0        | Approved with Comments (M2 ×1) |
| `backend/OniBus.Domain.Tests/Viagens/LayoutOnibusTests.cs`                | 100/100     | A       | 0        | Approved |
| `web/src/features/busca/ListaDeRotas.test.tsx`                            | 96/100*     | A       | 0        | Approved with Comments (M2 ×1) |
| `web/src/shared/api/client.test.ts`                                       | 100/100     | A       | 0        | Approved |
| `e2e/api/rotas.spec.ts`                                                   | 100/100     | A       | 0        | Approved |
| `e2e/ui/listar-rotas.spec.ts`                                             | 100/100     | A       | 0        | Approved |
| `e2e/support/merged-fixtures.ts`                                          | 100/100     | A       | 0        | Approved |

\* Pontuação por arquivo mostrada antes do bônus agregado de suíte (que se aplica ao conjunto todo, não por arquivo); o score oficial desta revisão é o agregado de suíte (100/100) na seção Quality Score Breakdown, calculado uma única vez sobre a suíte completa.

**Suite Average**: 100/100 (A) — score agregado único (ledger da suíte), não a média aritmética da coluna acima.

---

## Review Metadata

**Generated By**: BMad TEA Agent (Test Architect) — Murat
**Workflow**: testarch-test-review
**Review ID**: test-review-1.1-20260824
**Timestamp**: 2026-08-24
**Version**: 1.0

---

## Feedback on This Review

Se houver dúvidas ou feedback sobre esta revisão:

1. Revisar os padrões na base de conhecimento: `.claude/skills/bmad-testarch-test-review/resources/knowledge/`
2. Consultar `tea-index.csv` para orientação detalhada
3. Solicitar esclarecimento sobre violações específicas
4. Emparelhar com um engenheiro de QA para aplicar os padrões

Esta revisão aplica o rubric de forma consistente. O contexto pode revelar achados adicionais e esclarecer impacto; ele não pode dispensar uma violação, mudar a severidade ou alterar a pontuação. Aceitação formal de risco pertence ao `trace` ou ao gate de release.

---

## Reviewed Files

- backend/OniBus.Api.Tests/Features/Rotas/GetRotasTests.cs
- backend/OniBus.Api.Tests/Persistence/Seed/RotaSeedTests.cs
- backend/OniBus.Api.Tests/Persistence/OniBusDbContextIndexesTests.cs
- backend/OniBus.Domain.Tests/Viagens/LayoutOnibusTests.cs
- web/src/features/busca/ListaDeRotas.test.tsx
- web/src/shared/api/client.test.ts
- e2e/api/rotas.spec.ts
- e2e/ui/listar-rotas.spec.ts
- e2e/support/merged-fixtures.ts

## Review Context

- _bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md
- _bmad-output/implementation-artifacts/epic-1-context.md

## Excluded From Review Set

Nenhum arquivo excluído — todos os 9 arquivos de teste descobertos no repositório (fora de `bin/`, `obj/`, `node_modules/`) existem, foram lidos com sucesso e são de formatos com critérios aplicáveis no registro (xUnit/.NET, Vitest/TS, Playwright/TS).
