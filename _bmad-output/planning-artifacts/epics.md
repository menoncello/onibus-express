---
stepsCompleted: [1, 2, 3]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-ticket-busao-2026-08-18/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-ticket-busao-2026-08-18/addendum.md"
  - "_bmad-output/planning-artifacts/architecture/architecture-ticket-busao-2026-08-18/ARCHITECTURE-SPINE.md"
  - "_bmad-output/planning-artifacts/architecture/architecture-ticket-busao-2026-08-18/DIVISAO-POR-EPICO.md"
---

# OniBus Express - Epic Breakdown

## Overview

Este documento apresenta o desmembramento completo de épicos e histórias para o OniBus Express, decompondo os requisitos do PRD e as decisões de Arquitetura em histórias implementáveis. Não existe um contrato de UX design dedicado (bmad-ux) para este projeto; as notas de UX relevantes vivem embutidas nos FRs do PRD e nas convenções de frontend da Arquitetura (ver seção UX Design Requirements abaixo).

## Requirements Inventory

### Functional Requirements

FR-1: Listar Rotas disponíveis — qualquer visitante pode obter a lista completa de Rotas cadastradas, sem autenticação.
FR-2: Buscar Viagens por origem, destino e data — origem, destino e data são todos obrigatórios; Viagem Realizada não aparece nos resultados; Viagem Esgotada aparece marcada como indisponível.
FR-3: Detalhar uma Viagem com o estado dos Assentos — resposta identifica cada um dos 44 Assentos e seu estado (Livre/Ocupado).
FR-4: Visualizar o Mapa de Assentos — 44 Assentos em 11 fileiras de 4, com corredor central; estados Livre/Ocupado/Selecionado distinguíveis sem depender só de cor.
FR-5: Selecionar um Assento Livre — exatamente um Assento selecionável por vez; Assento Ocupado não é interativo.
FR-6: Validar CPF por formato e dígito verificador — validação no cliente e no servidor, de forma independente; sequências repetidas rejeitadas explicitamente.
FR-7: Criar Reserva — nome, CPF, e-mail e data de nascimento para uma Viagem e um Assento Livre; resumo exibido antes da confirmação.
FR-8: Gerar Código de Reserva único e legível — formato `AAA-00000`; unicidade garantida por restrição de banco; colisão tratada com retry; gerador injetável.
FR-9: Impedir Reserva de Assento Ocupado — inclusive sob requisições concorrentes; garantia do banco de dados (restrição de unicidade), não verificação em memória.
FR-10: Impedir Reserva de Viagem Realizada — comparação via Relógio da Aplicação; teste automatizado obrigatório por este PRD.
FR-11: Consultar Reserva pelo Código de Reserva — Reserva Cancelada é retornada normalmente; código inexistente retorna 404 uniforme; consulta tolera caixa e hífen.
FR-12: Cancelar Reserva — dentro da Janela de Cancelamento; cancelar Reserva já Cancelada é idempotente.
FR-13: Fazer valer a Janela de Cancelamento — recusa a menos de 2 horas da partida (fronteira exclusiva); comparação via Relógio da Aplicação; precedência da idempotência de FR-12 quando a Viagem já não permite transição.
FR-14: Semear Rotas e Viagens ao iniciar — datas relativas ao instante de execução do seed; idempotente; inclui Viagem Esgotada, Realizada, parcialmente ocupada, dentro e fora da Janela de Cancelamento.

### NonFunctional Requirements

NFR-1: Concorrência — a unicidade de (Viagem, Assento) entre Reservas Confirmadas é garantida pelo banco de dados, nunca por verificação em memória seguida de inserção.
NFR-2: Testabilidade dirigida — as cinco regras de negócio (FR-6, FR-8, FR-9, FR-10, FR-13) precisam de teste que prove a regra, não apenas a exercite; perseguir percentual de cobertura não é objetivo.
NFR-3: Testes de comportamento no frontend — os três testes exigidos (busca, Mapa de Assentos, validação de formulário) verificam comportamento do usuário, não detalhe de implementação; mock da API é permitido.
NFR-4: Reprodutibilidade — um único comando (`docker-compose up --build`) sobe o ambiente completo do zero, em máquina limpa, sem passo manual não documentado.
NFR-5: Acessibilidade mínima — estados de Assento não podem depender exclusivamente de cor.
NFR-6: Relógio injetável — FR-10 e FR-13 comparam contra o Relógio da Aplicação (`IRelogio`), nunca contra chamada direta ao relógio do sistema, para permitir teste determinístico.
NFR-7: Domínio isolado com fronteira honesta — regras de decisão pura (CPF, Janela de Cancelamento, Viagem Realizada, formato do Código de Reserva) são testáveis sem infraestrutura; FR-9 é a exceção declarada, pois sua garantia depende do banco real.
NFR-8: Privacidade — nenhum dado pessoal real no repositório público; dado pessoal não aparece em log; mitigação de enumeração de Código de Reserva via geração imprevisível e resposta 404 uniforme.

### Additional Requirements

Requisitos de Entrega e Avaliação (PRD §8, numeração própria DR-n — obrigatórios e pontuados pela especificação do desafio):

