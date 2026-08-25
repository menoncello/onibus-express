import { mergeTests } from '@playwright/test';
import { log } from '@seontechnologies/playwright-utils';
import { test as apiRequestFixture } from '@seontechnologies/playwright-utils/api-request/fixtures';
import { test as interceptFixture } from '@seontechnologies/playwright-utils/intercept-network-call/fixtures';
import { test as networkErrorFixture } from '@seontechnologies/playwright-utils/network-error-monitor/fixtures';
import { test as recurseFixture } from '@seontechnologies/playwright-utils/recurse/fixtures';

// playwright-utils deviation: sem authFixture — o sistema não tem autenticação,
// contas nem sessão (Non-Goal explícito do PRD/ARCHITECTURE-SPINE.md); o Código
// de Reserva é identificador de negócio, não credencial de sessão.
export const test = mergeTests(apiRequestFixture, interceptFixture, networkErrorFixture, recurseFixture);

export { expect } from '@playwright/test';
export { log };
