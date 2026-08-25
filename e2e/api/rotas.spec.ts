import { test, expect, log } from '../support/merged-fixtures';

type RotaResponse = {
  id: string;
  origem: string;
  destino: string;
  duracaoEstimadaMinutos: number;
};

test.describe('GET /api/rotas', () => {
  test('[P0] lista as Rotas semeadas (FR-1)', async ({ apiRequest }) => {
    await log.step('GET /api/rotas');

    const { status, body } = await apiRequest<RotaResponse[]>({
      method: 'GET',
      path: '/api/rotas',
    });

    expect(status).toBe(200);
    expect(Array.isArray(body)).toBe(true);
    // Nenhum schema formal existe ainda para RotaResponse; a asserção cobre só os campos que
    // FR-1 exercita. O seed atual (RotaSeed.cs) é provisório e será substituído pela Story 1.4 —
    // por isso a asserção procura a Rota conhecida em vez de fixar a contagem total.
    const saoPauloRio = body.find((rota) => rota.origem === 'São Paulo' && rota.destino === 'Rio de Janeiro');
    expect(saoPauloRio).toBeDefined();
    expect(saoPauloRio?.duracaoEstimadaMinutos).toBe(330);
    expect(typeof saoPauloRio?.id).toBe('string');
  });
});