- DR-1: Ambiente completo em um comando — `docker-compose up --build` sobe API, banco e frontend; migrações aplicadas na inicialização; catálogo semeado disponível ao fim; frontend servido por Nginx (não servidor de dev).
- DR-2: README completo — como rodar com/sem Docker, tecnologias e por quê, decisões de arquitetura, o que foi implementado vs. fora, como rodar os testes; documenta cada divergência deliberada da especificação original.
- DR-3: Documentação de endpoints — Swagger/OpenAPI navegável, com link no README.
- DR-4: Histórico de git incremental — commits regulares e legíveis, distribuídos em pelo menos três dias distintos.
- DR-5: Evidência visual — screenshots ou gif da aplicação rodando, no README.
- DR-6: Pontos de melhoria declarados — o que seria feito com mais tempo e o que ficou de fora, alimentado pela seção "Deferred" da arquitetura.
- DR-7: Submissão da entrega — repositório público e acessível, sem dependência de artefato local não versionado.
- DR-8: Execução local sem Docker — caminho documentado e funcional (não apenas descrito) para rodar sem Docker, incluindo catálogo semeado.

Decisões de Arquitetura que impactam a construção de épicos e histórias (ARCHITECTURE-SPINE.md):

- Paradigma: fatias verticais sobre núcleo de domínio puro — três projetos (`OniBus.Domain`, `OniBus.Api`, `web`), sem camadas `Infrastructure`/`Application` (AD-1).
- Duas portas apenas — `IRelogio` e `IGeradorCodigoReserva`, declaradas no domínio e implementadas na Api (AD-2).
- Instantes em UTC; dia de calendário traduzido no fuso `America/Sao_Paulo` antes da consulta (AD-3).
- Não existe tabela de Assentos — disponibilidade é sempre derivada das Reservas Confirmadas via função única `LayoutOnibus.EstadoDosAssentos` (AD-4).
- Passageiro é valor inline em `reservas` (EF Core `ComplexProperty`), não entidade própria (AD-5).
- Unicidade de (Viagem, Assento) via índice único parcial `ux_reservas_viagem_assento_confirmada`, filtrado por `Status = 'Confirmada'` (AD-6).
- Violação de unicidade (`SqlState 23505`) discriminada por nome de constraint — `ux_reservas_codigo` aciona retry; `ux_reservas_viagem_assento_confirmada` aciona `409 ASSENTO_OCUPADO` (AD-7).
- Gerador de Código de Reserva injetável, usa `RandomNumberGenerator`, retry até 5 tentativas, normalização de entrada com dono único `CodigoReserva.Parse` (AD-8).
- Toda recusa é `ProblemDetails` (RFC 9457) com vocabulário fechado de códigos (AD-9).
- Duas camadas de teste — `OniBus.Domain.Tests` (xUnit puro, sem banco) e `OniBus.Api.Tests` (Testcontainers com PostgreSQL real); nenhum SQLite em nenhuma camada (AD-10).
- Frontend nunca conhece URL absoluta da API — sempre `/api/*` via proxy reverso, sem CORS e sem variável de ambiente de URL (AD-11).
- Quatro estados de tela: carregando, sucesso (com/sem resultados), erro de entrada (400), falha de comunicação (rede/5xx) (AD-12).
- Store Zustand carrega rascunho do formulário; `viagemId` viaja na rota (AD-13).
- Validação idêntica em cliente e servidor com fronteiras fixadas (CPF, nome 3-120 chars, e-mail até 254 chars, data de nascimento 0-120 anos) (AD-14).
- Migração e seed no boot da Api, mesmo caminho de código nos dois ambientes; `depends_on: service_healthy`; seed idempotente por chave natural; 5 Rotas e 16 Viagens (AD-15).
- Dado pessoal nunca entra em log (AD-16).
- `DELETE /reservas/{codigo}` verifica na ordem: existe → já Cancelada (idempotente, sem checar janela) → Janela de Cancelamento (AD-17).
- Stack fixada: .NET 10 (net10.0), EF Core 10, PostgreSQL 18, xunit.v3, Testcontainers 4.12.0, Node 24, React 19.2, TypeScript 5, Vite 8.2, react-router 8.3 (não react-router-dom), Zustand 5.0, Vitest 4.1, Nginx 1.30.
- Sequência de épicos sugerida pela Arquitetura (DIVISAO-POR-EPICO.md): E1 Esqueleto vivo com Docker desde o dia 1 → E2 Domínio e regras puras (paralelizável com E1) → E3 Descoberta de Viagens → E4 Reserva e concorrência → E5 Consulta e cancelamento → E6 Frontend Telas 1-3 → E7 Frontend Tela 4 → E8 Swagger e CI → E9 Documentação e entrega.

### UX Design Requirements

Não há um contrato de UX design dedicado (bmad-ux) para este projeto. Os requisitos de UX relevantes estão embutidos no PRD e na Arquitetura e serão tratados como parte das histórias de frontend correspondentes:

UX-DR1: Estado vazio da busca é requisito (FR-2), não fallback — distinguível visualmente de carregando e de erro (AD-12).
UX-DR2: Mapa de Assentos é o único componente com decisão visual real — três estados (Livre, Ocupado, Selecionado) distinguíveis sem depender de cor isolada (forma, borda ou rótulo) (FR-4).
UX-DR3: Preservação de contexto na falha de FR-9 — quando o Assento é tomado entre abertura do mapa e confirmação, o formulário já preenchido não pode ser perdido; o mapa recarrega com o estado atual (AD-13).
UX-DR4: Código de Reserva destacado e copiável na tela de sucesso, imediatamente após a confirmação (FR-8).
UX-DR5: Quatro estados de tela em toda tela que chama a API — carregando, sucesso (com/sem resultados), erro de entrada (400), falha de comunicação (rede/5xx) — vazio nunca é renderizado como erro (AD-12).
UX-DR6: Tela de consulta de Reserva distingue visualmente Confirmada de Cancelada, e só oferece botão de cancelar quando a Janela de Cancelamento permite, com razão explícita na recusa (FR-11, FR-12, FR-13).
UX-DR7: Funções únicas de formatação de preço (`Intl.NumberFormat` pt-BR/BRL) e de data/hora (`Intl.DateTimeFormat`) centralizadas em `src/shared`, usadas por todas as telas — nenhuma tela formata por conta própria.

