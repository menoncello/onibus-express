#!/usr/bin/env bash
# Verifica automaticamente o critério SM-1: um clone limpo sobe com `docker compose up --build`,
# sem nenhum passo manual, e o endpoint público GET /rotas responde com dados semeados.
#
# Cobre dois pontos da story 1.1 que não têm teste automatizado no nível de código:
#   - AC-1: os 3 serviços (web/api/db) sobem e a aplicação fica pronta sem intervenção manual.
#   - AC-3: a `api` só fica saudável depois que o `db` aceita conexões (depends_on: service_healthy).
#
# Uso: scripts/verify-compose-boot.sh
# Saída: 0 se o boot completou e GET /api/rotas respondeu 200 com uma lista não vazia; 1 caso
# contrário, com o motivo impresso em stderr.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

HEALTH_TIMEOUT_SECONDS=120
HEALTH_POLL_INTERVAL_SECONDS=2
CURL_MAX_TIME_SECONDS=15
ENDPOINT_URL="http://localhost/api/rotas"

echo "AVISO: ao terminar (sucesso ou falha), este script derruba o ambiente com 'docker compose down -v', removendo o volume 'db-data'. Não rode isto se houver dados locais importantes nesse volume."

cleanup() {
  echo "==> Derrubando o ambiente (docker compose down -v)"
  docker compose down -v >/dev/null 2>&1 || true
}
trap cleanup EXIT

echo "==> Subindo o ambiente a partir de um build limpo (docker compose up --build -d)"
docker compose up --build -d

wait_for_healthy() {
  local service="$1"
  local elapsed=0

  while true; do
    local container_id
    container_id="$(docker compose ps -q "$service")"

    if [ -z "$container_id" ]; then
      echo "ERRO: serviço '$service' não tem container em execução." >&2
      return 1
    fi

    local status
    status="$(docker inspect --format '{{.State.Health.Status}}' "$container_id" 2>/dev/null || echo "sem-healthcheck")"

    if [ "$status" = "healthy" ]; then
      echo "==> Serviço '$service' saudável após ${elapsed}s"
      return 0
    fi

    if [ "$status" = "unhealthy" ]; then
      echo "ERRO: serviço '$service' ficou 'unhealthy'." >&2
      docker compose logs "$service" >&2 || true
      return 1
    fi

    if [ "$elapsed" -ge "$HEALTH_TIMEOUT_SECONDS" ]; then
      echo "ERRO: serviço '$service' não ficou saudável em ${HEALTH_TIMEOUT_SECONDS}s (status atual: $status)." >&2
      docker compose logs "$service" >&2 || true
      return 1
    fi

    sleep "$HEALTH_POLL_INTERVAL_SECONDS"
    elapsed=$((elapsed + HEALTH_POLL_INTERVAL_SECONDS))
  done
}

echo "==> Aguardando 'db' aceitar conexões (pg_isready)"
wait_for_healthy db

echo "==> Aguardando 'api' ficar saudável (prova que ela só conectou ao banco depois dele estar pronto — AC-3)"
wait_for_healthy api

echo "==> Verificando GET /api/rotas"
if ! response="$(curl -s --max-time "$CURL_MAX_TIME_SECONDS" -w '\n%{http_code}' "$ENDPOINT_URL")"; then
  echo "ERRO: não foi possível obter resposta de $ENDPOINT_URL em até ${CURL_MAX_TIME_SECONDS}s (conexão recusada, timeout, ou outra falha de rede)." >&2
  exit 1
fi
status_code="$(echo "$response" | tail -n1)"
body="$(echo "$response" | sed '$d')"

if [ "$status_code" != "200" ]; then
  echo "ERRO: GET /api/rotas retornou HTTP $status_code, esperado 200. Corpo: $body" >&2
  exit 1
fi

trimmed_body="$(echo "$body" | tr -d '[:space:]')"
if [[ "$trimmed_body" != \[*\] || "$trimmed_body" == "[]" ]]; then
  echo "ERRO: GET /api/rotas não retornou uma lista JSON não vazia. Corpo: $body" >&2
  exit 1
fi

echo "==> SM-1 verificado: ambiente subiu com um único comando, sem passo manual, e GET /api/rotas respondeu 200 com Rotas semeadas."
