import { getJson } from './client'

export interface Rota {
  id: string
  origem: string
  destino: string
  duracaoEstimadaMinutos: number
}

export function getRotas(): Promise<Rota[]> {
  return getJson<Rota[]>('/rotas')
}