### FR Coverage Map

FR-1: Epic 1 - Listar Rotas disponíveis (popula campos de busca)
FR-2: Epic 1 - Buscar Viagens por origem, destino e data (estados de carregando/resultados/vazio)
FR-3: Epic 1 - Detalhar Viagem com estado dos Assentos
FR-14: Epic 1 - Semear Rotas e Viagens ao iniciar (idempotente, datas relativas)
FR-4: Epic 2 - Visualizar Mapa de Assentos (44 assentos, estados acessíveis)
FR-5: Epic 2 - Selecionar um Assento Livre
FR-6: Epic 3 - Validar CPF por formato e dígito verificador (cliente + servidor)
FR-7: Epic 3 - Criar Reserva (dados do Passageiro, resumo antes da confirmação)
FR-8: Epic 3 - Gerar Código de Reserva único e legível (gerador injetável, retry)
FR-9: Epic 3 - Impedir Reserva de Assento Ocupado (concorrência garantida pelo banco)
FR-10: Epic 3 - Impedir Reserva de Viagem Realizada (Relógio da Aplicação)
FR-11: Epic 4 - Consultar Reserva pelo Código de Reserva (tolerante a caixa/hífen)
FR-12: Epic 4 - Cancelar Reserva (idempotente)
FR-13: Epic 4 - Fazer valer a Janela de Cancelamento (fronteira exclusiva de 2h)
DR-1 a DR-8: Epic 5 - Requisitos de Entrega e Avaliação (Docker, README, Swagger, CI, git, evidência visual, submissão, execução sem Docker)

NFR-1 (Concorrência): Epic 3 - garantida por índice único parcial no banco (AD-6)
NFR-2 (Testabilidade dirigida): Epic 3 e Epic 4 - cada uma das 5 regras de negócio com teste que prova a regra
NFR-3 (Testes de comportamento frontend): Epic 1, Epic 2, Epic 3 - busca, mapa de assentos, validação de formulário
NFR-4 (Reprodutibilidade): Epic 1 e Epic 5 - ambiente sobe com um comando
NFR-5 (Acessibilidade mínima): Epic 2 - estados de assento não dependem só de cor
NFR-6 (Relógio injetável): Epic 3 (Viagem Realizada) e Epic 4 (Janela de Cancelamento)
NFR-7 (Domínio isolado): Epic 3 e Epic 4 - regras puras testáveis sem infraestrutura
NFR-8 (Privacidade): Epic 3 (dado pessoal não loga) e Epic 5 (repositório público sem segredos)

UX-DR1 (estado vazio): Epic 1
UX-DR2 (mapa de assentos acessível): Epic 2
UX-DR3 (preservação de contexto na falha de FR-9): Epic 3
UX-DR4 (código copiável): Epic 3
UX-DR5 (quatro estados de tela): Epic 1, Epic 2, Epic 3, Epic 4
UX-DR6 (distinção visual Confirmada/Cancelada): Epic 4
UX-DR7 (formatação centralizada): Epic 1 (introduzida) e reutilizada nos demais

## Epic List

**Nota de rastreabilidade (levantada em party mode):** cada história, ao ser criada no próximo passo, deve receber a Faixa de corte do PRD (§10.3: Faixa 1 núcleo inegociável, Faixa 2 o que decide a nota, Faixa 3 diferenciação, Faixa 4 primeiro a cair) como metadado explícito — necessário para que SM-8 (todo item cortado aparece nominalmente no README) seja verificável por comparação direta.

### Epic 1: Fundação e Descoberta de Viagens
Ambiente completo sobe com um comando (Docker + Postgres + migrações + seed), e o Passageiro consegue buscar viagens por origem, destino e data, vendo o catálogo semeado — incluindo os casos de borda de viagem esgotada, realizada e parcialmente ocupada.

**Nota de valor duplo (levantada em party mode):** este épico serve dois usuários com critérios de sucesso distintos — o Passageiro (busca funcional) e o avaliador técnico (SM-1: `docker-compose up --build` resulta em aplicação funcional em máquina limpa, sem passo manual). As histórias devem tornar esse segundo critério verificável explicitamente, não apenas implícito no resultado da busca.

**FRs covered:** FR-1, FR-2, FR-3, FR-14

### Epic 2: Seleção de Assento
O Passageiro visualiza o mapa de 44 assentos da viagem escolhida e seleciona um assento livre, com estados visuais distinguíveis sem depender só de cor.
**FRs covered:** FR-4, FR-5

### Epic 3: Reserva e Emissão do Código
O Passageiro informa seus dados, confirma a reserva e recebe um Código de Reserva único e legível — com CPF validado nos dois lados, concorrência de assento tratada pelo banco, e viagens já realizadas recusadas.
**FRs covered:** FR-6, FR-7, FR-8, FR-9, FR-10

### Epic 4: Consulta e Cancelamento
O Passageiro consulta sua reserva pelo Código de Reserva e cancela sozinho, respeitando a Janela de Cancelamento de 2 horas antes da partida.

**Nota de sequenciamento de risco (levantada em party mode):** a regra `JanelaCancelamento` (FR-13) deve ser escrita e testada como regra pura de domínio — sem banco, nas três fronteiras 2h01/2h00/1h59 — **dentro da mesma história** que implementa `DELETE /reservas/{codigo}`, e **antes** de tocar o endpoint. Isso evita que um dos quatro testes obrigatórios pela especificação fique represado até a validação de integração do último épico funcional.

