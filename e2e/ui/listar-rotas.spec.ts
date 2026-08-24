import { test, expect } from '../support/merged-fixtures';

test.describe('Lista de Rotas (FR-1)', () => {
  test('[P0] exibe as Rotas semeadas ao carregar a página', async ({ page, interceptNetworkCall }) => {
    const rotasCall = interceptNetworkCall({ url: '**/api/rotas' });

    await page.goto('/');

    const { status, responseJson } = await rotasCall;
    expect(status).toBe(200);

    const lista = page.getByRole('list', { name: 'Rotas disponíveis' });
    await expect(lista.getByRole('listitem')).toHaveCount(responseJson.length);
    await expect(lista.getByText('São Paulo → Rio de Janeiro (5h30)')).toBeVisible();
  });

  test(
    '[P2] mostra falha de comunicação em 5xx, nunca como "sem resultados" (AD-12)',
    { annotation: [{ type: 'skipNetworkMonitoring' }] }, // 5xx é o cenário testado, não um erro real
    async ({ page, interceptNetworkCall }) => {
      const rotasCall = interceptNetworkCall({
        url: '**/api/rotas',
        fulfillResponse: { status: 500, body: { title: 'Erro interno' } },
      });

      await page.goto('/');
      await rotasCall;

      // client.ts trata todo status >= 500 como ApiComunicacaoError — o teste existe para provar
      // que esse estado nunca renderiza como "nenhuma Rota disponível" (AD-12).
      await expect(page.getByRole('alert')).toHaveText(/não foi possível carregar as rotas/i);
    },
  );
});
