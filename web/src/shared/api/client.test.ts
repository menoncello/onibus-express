import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiComunicacaoError, ApiRespostaInesperadaError } from './client'
import { getRotas } from './rotas'

// Diferente de ListaDeRotas.test.tsx (que mocka o módulo `rotas`), este arquivo mocka apenas
// `fetch` global para exercitar o próprio cliente: prefixo `/api`, mapeamento do JSON e a
// classificação de status code em erro de comunicação vs. erro de resposta inesperada.

function stubFetch(response: Partial<Response> & { json?: () => Promise<unknown> }) {
  const fetchMock = vi.fn().mockResolvedValue(response as Response)
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('getRotas (via cliente HTTP tipado)', () => {
  it('mapeia uma resposta de sucesso para Rota[] chamando /api/rotas', async () => {
    const corpo = [
      { id: '1', origem: 'São Paulo', destino: 'Rio de Janeiro', duracaoEstimadaMinutos: 330 },
    ]
    const fetchMock = stubFetch({
      ok: true,
      status: 200,
      json: () => Promise.resolve(corpo),
    })

    const rotas = await getRotas()

    expect(fetchMock).toHaveBeenCalledWith('/api/rotas')
    expect(rotas).toEqual(corpo)
  })

  it('resolve uma resposta 5xx para erro de comunicacao', async () => {
    stubFetch({ ok: false, status: 503 })

    await expect(getRotas()).rejects.toBeInstanceOf(ApiComunicacaoError)
  })

  it('resolve uma resposta nao-2xx/nao-5xx (404) para erro de resposta inesperada', async () => {
    stubFetch({ ok: false, status: 404 })

    const erro = await getRotas().catch((e) => e)

    expect(erro).toBeInstanceOf(ApiRespostaInesperadaError)
    expect((erro as ApiRespostaInesperadaError).status).toBe(404)
  })
})