**FRs covered:** FR-11, FR-12, FR-13

### Epic 5: Qualidade, Documentação e Entrega
Swagger navegável, pipeline de CI, README completo (como rodar com/sem Docker, decisões documentadas, evidência visual, pontos de melhoria) e submissão do repositório — os itens obrigatórios e pontuados pela avaliação do desafio.
**Delivers:** DR-1, DR-2, DR-3, DR-4, DR-5, DR-6, DR-7, DR-8

---

## Epic 1: Fundação e Descoberta de Viagens

Ambiente completo sobe com um comando, e o Passageiro consegue buscar viagens por origem, destino e data, vendo o catálogo semeado. Serve dois usuários: o Passageiro (busca funcional) e o avaliador técnico (SM-1).

**FRs:** FR-1, FR-2, FR-3, FR-14 · **NFRs:** NFR-3, NFR-4 · **UX-DRs:** UX-DR1, UX-DR5, UX-DR7

### Story 1.1: Esqueleto vivo — ambiente sobe com um comando e lista Rotas

As a Passageiro (e avaliador técnico),
I want subir o ambiente completo com um único comando e ver a lista de rotas disponíveis,
So that eu possa começar a busca sem nenhum passo manual, e o avaliador possa verificar em minutos que o projeto funciona.

**Faixa:** 1 (núcleo inegociável) — base de DR-1

**Acceptance Criteria:**

**Given** um clone limpo do repositório
**When** executo `docker-compose up --build`
**Then** os três serviços (web, api, db) sobem, as migrações são aplicadas automaticamente
**And** a aplicação está pronta sem nenhum passo manual adicional

**Given** a primeira migração
**When** ela é aplicada
**Then** a tabela `reservas` já contém os índices únicos `ux_reservas_codigo` e `ux_reservas_viagem_assento_confirmada` (filtrado por `status = 'Confirmada'`)
**And** isso vale mesmo antes de existir endpoint de reserva

**Given** o serviço `api` configurado com `depends_on: condition: service_healthy` sobre o healthcheck `pg_isready` do `db`
**When** o compose sobe
**Then** a Api não tenta conectar ao banco antes dele aceitar conexões

**Given** ao menos uma Rota semeada no banco
**When** faço `GET /rotas`
**Then** recebo `200` com origem, destino e duração estimada de cada rota

**Given** o frontend servido por Nginx (não servidor de dev)
**When** acesso a página inicial
**Then** vejo a lista de rotas carregada via `/api/rotas`, caminho relativo, sem URL absoluta

**Given** `OniBus.Domain.csproj`
**When** inspeciono o arquivo
**Then** ele não contém nenhum `PackageReference` nem `ProjectReference`

### Story 1.2: Buscar Viagens por origem, destino e data

As a Passageiro,
I want buscar viagens informando origem, destino e data,
So that eu veja apenas as viagens que servem para minha necessidade.

**Faixa:** 1 (núcleo inegociável)

**Acceptance Criteria:**

**Given** origem, destino e data válidos correspondendo a viagens semeadas
**When** faço `GET /viagens?origem=X&destino=Y&data=D`
**Then** recebo `200` com a lista de viagens daquele dia, cada uma com data/hora de partida, preço base e quantidade de assentos livres

**Given** a requisição sem um dos três parâmetros obrigatórios
**When** busco
**Then** recebo `400` com `ProblemDetails` e código `CAMPO_INVALIDO`
**And** não é tratada como busca aberta

**Given** uma busca sem correspondência
**When** busco
**Then** recebo `200` com coleção vazia — não é erro

**Given** uma Viagem cuja partida já passou
**When** busco viagens naquele dia
**Then** ela não aparece nos resultados

**Given** uma Viagem sem assentos livres (Esgotada)
**When** busco viagens naquele dia
**Then** ela aparece nos resultados, marcada como indisponível

**Given** a tela de busca
**When** a chamada está em andamento, retorna resultados, ou retorna vazio
**Then** a interface distingue visualmente os três estados: carregando, sucesso com resultados, sucesso vazio com mensagem explícita

**Given** a tela de busca
**When** a API falha ou está fora do ar (5xx ou erro de rede)
**Then** a interface mostra um estado de falha de comunicação, distinto do estado vazio

**Given** o teste de frontend exigido de busca
**When** executado
**Then** ele mocka o cliente tipado de `src/shared/api` (nunca `fetch` global) e verifica o comportamento do usuário: preencher, buscar, ver resultados/vazio

**Given** o preço e a data/hora de partida de cada viagem listada
**When** renderizados
**Then** usam funções únicas de formatação centralizadas em `src/shared` (`Intl.NumberFormat` pt-BR/BRL para preço, `Intl.DateTimeFormat` para data/hora) — nenhuma tela formata por conta própria, e as demais telas do produto reutilizam essas mesmas funções

### Story 1.3: Ver detalhes de uma Viagem com o estado dos Assentos

As a Passageiro,
I want ver os detalhes de uma viagem, incluindo quais assentos estão livres e ocupados,
So that eu possa decidir se aquela viagem serve antes de escolher onde sentar.

**Faixa:** 1 (núcleo inegociável)

**Acceptance Criteria:**

**Given** uma Viagem existente
**When** faço `GET /viagens/{id}`
**Then** recebo `200` com Rota, data/hora de partida, preço base, e o estado (Livre/Ocupado) de cada um dos 44 assentos

**Given** um id de Viagem inexistente
**When** faço a chamada
**Then** recebo `404` uniforme

**Given** a derivação de estado dos assentos
**When** o endpoint responde
**Then** o cálculo usa exclusivamente `LayoutOnibus.EstadoDosAssentos(reservasConfirmadas)` do domínio
**And** nenhuma outra consulta deriva ocupação por conta própria

