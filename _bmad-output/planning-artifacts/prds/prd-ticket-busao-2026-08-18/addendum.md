---
title: "Addendum do PRD — OniBus Express"
created: 2026-08-18
updated: 2026-08-18
---

# Addendum do PRD

Profundidade que pertence a documentos downstream — arquitetura, design de solução, spec de UX — ou que foi decidida mas não cabe no PRD. O PRD define **o quê**; isto guarda o **por quê descartado** e o **como**, para que `bmad-architecture` não redescubra o raciocínio.

Fonte canônica de requisitos: `../../briefs/brief-ticket-busao-2026-08-18/addendum.md` (especificação original íntegra). Este arquivo não a duplica, nem repete requisito que já esteja no PRD.

---

## 1. Alternativas consideradas e rejeitadas

### 1.1 Layout do Ônibus — fixo vs. configurável por Viagem

| Opção | A favor | Contra | Veredito |
|---|---|---|---|
| **44 assentos, 2+2, fixo** | Layout rodoviário convencional brasileiro; suficiente para o Mapa de Assentos; migração e seed triviais | Modelo de domínio mais pobre; não representa frota heterogênea | **Escolhida** |
| 46 assentos, 2+2, fixo | Igualmente simples | Nenhuma vantagem sobre 44 | Rejeitada |
| Configurável por Viagem (entidade `LayoutOnibus`) | Domínio mais rico e realista; sinaliza maturação de modelagem | Custo em migração, seed e frontend dentro da janela de uma semana; nenhum FR o exige | Rejeitada — **nomear em DR-6** como evolução |

O trade-off real: o layout configurável é o tipo de generalização que impressiona *se* sobrar tempo e *se* o README explicar por que ela existe. Adicionado sem necessidade, vira abstração não exercida — exatamente o que SM-C3 desincentiva.

### 1.2 Data de nascimento — quatro leituras do conflito

A especificação lista `data de nascimento` na entidade Passageiro e a omite tanto na lista de campos do formulário da Tela 3 quanto no payload de `POST /reservas`.

| Opção | Consequência | Veredito |
|---|---|---|
| **Coletar no formulário e no payload** | Honra a tabela de entidades; divergência explícita da lista literal de campos do formulário | **Escolhida** — decisão do usuário |
| Manter na entidade, nullable, não coletada | Honra os dois textos ao mesmo tempo | Rejeitada |
| Remover da entidade | Modelo estritamente igual ao coletado | Rejeitada — contradiz a tabela de entidades |
| Coletar e dar propósito (meia-entrada, menor de idade) | Justifica o dado e vira diferencial | Rejeitada — adiciona escopo não pedido |

Qualquer das quatro é defensável. O que **não** é defensável é escolher em silêncio: a divergência tem de aparecer no README (DR-2). Resolver bem uma ambiguidade da especificação é sinal avaliado.

### 1.3 Status da Reserva — enumeração mínima

`Confirmada` e `Cancelada` bastam. Valores como `Utilizada`, `Expirada`, `PendentePagamento` ou `NoShow` pressupõem embarque ou pagamento, ambos Non-Goals (§9 do PRD). Estado derivável não precisa ser persistido: "Viagem Realizada" é comparação de data/hora, não status.

---

## 2. Decisões técnicas em aberto — insumo para `bmad-architecture`

> **Escopo desta seção.** Só entra aqui o que a especificação deixou **livre**. A stack que ela **impõe** — .NET 8+, EF Core, React 18+ com TypeScript, React Testing Library, Docker com frontend servido por Nginx — não é decisão e vive em **§7.1 do PRD**, como restrição. Uma revisão anterior deste addendum tratava a stack obrigatória como escolha de arquitetura; era erro, e está corrigido.

Nada abaixo é decisão de produto, e cada item precisa de justificativa registrável no README (DR-2).

