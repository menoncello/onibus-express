# Epic 1 Context: Fundação e Descoberta de Viagens

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Este épico entrega a fundação de toda a construção seguinte: o ambiente completo sobe com um único comando (Docker + Postgres + migrações + seed), e o Passageiro consegue buscar viagens por origem, destino e data, vendo o catálogo semeado — incluindo os casos de borda de viagem esgotada, realizada e parcialmente ocupada. O épico serve dois usuários com critérios de sucesso distintos: o Passageiro (busca funcional) e o avaliador técnico do desafio, para quem `docker-compose up --build` resultar em aplicação funcional em máquina limpa, sem nenhum passo manual, é um critério pontuado e binário. As histórias devem tornar esse segundo critério verificável explicitamente, não apenas implícito no resultado da busca.

## Stories

- Story 1.1: Esqueleto vivo — ambiente sobe com um comando e lista Rotas
- Story 1.2: Buscar Viagens por origem, destino e data
- Story 1.3: Ver detalhes de uma Viagem com o estado dos Assentos
- Story 1.4: Seed completo e idempotente com casos de borda

## Requirements & Constraints

- Listagem de Rotas é pública, sem autenticação, e alimenta os campos de busca.
- Busca de Viagens exige origem, destino e data — os três são obrigatórios; ausência de qualquer um retorna erro de campo inválido, nunca busca aberta. Busca sem correspondência retorna coleção vazia, nunca erro.
- Viagem já realizada (partida no passado) nunca aparece nos resultados de busca. Viagem sem assentos livres (esgotada) aparece, mas marcada como indisponível.
- Detalhe de uma Viagem deve identificar cada um dos 44 assentos e seu estado (Livre/Ocupado); id inexistente retorna 404 uniforme.
- Seed deve ser idempotente (por chave natural, nunca por contagem prévia) e usar datas relativas ao instante de execução, nunca literais de calendário — precisa sobreviver a um segundo `up` sem duplicar. Deve cobrir: viagem esgotada, viagem realizada, viagem parcialmente ocupada, viagem dentro e fora da janela de cancelamento (5 Rotas, 16 Viagens). As reservas do seed devem passar pelo gerador de código e pela validação de CPF reais, nunca valores escritos à mão. O seed deve funcionar de forma idêntica no caminho sem Docker.
- Toda tela que chama a API distingue quatro estados: carregando, sucesso (com/sem resultados), erro de entrada, falha de comunicação — vazio nunca é tratado como erro.
- Testes de comportamento de frontend (mock da API, nunca `fetch` global) são exigidos para o fluxo de busca.
- Reprodutibilidade: um único comando sobe o ambiente do zero em máquina limpa, sem passo manual não documentado — este é o critério de sucesso SM-1 do desafio, tratado como binário e pontuado.

## Technical Decisions

- Paradigma de fatias verticais: três projetos (`OniBus.Domain`, `OniBus.Api`, `web`), sem camadas `Infrastructure`/`Application`. `OniBus.Domain.csproj` não pode ter nenhum `PackageReference` nem `ProjectReference`.
- A primeira migração já deve conter os dois índices únicos que sustentam todo o resto da entrega: `ux_reservas_codigo` e `ux_reservas_viagem_assento_confirmada` (parcial, filtrado por `status = 'Confirmada'`) — mesmo antes de existir endpoint de reserva. Status é persistido como string.
- Disponibilidade de assento nunca é uma tabela própria: é sempre derivada das Reservas Confirmadas via função única `LayoutOnibus.EstadoDosAssentos`, dono único desse cálculo.
- `docker-compose.yml` com três serviços (web, api, db); `api` usa `depends_on: condition: service_healthy` sobre healthcheck `pg_isready` do `db` — a Api nunca tenta conectar antes do banco aceitar conexões.
- Migração e seed rodam no boot da Api, pelo mesmo caminho de código em ambos os ambientes (com ou sem Docker).
- Frontend nunca conhece URL absoluta da API: sempre `/api/*` via proxy reverso do Nginx, sem CORS e sem variável de ambiente de URL. Frontend é servido por Nginx, nunca por servidor de desenvolvimento.
- Instantes são tratados em UTC; dia de calendário é traduzido no fuso `America/Sao_Paulo` antes da consulta de busca.
- Recusas de API seguem `ProblemDetails` (RFC 9457) com vocabulário fechado de códigos (ex.: `CAMPO_INVALIDO`).
- Funções de formatação de preço (`Intl.NumberFormat` pt-BR/BRL) e de data/hora (`Intl.DateTimeFormat`) são centralizadas em `src/shared` e reutilizadas por todas as telas — nenhuma tela formata por conta própria; esta convenção é introduzida neste épico.
- Navegação da lista de viagens para o detalhe usa `viagemId` na própria URL (`/viagens/:viagemId/assentos`), nunca apenas em estado de aplicação em memória.
- Stack fixada relevante aqui: .NET 10, EF Core 10, PostgreSQL 18, Node 24, React 19.2, TypeScript 5, Vite 8.2, react-router 8.3 (não react-router-dom), Nginx 1.30.

## UX & Interaction Patterns

- Estado vazio da busca é um requisito de produto, não um fallback — precisa ser visualmente distinguível de carregando e de erro.
- Os quatro estados de tela (carregando, sucesso com/sem resultados, erro de entrada, falha de comunicação) se aplicam a toda tela que chama a API, incluindo busca e detalhe de viagem.

## Cross-Story Dependencies

- Story 1.1 estabelece o schema (índices únicos) e o esqueleto Docker que todas as demais stories deste épico e dos épicos seguintes (2 a 5) dependem para rodar.
- Story 1.3 (detalhe da viagem com estado dos assentos) é pré-requisito direto do Epic 2 (mapa de assentos), que reutiliza a mesma resposta sem chamada redundante.
- Story 1.4 (seed com casos de borda) é pré-condição para demonstrar as regras de concorrência (Epic 3, FR-9), viagem realizada (Epic 3, FR-10) e janela de cancelamento (Epic 4, FR-13), pois essas regras precisam de viagens semeadas nesses estados específicos para serem exercitadas.
- Este épico é paralelizável com o desenvolvimento do domínio puro (regras de CPF, janela de cancelamento, viagem realizada), que não toca infraestrutura.
