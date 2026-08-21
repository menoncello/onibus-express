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

describe('ListaDeRotas', () => {
  it('mostra a lista de rotas apos o carregamento com sucesso', async () => {
    vi.spyOn(rotasApi, 'getRotas').mockResolvedValue([
      { id: '1', origem: 'São Paulo', destino: 'Rio de Janeiro', duracaoEstimadaMinutos: 330 },
    ])

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
})
