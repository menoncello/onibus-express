/**
 * Cliente HTTP tipado único (AD-12): concentra `fetch`, caminho relativo `/api/*` (AD-11 — o
 * frontend nunca conhece URL absoluta da API, nem em Docker nem no proxy de dev do Vite) e a
 * distinção entre falha de comunicação (rede/5xx) e recusa de entrada.
 *
 * O vocabulário fechado de `codigo` (ProblemDetails, AD-9) chega a este cliente nas stories que
 * introduzem endpoints com recusa de negócio; `GET /rotas` é público e não recusa entrada.
 */

export class ApiComunicacaoError extends Error {
  constructor(cause?: unknown) {
    super('Falha de comunicação com a API')
    this.name = 'ApiComunicacaoError'
    this.cause = cause
  }
}

export class ApiRespostaInesperadaError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`Resposta inesperada da API (status ${status})`)
    this.name = 'ApiRespostaInesperadaError'
    this.status = status
  }
}

export async function getJson<T>(caminho: string): Promise<T> {
  let response: Response

  try {
    response = await fetch(`/api${caminho}`)
  } catch (erro) {
    // Rede indisponível, DNS, conexão recusada etc. — nunca confundir com "sem resultados".
    throw new ApiComunicacaoError(erro)
  }

  if (response.status >= 500) {
    throw new ApiComunicacaoError()
  }

  if (!response.ok) {
    throw new ApiRespostaInesperadaError(response.status)
  }

  try {
    return (await response.json()) as T
  } catch (erro) {
    // Corpo vazio ou JSON inválido com status 2xx: ainda é falha de comunicação, nunca deve
    // escapar como SyntaxError cru para quem chamou.
    throw new ApiComunicacaoError(erro)
  }
}
