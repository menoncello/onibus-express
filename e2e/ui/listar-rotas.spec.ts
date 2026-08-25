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

  test('[P1] mostra estado de carregando enquanto /api/rotas ainda não respondeu (AD-12)', async ({ page, interceptNetworkCall }) => {
    interceptNetworkCall({
      url: '**/api/rotas',
      // Atraso deliberado para manter a janela de "carregando" aberta o suficiente para a
      // asserção observar o estado antes da resposta chegar — não é um hard wait de teste
      // (nenhuma asserção depende de um tempo fixo, só a resposta mockada é que demora). 1500ms
      // dá margem folgada acima do tempo de navegação/render em CI mais lento, bem abaixo do
      // timeout padrão de asserção (10s, playwright.config.ts).
      handler: async (route) => {
        await new Promise((resolve) => setTimeout(resolve, 1500));
        await route.fulfill({ status: 200, body: JSON.stringify([]) });
      },
    });

    await page.goto('/');

    await expect(page.getByRole('status')).toHaveText('Carregando rotas…');

    // Espera determinística pela transição de estado (nunca um waitForTimeout) — o mock some
    // depois de ~1.5s e a tela deve trocar para o estado de sucesso vazio (AD-12).
    await expect(page.getByText(/nenhuma rota disponível/i)).toBeVisible();
  });

  test('[P1] mostra estado vazio sem alerta quando /api/rotas responde sem nenhuma Rota (AD-12)', async ({ page, interceptNetworkCall }) => {
    const rotasCall = interceptNetworkCall({
      url: '**/api/rotas',
      fulfillResponse: { status: 200, body: [] },
    });

    await page.goto('/');
    await rotasCall;

    await expect(page.getByText(/nenhuma rota disponível/i)).toBeVisible();
    await expect(page.getByRole('alert')).toHaveCount(0);
  });
});