| Decisão | Opções da especificação | Nota |
|---|---|---|
| Banco relacional | PostgreSQL **ou** SQL Server | PostgreSQL tem imagem Docker mais leve e licenciamento mais simples para repositório público. O suporte a índice filtrado também entra nesta conta — ver §2.1 |
| Framework de teste .NET | xUnit **ou** NUnit | — |
| Estado no React | Context API, Zustand, Redux, outro | O fluxo tem pouco estado compartilhado — Viagem escolhida e Assento selecionado. Redux provavelmente é excesso; ver SM-C3 |
| Test runner do frontend | Jest **ou** Vitest | — |
| Banco nos testes de integração | SQLite in-memory **ou** TestContainers | **Tensão com FR-9:** SQLite in-memory não reproduz o comportamento de concorrência que FR-9 exige provar. Se o teste de concorrência é o diferencial pretendido, TestContainers é praticamente obrigatório nesse caso, ainda que os demais testes usem in-memory |
| Envelope de erro da API | Não especificado | §5 do PRD exige consistência entre endpoints; a forma é escolha de arquitetura (ex.: ProblemDetails) |
| Referência de tempo | Não especificado | Questão Aberta 1 do PRD, com consequência de produto: o Passageiro raciocina no fuso dele |
| Corpo de resposta do `DELETE` | Não especificado | FR-13 precisa comunicar a razão da recusa; confirmar que o verbo dado pela especificação suporta isso |

### 2.1 Mecanismo de FR-9 (concorrência) — o ponto técnico de maior peso

O PRD exige que duas requisições simultâneas ao mesmo Assento produzam exatamente uma Reserva Confirmada, e que a garantia venha do banco. Direções a avaliar em arquitetura:

- **Índice único filtrado** sobre `(ViagemId, NumeroAssento)` restrito a `Status = Confirmada` — deixa o Assento rerreservável depois do cancelamento, como FR-12 exige. O suporte a índice filtrado difere entre PostgreSQL e SQL Server, e por isso pesa na escolha do banco.
- Alternativas: bloqueio pessimista na linha da Viagem, que serializa demais, ou controle otimista de concorrência via token de versão.
- Qualquer que seja o mecanismo, a violação de unicidade precisa ser **traduzida** em recusa de negócio legível (FR-9), e não vazar como erro 500 de banco.

---

## 3. Notas de UX para `bmad-ux`

Na ordem do fluxo de UJ-1:

- **Estado vazio da busca** é requisito (FR-2), não fallback. É o único ponto onde o Passageiro pode não encontrar nada.
- **Mapa de Assentos** é o único componente com decisão visual real. Três estados (Livre, Ocupado, Selecionado) distinguíveis sem depender de cor isolada — FR-4 trata isso como requisito, não como cortesia.
- **Preservação de contexto na falha de FR-9.** Quando o Assento é tomado entre a abertura do mapa e a confirmação, o Passageiro não pode perder o que digitou. É o caso de borda de UJ-1 e o momento mais frustrante do fluxo.
- **Código de Reserva** precisa ser destacado e copiável na tela de sucesso. O formato `AAA-00000` foi escolhido justamente para ser ditável por telefone; a interface deve honrar isso.

---

## 4. O que este PRD deliberadamente não é

O usuário primário deste artefato é o avaliador técnico, não o passageiro (ver `brief.md`). Isso justifica duas escolhas que um PRD de produto real não faria:

1. **§8, Requisitos de Entrega e Avaliação** — seção inventada fora do Adapt-In Menu. README, histórico de git e submissão do repositório são obrigatórios e pontuados pela especificação, mas não são capacidade de produto; a numeração própria (`DR-n`) existe para que isso não contamine o espaço de FRs. Fora do PRD, não virariam épico nem tarefa.
2. **Métricas de sucesso verificáveis por inspeção**, não por telemetria. Não há usuário real para medir. As contra-métricas (SM-C1 a SM-C3) carregam peso desproporcional aqui porque o risco dominante da entrega não é fazer pouco — é dispersar.
