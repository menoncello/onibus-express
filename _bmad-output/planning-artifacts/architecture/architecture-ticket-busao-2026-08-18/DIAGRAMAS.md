---
title: 'Diagramas — OniBus Express'
type: rendering
audience: 'avaliador técnico + implementação'
serves: 'DR-2, DR-3'
created: '2026-08-18'
updated: '2026-08-18'
---

# Diagramas — OniBus Express

Conjunto C4 até nível de componente, mais três diagramas de comportamento para os pontos onde a implementação erra. Os de comportamento são os que valem no README: eles mostram julgamento, e os de estrutura mostram organização.

Os diagramas de estrutura são **seed** — verdadeiros no início, e o código passa a ser o dono deles assim que existir. Os de comportamento carregam invariantes e acompanham os ADs.

---

## C1 — Contexto

Um único ator humano. Nenhum sistema externo: sem gateway de pagamento, sem provedor de e-mail, sem integração com terceiros. A ausência é a informação.

```mermaid
graph TB
  P["Passageiro<br/><i>sem conta, sem sessão</i>"]
  S["OniBus Express<br/><i>venda de passagens rodoviárias</i>"]
  A["Avaliador técnico<br/><i>lê o repositório, sobe o ambiente</i>"]
  P -->|"busca, escolhe assento,<br/>reserva, consulta, cancela"| S
  A -->|"docker compose up --build<br/>dotnet test · npm test · /swagger"| S
```

## C2 — Containers

```mermaid
graph TB
  P["Passageiro<br/>navegador"]
  subgraph Sistema["OniBus Express"]
    W["<b>web</b><br/>React 19 · TS · Vite 8<br/>servido por Nginx 1.30<br/>SPA + reverse proxy /api"]
    A["<b>api</b><br/>ASP.NET Core net10.0<br/>minimal APIs<br/>migra e semeia no boot"]
    D[("<b>db</b><br/>PostgreSQL 18<br/>3 tabelas")]
  end
  P -->|"HTTPS"| W
  W -->|"/api/* → api:8080<br/><i>sem CORS: origem única</i>"| A
  A -->|"Npgsql · EF Core 10"| D
```

O `web` tem duas responsabilidades de propósito: servir os arquivos estáticos e ser o proxy de `/api`. É isso que faz o frontend nunca conhecer uma URL absoluta de API (AD-11) e que elimina CORS dos dois caminhos de execução.

## C3 — Componentes da `api`

```mermaid
graph TB
  subgraph Api["OniBus.Api"]
    F["<b>Features</b><br/>uma fatia por endpoint<br/>Rotas · Viagens · Reservas"]
    E["<b>Errors</b><br/>ProblemDetails<br/>tradutor de 23505<br/>por nome de constraint"]
    AD["<b>Adapters</b><br/>RelogioSistema<br/>GeradorCodigoCriptografico"]
    PE["<b>Persistence</b><br/>DbContext · mapeamentos<br/>Migrations · Seed"]
  end
  subgraph Dom["OniBus.Domain — csproj sem dependências"]
    R["<b>Regras</b><br/>CPF · JanelaCancelamento<br/>ViagemRealizada"]
    EN["<b>Entidades</b><br/>Rota · Viagem · Reserva<br/>Passageiro · LayoutOnibus"]
    PO["<b>Portas</b><br/>IRelogio<br/>IGeradorCodigoReserva"]
  end
  DB[("PostgreSQL 18")]
  F --> R
  F --> EN
  F --> PE
  F --> E
  AD -.->|"implementa"| PO
  R --> PO
  PE --> DB
  E --> DB
```

`Domain` não tem seta saindo para nada de infraestrutura — nem para `Persistence`, nem para o banco. Essa ausência é o AD-1, e ela é imposta pelo csproj, não pelo diagrama.

---

## Comportamento 1 — Concorrência no mesmo assento (FR-9)

O diagrama que vale mais no README. Duas requisições simultâneas para o assento 15; exatamente uma reserva confirmada, e a garantia é do banco.

```mermaid
sequenceDiagram
  participant R as Rafael
  participant C as Carla
  participant A as api
  participant D as PostgreSQL

  par Duas requisições simultâneas
    R->>A: POST /reservas {viagem 7, assento 15}
  and
    C->>A: POST /reservas {viagem 7, assento 15}
  end

  Note over A: Domain valida CPF e Viagem Realizada<br/>(decisão pura, sem banco)

  A->>D: INSERT reserva (Rafael, 15, Confirmada)
  A->>D: INSERT reserva (Carla, 15, Confirmada)

  D-->>A: OK
  D-->>A: 23505 em ux_reservas_viagem_assento_confirmada

  Note over A: tradutor lê ConstraintName<br/>e mapeia por NOME, não por SqlState

  A-->>R: 201 + KRT-48210
  A-->>C: 409 codigo=ASSENTO_OCUPADO

  Note over C: frontend distingue este 409 dos outros:<br/>recarrega o Mapa e PRESERVA o formulário
```