**Given** a lista de resultados da busca (Story 1.2)
**When** clico em uma viagem
**Then** navego para a rota `/viagens/:viagemId/assentos` (viagemId na URL, não só em estado de aplicação), vendo Rota, data, hora e preço da viagem

**Given** a página de detalhe da viagem
**When** a API falha
**Then** o erro é distinguível do estado vazio, seguindo o mesmo padrão de quatro estados

### Story 1.4: Seed completo e idempotente com casos de borda

As an avaliador técnico,
I want que o ambiente suba com um catálogo semeado realista e idempotente,
So that eu possa exercitar toda a superfície do produto — inclusive casos de borda — desde o primeiro `up`, sem passo manual e sem duplicação numa segunda subida.

**Faixa:** 1 (núcleo inegociável) — seed com datas relativas é núcleo; volume/casos de borda plenos entram aqui também por serem baratos e pré-condição de FR-9/FR-10/FR-13 demonstráveis

**Acceptance Criteria:**

**Given** o boot da Api
**When** o seed roda
**Then** ele semeia 5 Rotas e 16 Viagens, com datas de partida relativas ao instante de execução (`IRelogio.Agora`)
**And** nunca literais de calendário

**Given** o catálogo semeado
**When** inspeciono as viagens
**Then** há pelo menos uma Viagem Esgotada (44 reservas confirmadas), uma Realizada (partida no passado), uma parcialmente ocupada, uma dentro da Janela de Cancelamento e uma fora dela

**Given** as Reservas criadas pelo seed para popular esses casos
**When** são criadas
**Then** passam pelo `IGeradorCodigoReserva` real e pela validação de CPF real
**And** nunca usam códigos ou CPFs escritos à mão

**Given** o ambiente já semeado
**When** executo `docker-compose up --build` uma segunda vez
**Then** Rotas e Viagens não duplicam — a idempotência é por chave natural (Rota por origem/destino, Viagem por rota+partida), nunca por contagem prévia

**Given** o caminho sem Docker (DR-8)
**When** executo o seed pelos passos documentados
**Then** o mesmo catálogo é semeado, pelo mesmo caminho de código do ambiente Docker

---

## Epic 2: Seleção de Assento

O Passageiro visualiza o mapa de 44 assentos da viagem escolhida e seleciona um assento livre, com estados visuais distinguíveis sem depender só de cor.

**FRs:** FR-4, FR-5 · **NFRs:** NFR-3, NFR-5 · **UX-DRs:** UX-DR2

### Story 2.1: Visualizar o Mapa de Assentos

As a Passageiro,
I want ver o mapa de assentos da viagem escolhida,
So that eu enxergue de uma vez o que está livre e o que está ocupado antes de escolher onde sentar.

**Faixa:** 1 (núcleo inegociável) — Tela 2 é núcleo do MVP

**Acceptance Criteria:**

**Given** uma viagem com estado de assentos (obtido na Story 1.3)
**When** acesso a página de assentos
**Then** vejo os 44 assentos renderizados em 11 fileiras de 4, com corredor central entre a segunda e a terceira coluna

**Given** os assentos Livre e Ocupado
**When** a tela renderiza
**Then** os dois estados são distinguíveis sem depender só de cor — usando forma, borda ou rótulo adicional

**Given** a página de assentos
**When** carrega
**Then** exibe Rota, data, hora e preço da viagem, sem chamada redundante além da já feita em Story 1.3

**Given** a tela de assentos
**When** a API falha ao carregar o detalhe da viagem
**Then** distingue o estado de falha de comunicação do estado de carregamento

### Story 2.2: Selecionar um Assento Livre

As a Passageiro,
I want selecionar um assento livre,
So that eu possa prosseguir para informar meus dados com o lugar já escolhido.

**Faixa:** 1 (núcleo inegociável)

**Acceptance Criteria:**

**Given** o mapa de assentos
**When** clico em um Assento Livre
**Then** ele passa ao estado visual "Selecionado", distinguível de Livre e Ocupado sem depender só de cor

**Given** um Assento já Ocupado
**When** clico nele
**Then** nada acontece — não é interativo e não emite erro

**Given** um Assento já selecionado
**When** clico em outro Assento Livre
**Then** a seleção anterior é substituída — nunca há dois assentos selecionados ao mesmo tempo

**Given** nenhum assento selecionado
**When** a tela está nesse estado
**Then** o botão de prosseguir permanece bloqueado

**Given** um assento selecionado e a store Zustand de fluxo de compra
**When** a página é recarregada (F5) na mesma URL `/viagens/:viagemId/assentos`
**Then** a viagem carregada permanece a mesma, pois o `viagemId` vem da rota, não apenas do estado em memória

**Given** o teste de frontend exigido do Mapa de Assentos
**When** executado
**Then** verifica que clicar em assento livre seleciona, clicar em ocupado não faz nada, e selecionar outro substitui a seleção — mockando o cliente tipado de `src/shared/api`

---

## Epic 3: Reserva e Emissão do Código

O Passageiro informa seus dados, confirma a reserva e recebe um Código de Reserva único e legível — com CPF validado nos dois lados, concorrência de assento tratada pelo banco, e viagens já realizadas recusadas. O épico de maior risco técnico da entrega.

**FRs:** FR-6, FR-7, FR-8, FR-9, FR-10 · **NFRs:** NFR-1, NFR-2, NFR-6, NFR-7, NFR-8 · **UX-DRs:** UX-DR3, UX-DR4

### Story 3.1: Validar dados do Passageiro no formulário

