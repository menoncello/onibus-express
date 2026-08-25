import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import * as rotasApi from '../../shared/api/rotas'
import { ListaDeRotas } from './ListaDeRotas'

// AD-10: a única costura de mock no frontend é o cliente tipado de src/shared/api — nunca o
// `fetch` global.
vi.mock('../../shared/api/rotas')

afterEach(() => {
  // Com `globals: false` no Vitest, o auto-cleanup do Testing Library (que depende de um
  // `afterEach` global) não dispara por conta própria — sem isto, o render de um teste vaza
  // para o DOM do próximo.
  cleanup()
  vi.restoreAllMocks()
})

const criarRota = (overrides: Partial<rotasApi.Rota> = {}): rotasApi.Rota => ({
  id: '1',
  origem: 'São Paulo',
  destino: 'Rio de Janeiro',
  duracaoEstimadaMinutos: 330,
  ...overrides,
})

describe('ListaDeRotas', () => {
  it('mostra a lista de rotas apos o carregamento com sucesso', async () => {
    vi.spyOn(rotasApi, 'getRotas').mockResolvedValue([criarRota()])

    render(<ListaDeRotas />)

    const item = await screen.findByText(/São Paulo/)
    expect(item.textContent).toContain('Rio de Janeiro')
  })

  it('distingue falha de comunicacao de lista vazia', async () => {
    vi.spyOn(rotasApi, 'getRotas').mockRejectedValue(new Error('rede fora do ar'))

    render(<ListaDeRotas />)

    const alerta = await screen.findByRole('alert')
    expect(alerta.textContent).toMatch(/não foi possível carregar/i)
  })

  it('mostra estado vazio sem alerta quando a lista de rotas esta vazia (AD-12)', async () => {
    vi.spyOn(rotasApi, 'getRotas').mockResolvedValue([])

    render(<ListaDeRotas />)

    const vazio = await screen.findByText(/nenhuma rota disponível/i)
    expect(vazio).toBeTruthy()
    expect(screen.queryByRole('alert')).toBeNull()
  })

  it('mostra estado de carregando antes do fetch resolver (AD-12)', async () => {
    let resolverPromise: ((rotas: rotasApi.Rota[]) => void) | undefined
    const promisePendente = new Promise<rotasApi.Rota[]>((resolve) => {
      resolverPromise = resolve
    })
    vi.spyOn(rotasApi, 'getRotas').mockReturnValue(promisePendente)

    render(<ListaDeRotas />)

    expect(screen.getByRole('status').textContent).toBe('Carregando rotas…')

    resolverPromise!([])
    await screen.findByText(/nenhuma rota disponível/i)
  })

  it('formata duracao de hora exata e de sub-hora sem minutos/horas sobrando', async () => {
    vi.spyOn(rotasApi, 'getRotas').mockResolvedValue([
      criarRota({ id: '1', origem: 'Curitiba', destino: 'Florianópolis', duracaoEstimadaMinutos: 60 }),
      criarRota({ id: '2', origem: 'Recife', destino: 'Natal', duracaoEstimadaMinutos: 45 }),
    ])

    render(<ListaDeRotas />)

    await screen.findByText(/Curitiba/)
    expect(screen.getByText('Curitiba → Florianópolis (1h)')).toBeTruthy()
    expect(screen.getByText('Recife → Natal (45min)')).toBeTruthy()
  })
})