O ponto de julgamento: nenhum `SELECT` de verificação antecede o `INSERT`. Uma verificação em memória passaria em teste sequencial e falharia exatamente aqui.

## Comportamento 2 — Colisão de código de reserva (FR-8)

O mesmo `SqlState 23505` do diagrama anterior, tratamento oposto. É por isso que a discriminação por nome de constraint é invariante (AD-7).

```mermaid
sequenceDiagram
  participant A as api
  participant G as IGeradorCodigoReserva
  participant D as PostgreSQL

  A->>G: gerar()
  G-->>A: ABC-12345
  A->>D: SaveChanges (reserva com ABC-12345)
  D-->>A: 23505 em ux_reservas_codigo

  Note over A: constraint DIFERENTE ⇒ retry silencioso,<br/>não erro ao Passageiro

  A->>G: gerar()
  G-->>A: XYZ-98765
  Note over A: muta o código da MESMA entidade rastreada.<br/>Criar nova entidade inseriria DUAS reservas,<br/>porque a primeira segue Added no change tracker.
  A->>D: SaveChanges (mesma entidade, novo código)
  D-->>A: OK

  Note over A: até 5 tentativas.<br/>Esgotadas, falha explícita —<br/>nunca Reserva sem código nem código duplicado.
```

## Comportamento 3 — Cancelamento: precedência entre FR-12 e FR-13

A ordem das verificações é a decisão (AD-17). Invertê-la faz a mesma entrada devolver `204` num builder e `409` no outro.

```mermaid
flowchart TB
  S["DELETE /reservas/{codigo}"] --> N["normaliza o código<br/>CodigoReserva.Parse<br/><i>caixa e hífen</i>"]
  N --> E{"Reserva existe?"}
  E -->|não| E404["<b>404</b><br/>RESERVA_NAO_ENCONTRADA<br/><i>uniforme, não revela<br/>o espaço de códigos</i>"]
  E -->|sim| C{"Status já é<br/>Cancelada?"}
  C -->|sim| OK204["<b>204</b><br/><i>idempotente — a Janela<br/>NÃO é consultada</i>"]
  C -->|"não (Confirmada)"| J{"partida − agora > 2h?<br/><i>IRelogio, UTC</i>"}
  J -->|não| E409["<b>409</b><br/>JANELA_CANCELAMENTO_FECHADA<br/><i>2h00min exatas já recusam</i>"]
  J -->|sim| T["Status ← Cancelada"]
  T --> L["assento sai do índice parcial<br/>⇒ volta a ser reservável<br/><i>sem código adicional</i>"]
  L --> OK["<b>204</b>"]
```

A janela guarda a **transição** de confirmada para cancelada. Se não há transição a fazer, não há o que impedir — por isso o teste de status vem antes do teste de tempo.

---

## Fluxo do Passageiro e telas

```mermaid
flowchart LR
  T1["<b>Tela 1</b> Busca<br/>/<br/><i>FR-1, FR-2</i>"]
  T2["<b>Tela 2</b> Mapa de Assentos<br/>/viagens/:viagemId/assentos<br/><i>FR-3, FR-4, FR-5</i>"]
  T3["<b>Tela 3</b> Dados e confirmação<br/>/reserva<br/><i>FR-6, FR-7</i>"]
  T3S["<b>Sucesso</b><br/>código destacado e copiável<br/><i>FR-8</i>"]
  T4["<b>Tela 4</b> Consulta<br/>/consulta<br/><i>FR-11, FR-12, FR-13</i>"]
  T1 -->|"escolhe Viagem"| T2
  T2 -->|"escolhe Assento"| T3
  T3 -->|"201"| T3S
  T3 -.->|"409 ASSENTO_OCUPADO<br/>preserva o formulário"| T2
  T3S -.->|"leva o código"| T4
```

A seta pontilhada de volta é o caso de borda de FR-9 e a razão de existir a store de rascunho (AD-13). `viagemId` está na URL para que F5 no mapa não zere o fluxo.

## Ambientes

Dois ambientes obrigatórios, mais o CI. Nenhum ambiente hospedado — declarado, não esquecido.

```mermaid
flowchart TB
  subgraph A1["docker compose up --build — DR-1"]
    direction LR
    W1["web<br/>nginx:1.30-alpine"] --> P1["api<br/>net10.0"] --> D1[("db<br/>postgres:18-alpine<br/>healthcheck pg_isready")]
  end
  subgraph A2["local sem Docker — DR-8"]
    direction LR
    W2["vite dev :5173<br/>proxy /api"] --> P2["dotnet run"] --> D2[("PostgreSQL local")]
  end
  subgraph CI["GitHub Actions — push e PR"]
    direction LR
    J1["dotnet test<br/><i>Testcontainers usa o<br/>Docker do runner</i>"]
    J2["npm ci · vitest run"]
  end
  A1 -.->|"mesmo código de migração e seed"| A2
```

O mesmo caminho de código migra e semeia nos dois ambientes. É isso que impede DR-8 de entregar uma busca vazia a quem não usa Docker.