As a Passageiro,
I want que o sistema valide meus dados (CPF, nome, e-mail, data de nascimento) antes de eu conseguir confirmar,
So that eu corrija erros de digitação na hora, sem esperar uma resposta de servidor.

**Faixa:** 1 (núcleo inegociável) — FR-6 é uma das cinco regras de negócio e um dos quatro testes de backend obrigatórios

**Acceptance Criteria:**

**Given** um CPF com quantidade de dígitos diferente de 11
**When** informo no formulário
**Then** é rejeitado com mensagem clara, e o botão de confirmar permanece bloqueado

**Given** um CPF cujos dois dígitos verificadores não conferem
**When** informo
**Then** é rejeitado

**Given** uma sequência de dígitos repetidos (`111.111.111-11`, `000.000.000-00` e as demais)
**When** informo
**Then** é rejeitada explicitamente, mesmo passando no algoritmo de dígito verificador

**Given** um CPF válido
**When** informo com ou sem máscara de pontuação
**Then** é aceito

**Given** a regra de CPF
**When** testada em `OniBus.Domain.Tests`
**Then** existe teste unitário puro, sem banco e sem host, cobrindo os quatro casos acima

**Given** nome, e-mail e data de nascimento
**When** preencho o formulário
**Then** nome exige 3 a 120 caracteres após trim, e-mail exige formato válido e até 254 caracteres, data de nascimento exige data no passado com idade entre 0 e 120 anos

**Given** qualquer campo inválido
**When** tento confirmar
**Then** o botão de confirmar permanece bloqueado e a chamada à API não é disparada

**Given** o teste de frontend exigido de validação de formulário
**When** executado
**Then** verifica os casos de CPF inválido, nome vazio/curto e e-mail malformado, sem mockar rede (validação client-side pura)

### Story 3.2: Criar Reserva e emitir Código de Reserva

As a Passageiro,
I want confirmar minha reserva depois de conferir os dados e receber um código legível,
So that eu tenha uma prova da compra que consigo ditar por telefone.

**Faixa:** 1 (núcleo inegociável) — FR-7 e FR-8 são núcleo; o teste de colisão forçada é um dos quatro testes de backend obrigatórios

**Acceptance Criteria:**

**Given** uma Viagem e um Assento Livre, e dados válidos de Passageiro (nome, CPF, e-mail, data de nascimento)
**When** faço `POST /reservas`
**Then** recebo `201` com a Reserva criada em Status `Confirmada` e um Código de Reserva no formato `AAA-00000`

**Given** a mesma requisição
**When** o servidor recebe
**Then** revalida CPF, nome, e-mail e data de nascimento de forma independente do cliente — entrada inválida retorna `400` com código `CAMPO_INVALIDO` ou `CPF_INVALIDO`, sem criar registro

**Given** um campo obrigatório ausente no payload
**When** faço a requisição
**Then** recebo `400`, sem criar Reserva

**Given** a tela de dados do Passageiro
**When** preencho e sigo
**Then** vejo um resumo (trecho, data, hora, assento, preço) para conferência antes da confirmação

**Given** a confirmação
**When** a Reserva é criada com sucesso
**Then** a tela de sucesso exibe o Código de Reserva de forma destacada e copiável, imediatamente

**Given** o gerador de código (`IGeradorCodigoReserva`)
**When** uma colisão é simulada em teste, com um gerador de teste que devolve o mesmo código duas vezes
**Then** a segunda tentativa recebe um código diferente — a colisão é tratada com retry sobre a mesma entidade já rastreada, sem propagar erro ao Passageiro

**Given** as tentativas de retry esgotadas (cenário de teste)
**When** isso ocorre
**Then** a falha é explícita — nunca uma Reserva sem código ou com código duplicado

**Given** o Assento reservado
**When** consulto `GET /viagens/{id}` novamente
**Then** ele aparece como Ocupado

**Given** o payload de `POST /reservas` (nome, CPF, e-mail, data de nascimento)
**When** a requisição é processada, com sucesso ou com erro
**Then** nenhum desses dados pessoais aparece em log de aplicação ou em mensagem de exceção — apenas o Código de Reserva pode ser logado

### Story 3.3: Impedir Reserva de Assento Ocupado sob concorrência real

As a Passageiro,
I want que o sistema nunca venda o mesmo assento duas vezes,
So that eu tenha certeza de que o lugar que escolhi é meu, mesmo se outra pessoa tentar o mesmo assento ao mesmo tempo.

**Faixa:** 1 (recusa funcional, núcleo inegociável) + 2 (teste de concorrência real — o item que decide a nota)

**Acceptance Criteria:**

**Given** um Assento já Ocupado por uma Reserva Confirmada na mesma Viagem
**When** outro Passageiro tenta reservá-lo
**Then** recebo `409` com código `ASSENTO_OCUPADO`

**Given** duas requisições simultâneas para o mesmo Assento e Viagem
**When** ambas chegam ao mesmo tempo
**Then** exatamente uma resulta em Reserva Confirmada — a garantia vem do índice único parcial do banco (criado na Story 1.1), não de verificação em memória

**Given** a violação de unicidade (`SqlState 23505` sobre `ux_reservas_viagem_assento_confirmada`)
**When** capturada
**Then** é traduzida para `409 ASSENTO_OCUPADO` — nunca escapa como `500`

**Given** o teste de concorrência real
**When** executado em `OniBus.Api.Tests` com Testcontainers (`postgres:18-alpine`)
**Then** dispara duas requisições simultâneas e verifica exatamente uma Confirmada e uma recusada

**Given** uma Reserva anterior Cancelada para aquele Assento
**When** uma nova Reserva é tentada
**Then** o Assento continua reservável, pois a linha Cancelada está fora do índice filtrado

