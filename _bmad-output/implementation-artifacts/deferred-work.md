- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
  summary: Root `README.md` documentando como rodar o projeto (com/sem Docker) ainda não existe.
  evidence: Já é entrega explícita de Epic 5 / Story 5.3 ("README completo e execução validada sem Docker"); não é AC desta story.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
  summary: Pipeline de CI (GitHub Actions) rodando `dotnet test`/`vitest run` ainda não existe.
  evidence: Já é entrega explícita de Epic 5 / Story 5.2 ("Pipeline de CI"); não é AC desta story.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
  summary: `ListaDeRotas` não usa `AbortController` para cancelar o fetch em andamento no unmount, só ignora o resultado via flag `cancelado`.
  evidence: Robustez válida para navegação futura entre telas (Epic 2+), mas não há AC ou risco atual nesta story de página única.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
  summary: Entidades de domínio (`Reserva`, etc.) ainda não impõem invariantes como faixa de `NumeroAssento` (1..44) ou forma canônica de `Codigo`.
  evidence: Já documentado no Change Log do spec como decisão intencional — CPF, `Passageiro` inline (AD-5) e `IGeradorCodigoReserva`/forma canônica (AD-8) chegam com a story que introduz `POST /reservas`.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
  summary: `ASPNETCORE_ENVIRONMENT` não é definido para o serviço `api` no `docker-compose.yml`, então endpoints de OpenAPI/Swagger (se gated por `IsDevelopment()`) não ficam acessíveis no caminho Docker.
  evidence: Relevante para Epic 5 / Story 5.1 ("Documentação de endpoints via Swagger"), que precisa decidir se Swagger é exposto também fora de Development; não é AC desta story.

- source_spec: `_bmad-output/implementation-artifacts/spec-1-1-esqueleto-vivo-ambiente-sobe-com-um-comando-e-lista-rotas.md`
  summary: Nenhum teste automatizado exercita a stack `nginx → api → db` completa via compose (proxy `/api`, strip de prefixo).
  evidence: Já verificado manualmente para esta story (`docker-compose up --build` + curl + checagem no browser); automação de ponta a ponta é natural em Epic 5 (CI).
