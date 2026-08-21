import { useEffect, useState } from 'react'
import { getRotas, type Rota } from '../../shared/api/rotas'

type Estado =
  | { tipo: 'carregando' }
  | { tipo: 'sucesso'; rotas: Rota[] }
  | { tipo: 'falha-comunicacao' }

function formatarDuracao(minutos: number): string {
  const horas = Math.floor(minutos / 60)
  const minutosRestantes = minutos % 60
  if (horas === 0) {
    return `${minutosRestantes}min`
  }
  if (minutosRestantes === 0) {
    return `${horas}h`
  }
  return `${horas}h${String(minutosRestantes).padStart(2, '0')}`
}

export function ListaDeRotas() {
  const [estado, setEstado] = useState<Estado>({ tipo: 'carregando' })

  useEffect(() => {
    let cancelado = false

    getRotas()
      .then((rotas) => {
        if (!cancelado) {
          setEstado({ tipo: 'sucesso', rotas })
        }
      })
      .catch(() => {
        // Qualquer falha em GET /rotas (rede, 5xx ou resposta inesperada) é comunicação, nunca
        // "sem resultados" — este endpoint não tem caminho de erro de entrada (é público, sem
        // parâmetros).
        if (!cancelado) {
          setEstado({ tipo: 'falha-comunicacao' })
        }
      })

    return () => {
      cancelado = true
    }
  }, [])

  if (estado.tipo === 'carregando') {
    return <p role="status">Carregando rotas…</p>
  }

  if (estado.tipo === 'falha-comunicacao') {
    // Distinto de "nenhuma rota encontrada" (AD-12): a mensagem nunca deve confundir
    // indisponibilidade do servidor com catálogo vazio.
    return (
      <p role="alert">
        Não foi possível carregar as rotas agora. Tente novamente em instantes.
      </p>
    )
  }

  if (estado.rotas.length === 0) {
    return <p>Nenhuma rota disponível no momento.</p>
  }

  return (
    <ul aria-label="Rotas disponíveis">
      {estado.rotas.map((rota) => (
        <li key={rota.id}>
          {rota.origem} → {rota.destino} ({formatarDuracao(rota.duracaoEstimadaMinutos)})
        </li>
      ))}
    </ul>
  )
}