**Given** a recusa por Assento Ocupado no frontend
**When** ocorre
**Then** a tela preserva os dados já digitados no formulário e recarrega o Mapa de Assentos com o estado atual, sem perder o rascunho da store

### Story 3.4: Impedir Reserva de Viagem Realizada

As a Passageiro,
I want que o sistema recuse reservas para viagens que já partiram,
So that eu nunca compre uma passagem para uma viagem que não vou conseguir pegar.

**Faixa:** 1 (a regra em si é núcleo) — o teste automatizado desta regra é adição da Faixa 3, além do que a especificação original exige

**Acceptance Criteria:**

**Given** uma Viagem cuja data/hora de partida já passou
**When** tento criar uma Reserva para ela
**Then** recebo `409` com código `VIAGEM_REALIZADA`

**Given** uma Viagem com partida no futuro, ainda que a minutos dela
**When** tento reservar
**Then** é aceita — a restrição de 2 horas vale para cancelamento (Epic 4), não para venda

**Given** a comparação de data/hora
**When** a regra é avaliada
**Then** usa exclusivamente o Relógio da Aplicação (`IRelogio`), nunca chamada direta ao relógio do sistema

**Given** a regra `ViagemRealizada`
**When** testada em `OniBus.Domain.Tests`
**Then** existe teste determinístico dos dois lados da fronteira (viagem no passado recusada, no futuro aceita), via `IRelogio` injetável

**Given** que a busca (Epic 1) já omite Viagens Realizadas dos resultados
**When** um avaliador quer exercitar esta regra
**Then** só é possível via chamada direta à API com o id de uma viagem semeada como Realizada (seed da Story 1.4)

---

## Epic 4: Consulta e Cancelamento

O Passageiro consulta sua reserva pelo Código de Reserva e cancela sozinho, respeitando a Janela de Cancelamento de 2 horas antes da partida.

**FRs:** FR-11, FR-12, FR-13 · **NFRs:** NFR-6 · **UX-DRs:** UX-DR6

### Story 4.1: Consultar Reserva pelo Código de Reserva

As a titular de um Código de Reserva,
I want consultar minha reserva usando apenas o código,
So that eu veja os detalhes e decida o que fazer, sem precisar de conta ou login.

**Faixa:** 1 (API `GET /reservas/{codigo}`) + 2 (Tela 4 de consulta, superfície de interface)

**Acceptance Criteria:**

**Given** um Código de Reserva existente
**When** faço `GET /reservas/{codigo}`
**Then** recebo `200` com Viagem, Rota, data/hora de partida, número do assento, Status e dados do Passageiro

**Given** uma Reserva Cancelada
**When** consulto seu código
**Then** ela é retornada normalmente, com Status `Cancelada` — não escondida nem tratada como erro

**Given** um código inexistente
**When** consulto
**Then** recebo `404` uniforme

**Given** o código informado com letras minúsculas e/ou sem hífen
**When** consulto
**Then** a busca é insensível a caixa e tolera a ausência do hífen — normalização feita por um único ponto (`CodigoReserva.Parse`), o mesmo reaproveitado pelo cancelamento

**Given** a tela de consulta
**When** informo um código e busco
**Then** vejo os detalhes da reserva, com distinção visual clara entre Confirmada e Cancelada

**Given** uma Reserva Confirmada
**When** a tela exibe os detalhes
**Then** o botão de cancelar é mostrado (o refinamento de quando ele deve desaparecer por causa da Janela de Cancelamento é tratado na Story 4.2)

**Given** uma Reserva Cancelada
**When** a tela exibe os detalhes
**Then** nenhum botão de cancelar é mostrado

**Given** um código inexistente
**When** busco
**Then** a tela mostra mensagem explícita de "não encontrado", distinta do estado de erro de comunicação

### Story 4.2: Cancelar Reserva e fazer valer a Janela de Cancelamento

As a Passageiro,
I want cancelar minha reserva sozinho, desde que ainda dentro do prazo,
So that eu resolva o cancelamento sem depender de atendimento, mesmo fora do horário comercial.

**Faixa:** 1 (capacidade de cancelar e a regra da Janela) + 2 (Tela 4, superfície de interface)

**Acceptance Criteria:**

**Given** a regra `JanelaCancelamento` (2 horas antes da partida, fronteira exclusiva)
**When** testada isoladamente em `OniBus.Domain.Tests` — sem banco, sem host, **antes de qualquer alteração no endpoint `DELETE`**
**Then** há teste para os três pontos exatos da fronteira: 2h01min (aceito), 2h00min (recusado) e 1h59min (recusado)

**Given** uma Reserva Confirmada e uma Viagem que parte em mais de 2 horas
**When** faço `DELETE /reservas/{codigo}`
**Then** recebo `204`, o Status muda para `Cancelada` e o Assento volta a ficar Livre (verificável via `GET /viagens/{id}`)

**Given** uma Reserva Confirmada e uma Viagem que parte em menos de 2 horas (ou exatamente 2h00min)
**When** tento cancelar
**Then** recebo `409` com código `JANELA_CANCELAMENTO_FECHADA` e razão explícita

**Given** uma Reserva já Cancelada
**When** tento cancelar novamente, mesmo que a Viagem parta em menos de 2 horas
**Then** recebo `204` silenciosamente, sem erro — a idempotência de FR-12 tem precedência sobre a Janela, pois não há transição de Confirmada para Cancelada a impedir

**Given** um Código de Reserva inexistente
**When** tento cancelar
**Then** recebo `404`

**Given** a ordem de verificação do endpoint
**When** avaliada
**Then** segue exatamente: (1) existe? senão `404`; (2) já Cancelada? então `204` sem checar a janela; (3) só então a Janela de Cancelamento

