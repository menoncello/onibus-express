<!-- bmad:context -->
<!-- Sem commits ainda (repositório greenfield). Managed by bmad-project-context; edits inside this block are replaced on refresh. -->

## ticket-busao — OniBus Express

MVP de venda de passagens rodoviárias, entregue como desafio técnico avaliado (backend .NET, frontend React, empacotados em Docker). Repositório greenfield: nenhum código ainda existe. Requisitos: `_bmad-output/planning-artifacts/prds/prd-ticket-busao-2026-08-18/prd.md`. Invariantes de arquitetura: `_bmad-output/planning-artifacts/architecture/architecture-ticket-busao-2026-08-18/ARCHITECTURE-SPINE.md`.

## Policy

- Nenhum dado pessoal real (CPF, nome, e-mail, data de nascimento) neste repositório público — apenas dados sintéticos no seed.
- Nunca versionar segredo, chave ou string de conexão real; usar `.env.example` com valores locais óbvios.

## Where things are

- Requisitos e regras de negócio: PRD acima.
- Invariantes de arquitetura (17 ADs), stack com versões pinadas, mapa capacidade→arquitetura: `ARCHITECTURE-SPINE.md` no diretório acima.
- Divisão em épicos e ordem de construção sugerida: `DIVISAO-POR-EPICO.md` no mesmo diretório.
- Racional das escolhas de stack, para o README da entrega: `README-ARQUITETURA-RASCUNHO.md` no mesmo diretório.

## Running and verifying

- TODO — verificar no primeiro refresh após o código existir: backend via `dotnet test` a partir de `backend/OniBus.sln`; frontend via `npm run test` (Vitest) a partir de `web/`; ambiente completo via `docker compose up --build`.

## Conventions that differ from defaults

- Domínio e regras de negócio em português (`Reserva`, `Viagem`, `Assento` — literal do Glossário do PRD); frameworks, pacotes e nomes técnicos em inglês.
- Importar sempre de `react-router`, nunca de `react-router-dom` — o pacote não existe mais na v8 desta stack.
- Dinheiro é `decimal`, nunca `float`/`double`, em nenhuma camada.
- `StatusReserva` é persistido como texto (`HasConversion<string>()`), nunca como inteiro — o índice único parcial de FR-9 filtra o literal `'Confirmada'`.

## Known pitfalls

- Violação de unicidade do Postgres (`SqlState 23505`) ocorre em dois índices com tratamento oposto: discriminar sempre por `ConstraintName`, nunca capturar genérico.
- Retry de colisão do Código de Reserva muta a MESMA entidade já rastreada; criar uma nova entidade insere duas Reservas.
- Idempotência do seed é por chave natural (Rota por origem+destino, Viagem por rota+partida); `if (!db.Rotas.Any())` não reconcilia quando o conjunto semeado muda.
- `DELETE /reservas/{codigo}` verifica nesta ordem: existe → já Cancelada (então 204, sem consultar a janela) → Janela de Cancelamento. Inverter quebra a idempotência de FR-12.

<!-- /bmad:context -->