**Given** a comparação de tempo
**When** a regra é avaliada
**Then** usa exclusivamente o Relógio da Aplicação (`IRelogio`)

**Given** a tela de consulta (Story 4.1)
**When** a Viagem está a menos de 2 horas da partida
**Then** o botão de cancelar não é exibido — refinando o comportamento da Story 4.1

**Given** uma tentativa de cancelamento recusada pela Janela
**When** ocorre
**Then** a tela exibe a razão explícita ao Passageiro, sem erro genérico

---

## Epic 5: Qualidade, Documentação e Entrega

Swagger navegável, pipeline de CI, README completo e submissão do repositório — os itens obrigatórios e pontuados pela avaliação do desafio, transversais a todos os épicos anteriores.

**Delivers:** DR-1, DR-2, DR-3, DR-4, DR-5, DR-6, DR-7, DR-8 · **NFRs:** NFR-4, NFR-8

### Story 5.1: Documentação de Endpoints via Swagger

As an avaliador técnico,
I want navegar pela documentação de todos os endpoints,
So that eu entenda a superfície da API sem precisar ler o código-fonte.

**Faixa:** 2 (o que decide a nota)

**Acceptance Criteria:**

**Given** a Api rodando
**When** acesso `/swagger` (com redirect configurado)
**Then** vejo a documentação OpenAPI navegável dos seis endpoints

**Given** cada endpoint documentado
**When** inspeciono
**Then** vejo os DTOs de requisição/resposta e os códigos de status possíveis (`200`/`201`/`204`/`400`/`404`/`409`)

**Given** o README
**When** leio
**Then** há um link direto para o Swagger

### Story 5.2: Pipeline de CI

As an avaliador técnico,
I want ver os testes rodando automaticamente a cada mudança,
So that eu confie que a suíte passa sem precisar executá-la localmente.

**Faixa:** 3 (diferenciação)

**Acceptance Criteria:**

**Given** um push ou pull request no repositório
**When** o GitHub Actions dispara
**Then** dois jobs rodam: backend (`dotnet test`, usando o Docker do runner para Testcontainers) e frontend (`npm ci` + `vitest run`)

**Given** qualquer um dos testes de regra de negócio falhando
**When** o job backend roda
**Then** o pipeline falha visivelmente

**Given** os três testes de frontend exigidos
**When** o job frontend roda
**Then** todos executam e o pipeline reporta o resultado

**Given** o pipeline configurado
**When** leio o README
**Then** há indicação de status do CI

### Story 5.3: README completo e execução validada sem Docker

As an avaliador técnico,
I want um README que me diga como rodar o projeto com e sem Docker, e por quê cada escolha foi feita,
So that eu suba o ambiente sem descobrir um passo manual não documentado, e entenda o julgamento por trás das decisões.

**Faixa:** 1 (DR-2, núcleo inegociável) + 2 (DR-8, execução sem Docker)

**Acceptance Criteria:**

**Given** o README
**When** leio
**Then** cobre os cinco itens obrigatórios: como rodar com e sem Docker, tecnologias e por quê, decisões de arquitetura relevantes, o que foi implementado e o que ficou de fora, como rodar os testes

**Given** cada divergência deliberada da especificação original (data de nascimento coletada, fronteira exclusiva de 2h no cancelamento, Viagem Realizada omitida da busca, Layout do Ônibus fixo em 44 assentos, Status com dois valores, limite de conformidade LGPD)
**When** leio o README
**Then** cada uma está documentada nominalmente

**Given** uma máquina com apenas os SDKs instalados (sem Docker)
**When** sigo os passos do README (banco local, string de conexão, comando de migração, comando da API, comando do frontend)
**Then** a aplicação sobe e o catálogo semeado está disponível, exatamente como no caminho com Docker

**Given** o README, seção de tecnologias
**When** leio
**Then** cada escolha da coluna "livre" da especificação (banco relacional, framework de teste .NET, gerenciador de estado do React, test runner do frontend, banco nos testes de integração) está justificada

### Story 5.4: Evidência visual, pontos de melhoria e submissão da entrega

As an avaliador técnico,
I want ver a aplicação em funcionamento, o histórico de construção, e o que foi conscientemente deixado de fora,
So that eu avalie o julgamento do candidato, não só o código que sobrou.

**Faixa:** 1 (DR-4, DR-7 — núcleo inegociável) + 3 (DR-5, DR-6 — diferenciação)

**Acceptance Criteria:**

**Given** o histórico de git
**When** inspecionado
**Then** mostra commits distribuídos em pelo menos três dias distintos, com mensagens que descrevem a mudança, e nenhum commit único contendo mais da metade do código

**Given** o README
**When** leio
**Then** há screenshots ou gif da aplicação rodando, cobrindo as quatro telas

**Given** o README, seção de pontos de melhoria
**When** leio
**Then** nomeia o que seria implementado com mais tempo (ex.: múltiplos assentos por reserva, layout de ônibus configurável) e todo item cortado das Faixas 2 a 4 do PRD (§10.3) aparece nominalmente ali

**Given** o repositório
**When** clonado em um diretório novo
**Then** sobe com `docker-compose up --build` sem nenhum ajuste manual

**Given** o repositório
**When** verificado
**Then** é público, acessível sem convite, e não contém nenhuma credencial real, string de conexão de ambiente real ou dado pessoal real — apenas um `.env.example` documentando o que existe

**Given** a submissão
**When** realizada
**Then** o link do repositório é enviado ao e-mail de recrutamento, declarando quais partes foram entregues (backend e frontend) e observações relevantes
