---
title: "Revisão Adversarial — PRD OniBus Express"
created: 2026-08-18
reviewer: adversarial-general
target: prd.md (+ addendum.md)
canonical_source: ../../briefs/brief-ticket-busao-2026-08-18/addendum.md
---

# Revisão Adversarial — PRD OniBus Express

**Veredito:** o PRD é bem escrito e cobre quase toda a superfície *nominal* da especificação, mas falha nos três lugares onde um PRD de desafio técnico precisa ser rígido: (a) ele **não registra a stack obrigatória** em nenhum lugar do próprio documento, (b) ele **contradiz a si mesmo** em pontos que decidem se a Tela 4 existe e se os testes exigidos são escrevíveis, e (c) ele declara escopo máximo para uma semana **sem nenhuma ordem de corte**, apesar de o brief upstream ter uma. Há também um bug de dado semeado que mata a demonstração exatamente quando o avaliador for rodar.

Contagem: **6 Crítico · 13 Alto · 12 Médio · 7 Baixo**.

---

## CRÍTICO

### C1. §7 torna a Tela 4 e a UJ-2 impossíveis — Reserva Cancelada não pode ser exibida

**Local:** §7 Restrições, 2º bullet; FR-11, 2ª consequência; FR-12; §2.3 UJ-2.

> §7: "FR-11 não vaza existência de Reserva: código inexistente e código cancelado respondem de forma indistinguível."
> FR-11: "Código inexistente retorna 'não encontrado', sem revelar se o código jamais existiu ou foi cancelado."

Isso é uma regra dura, não uma nuance de mensagem: se código cancelado responde como inexistente, então **`GET /reservas/{codigo}` nunca pode retornar `Status = Cancelada`**. Consequências em cascata, todas contraditórias com o restante do documento:

- FR-11, 1ª consequência exige "Status" na resposta — campo que passa a ter um único valor possível (`Confirmada`). Morto por construção.
- UJ-2 diz "**Clímax:** o status vira Cancelada e o assento volta a ficar livre" — a tela não tem como mostrar isso; o usuário cancela e a consulta seguinte diz "não encontrado", o que lê como "perdi minha reserva", não como "cancelada com sucesso".
- FR-12 exige idempotência ("Cancelar Reserva já Cancelada não gera efeito nem novo erro de estado") — impossível de implementar/observar se a Reserva cancelada é irrecuperável pelo código.
- A especificação original pede na Tela 4: "Exibir detalhes **ou** opção de cancelamento". Detalhes de uma reserva cancelada é o caso mais óbvio de "detalhes sem cancelamento".

**Por que importa:** um implementador que siga §7 literalmente entrega uma Tela 4 que responde 404 após qualquer cancelamento. Um que siga FR-11/UJ-2 viola um guardrail declarado do PRD. Os dois caminhos são defensáveis e mutuamente exclusivos — é exatamente o tipo de ambiguidade que produz retrabalho na última noite.

**Correção:** remover o guardrail. Reserva Cancelada retorna 200 com `Status = Cancelada`; apenas código **inexistente** retorna "não encontrado". Se houver desejo genuíno de não vazar informação, o alvo correto é outro (ver A9), não o status. Atualizar §7, FR-11 e DR-2 (deixa de ser divergência a documentar).

---

### C2. A stack obrigatória não aparece em nenhum lugar do PRD

**Local:** §7 "Restrições e Guardrails" (contém **apenas** privacidade); ausência em §5, §6, §10.

A especificação original lista tecnologias **obrigatórias** (spec §3.1 e §4.1): .NET 8+ ASP.NET Core Web API; Entity Framework Core; banco relacional PostgreSQL **ou** SQL Server; xUnit **ou** NUnit; React 18+ **com TypeScript**; React Testing Library + Jest **ou** Vitest; Docker para servir o frontend. Buscando no PRD: "React", "TypeScript", ".NET", "ASP.NET", "Entity Framework", "xUnit", "NUnit", "Vitest", "React Testing Library", "PostgreSQL", "SQL Server" — **nenhum desses termos existe no corpo do prd.md**. Só "Nginx ou equivalente" (DR-1) e "Swagger" (DR-3) sobreviveram.

O PRD justifica a omissão em §0: "Insumos a montante — este PRD constrói sobre eles e não os duplica". Mas §0 também declara que o PRD é "o contrato de **o quê**" e que existe "para que arquitetura, épicos e implementação partam de um vocabulário único... em vez de reinterpretar a especificação original a cada etapa". Restrição obrigatória não é duplicação: é a parte do contrato que não pode ser reinterpretada.

**Por que importa (build errado concreto):** um arquiteto lendo só o PRD pode escolher Dapper em vez de EF Core (nada no PRD exige ORM), Minimal API + SQLite como banco de produção (nada exige PostgreSQL/SQL Server), React **sem** TypeScript (nada exige TS), Next.js com SSR em vez de SPA servida por Nginx, ou MSTest. Qualquer uma dessas é **reprovação automática** num desafio cuja stack avaliada é fixa. E o §11 não tem nenhuma métrica que detecte a violação.

**Correção:** criar §7.0 "Restrições Tecnológicas (dadas, não escolhidas)" com a lista literal da spec §3.1/§4.1, marcando o que é obrigatório (EF Core, .NET 8+, React 18+, TypeScript, Docker) e o que é escolha entre opções fechadas (PostgreSQL|SQL Server, xUnit|NUnit, Jest|Vitest, RTL obrigatório). Adicionar SM: "a stack entregue casa item a item com a lista obrigatória".

---

### C3. "As quatro regras de negócio" — a spec tem cinco, e os dois conjuntos de "quatro" do PRD não coincidem

**Local:** §10.1; SM-3; brief §"A Solução"; spec original §3.3.

A spec §3.3 lista **cinco** bullets: assento ocupado, viagem realizada, CPF validado, código único e legível, cancelamento até 2h. O PRD repete "quatro regras" quatro vezes e monta dois conjuntos diferentes:

> §10.1: "As quatro regras de negócio exigidas: **FR-6, FR-9, FR-10, FR-13**."
> SM-3: "Quatro regras, quatro testes que provam a regra... Valida **FR-6, FR-8, FR-9, FR-13**."

FR-8 (código único) e FR-10 (viagem realizada) trocam de lugar entre as duas listas. O resultado prático é que **FR-10 fica órfão de verificação**:

- não tem teste exigido (SM-3 não o inclui);
- não é alcançável pela interface (FR-2 removeu Viagem Realizada da busca);
- SM-2 afirma validar "FR-1 a FR-13", o que é falso para FR-10 (ver M4);
- sobra apenas "exercitável apenas via API" (FR-2) — ou seja, ninguém verifica, e a regra pode ir quebrada para o avaliador.

**Por que importa:** FR-10 é uma das regras que a spec lista explicitamente. É a mais fácil de implementar e, neste PRD, a mais fácil de esquecer — a combinação exata que produz "faltou uma regra de negócio" no feedback do avaliador.

**Correção:** parar de dizer "quatro regras". Escrever: "as cinco regras da spec §3.3 → FR-6, FR-8, FR-9, FR-10, FR-13" e "os quatro testes exigidos pela spec §3.4 → FR-6, FR-8, FR-9, FR-13". Adicionar consequência testável a FR-10 (teste de unidade/integração, mesmo não exigido — custa 10 linhas) e corrigir SM-3 para nomear os cinco.

---

### C4. Datas semeadas fixas fazem a demonstração morrer antes de o avaliador abrir o repo

**Local:** FR-14, todas as consequências; DR-1; SM-1; §12 Questão Aberta 5.

> FR-14: "Inclui ao menos uma Viagem fora da Janela de Cancelamento e uma Viagem Realizada, tornando FR-10 e FR-13 demonstráveis manualmente."

O PRD exige um seed com **três classes temporais de Viagem** (futura reservável, dentro de 2h da partida, já partida) e nunca diz que essas datas devem ser **relativas ao momento da execução**. Um implementador escreve `new DateTime(2026, 8, 22, 8, 0, 0)` na migration/seed — o comportamento default e mais natural. Duas semanas depois, quando o avaliador clona e roda:

- toda Viagem virou Viagem Realizada;
- FR-2 (que esconde Viagem Realizada, por decisão do próprio PRD) retorna **coleção vazia** para qualquer busca;
- o avaliador vê o estado vazio da Tela 1 e conclui que o produto não funciona;
- SM-1 ("ambiente sobe em um comando... aplicação funcional. Binário") passa no dia do commit e falha no dia da avaliação.

**Por que importa:** este é o único defeito da lista que reprova a entrega inteira sem nenhum código estar errado. E o PRD o torna *mais* provável ao esconder Viagem Realizada da busca (FR-2) — sem essa decisão, o avaliador ainda veria viagens passadas e entenderia o problema.

**Correção:** consequência testável nova em FR-14: "Todas as datas/horas semeadas são calculadas em relação ao instante da inicialização (ex.: `agora + 3 dias`, `agora + 90 minutos`, `agora - 2 dias`), nunca literais de calendário. Verificável rodando `docker-compose up --build` com o relógio adiantado em 30 dias: FR-2 continua retornando resultados." Adicionar a SM-1.

---

### C5. Nenhum requisito de relógio injetável — os testes de fronteira exigidos não são escrevíveis de forma determinística

**Local:** FR-13, última consequência; FR-10; §12 Questão Aberta 1; §4.4 `[NOTE FOR PM]`.

> FR-13: "O teste do limite cobre os três pontos da fronteira — 2h01min, 2h00min e 1h59min — não apenas o caso confortável."

Este é um dos quatro testes que a spec exige (§3.4, "Regra de cancelamento dentro do prazo") e é o mais dependente de infraestrutura de teste. Sem uma abstração de tempo declarada como requisito, o implementador chama `DateTime.UtcNow` dentro da regra — caminho default em .NET — e então:

- o teste dos três pontos só é possível manipulando as datas da **Viagem** para trás/frente, o que funciona, mas mistura arranjo de dados com a regra e não cobre "Viagem já partiu" de forma limpa;
- qualquer teste que envolva "exatamente 2h00min" fica intrinsecamente frágil (o tempo passa entre o arrange e o act);
- a Questão Aberta 1 (UTC vs fuso local vs hora do servidor) permanece aberta **e é insumo de dois dos quatro testes exigidos** — ou seja, o PRD deixa não resolvido justamente o que bloqueia o item mais pontuado da §3.4.

**Por que importa:** o PRD trata "teste que falha se a regra for removida" (SM-3) como a métrica de maior peso e não cria a condição que a torna alcançável. O sintoma real será um teste que passa por acidente ou um teste flaky que o candidato desabilita na sexta-feira.

**Correção:** adicionar consequência a FR-10 e FR-13: "a referência de tempo é obtida de uma abstração injetável (ex.: `IClock`/`TimeProvider`), nunca de chamada estática direta, para que os três pontos da fronteira sejam testáveis sem manipular dados". Promover a Questão Aberta 1 de "levar para arquitetura" a **bloqueador de FR-13**, com decisão default proposta (UTC persistido, comparação em UTC, exibição em `America/Sao_Paulo`).

---

### C6. Escopo máximo declarado para uma semana, sem nenhuma ordem de corte — e o brief tinha uma

**Local:** §10.1 (lista integral em escopo); §11 (nove SMs, nenhuma priorizada); §13 item 11; brief §Escopo, último `[ASSUMPTION]`.

O brief upstream é explícito:

> brief: "**[ASSUMPTION]** A camada de diferenciação é cortável sob pressão de tempo, o núcleo não. Se a semana apertar, corta-se de baixo para cima na lista de diferenciação e declara-se o corte no README."

O PRD **descarta esse mecanismo**. §10.1 coloca tudo em escopo — 14 FRs, 7 DRs, 4 telas, 7 testes exigidos, teste de concorrência com banco real, seed em três classes temporais, Swagger, screenshots/gif, README com 5+3 itens, histórico de commits disciplinado — e §11 declara nove métricas sem peso, reforçado por §13 item 11: "os oito são tratados como igualmente obrigatórios" (que também está errado; ver M1). Em nenhum ponto o PRD diz o que sai primeiro.

Para uma pessoa em ~1 semana, o que realisticamente cai é previsível: DR-5 (gif/screenshots), o teste de concorrência do SM-4 (que exige TestContainers — ver A2), a Tela 4, a preservação de contexto do FR-9 (A4) e o caminho "sem Docker" do README (A11). Nenhum desses tem um plano de degradação declarado, então o corte acontecerá por exaustão, não por decisão — e §11 SM-8 ("todo corte está declarado") vira dívida de última hora.

**Por que importa:** o PRD afirma em §1 que "o escopo é fechado de propósito", mas o escopo declarado é o máximo possível. É o ponto de autoengano mais consequente do documento: prosa de contenção sobre um plano expansivo.

**Correção:** §10.1 recebe três faixas explícitas — **P0 (contrato com a spec, nunca corta)**, **P1 (diferenciais nomeados no brief)**, **P2 (cortável, com texto de README pré-escrito)** — mais uma linha de sequenciamento: "nenhum item P1 começa antes de todo P0 verificado por SM-1..SM-3, SM-5". Nomear explicitamente o que é sacrificado primeiro e onde isso é declarado (DR-6).

---

## ALTO

### A1. O princípio declarado em FR-2 não é aplicado à Viagem esgotada — e o comportamento com 0 vagas não existe no PRD

**Local:** FR-2, 4ª consequência; FR-4; FR-5.

> FR-2: "**Viagem Realizada não aparece nos resultados de busca** — oferecer o que não pode ser reservado é ruído."

Uma Viagem com 44 Assentos Ocupados também não pode ser reservada, e o PRD a mantém na busca (a spec pede "vagas restantes", que pode ser 0). O princípio é aplicado a um caso e ignorado no outro, sem justificativa. Pior: nenhum FR define o que acontece ao selecioná-la — FR-4 renderiza 44 Assentos Ocupados, FR-5 mantém o botão bloqueado ("permanece bloqueado enquanto nenhum Assento estiver selecionado"), e o Passageiro fica num beco sem saída sem mensagem.

**Correção:** decidir e escrever: ou Viagem esgotada é filtrada da busca (coerente com o princípio) ou aparece marcada como "Esgotada", não clicável. Adicionar consequência a FR-2 e a FR-4 ("Viagem sem Assentos Livres exibe estado explícito de esgotada; o CTA de prosseguir é inalcançável por construção"). Incluir uma Viagem esgotada no seed (FR-14).

### A2. §6 "domínio testável sem infraestrutura" contradiz FR-9 "a garantia é do banco"

**Local:** §6, bullets 1 e 6; FR-9, 2ª consequência; SM-3; SM-4; addendum §2 (linha "Banco nos testes de integração").

> §6: "**Domínio isolado.** As regras de negócio ficam testáveis sem subir infraestrutura."
> FR-9: "A garantia é do banco de dados — restrição de unicidade sobre (Viagem, Assento) entre Reservas Confirmadas — e não de verificação em memória antes da escrita."

Se a regra de assento ocupado é uma restrição de banco, ela **não** é testável sem infraestrutura, e SM-3 ("teste que falha se a regra for removida") passa a significar "teste que falha se o índice for removido" — só verificável contra banco real. O próprio addendum admite: "SQLite in-memory não reproduz o comportamento de concorrência que FR-9 exige provar... TestContainers é praticamente obrigatório". Isso é Docker-in-test, tempo de setup real, e colide com C6.

**Correção:** separar as duas afirmações. §6: "as regras puramente lógicas (FR-6, FR-13, FR-8-formato) são testáveis sem infraestrutura; FR-9 é deliberadamente uma invariante de banco e tem teste de integração contra banco real (TestContainers), aceito como custo". Declarar em SM-3 quais dos quatro testes são unitários e quais são de integração, e em §10.1 que TestContainers está em escopo (com fallback declarado se a semana apertar).

### A3. FR-8 "único" não é testável como escrito; o retry de colisão é intestável

**Local:** FR-8, consequências 2 e 3; SM-3.

> FR-8: "Duas Reservas nunca compartilham o mesmo código — garantido por restrição de unicidade no banco, não apenas por probabilidade no código da aplicação."
> FR-8: "Colisão na geração é tratada com nova tentativa, sem propagar erro ao Passageiro."

O teste exigido pela spec §3.4 é "Geração do código de reserva único". Um teste que gera N códigos e verifica distinção **passa trivialmente** por probabilidade (1,7 bilhão de combinações) e não falha se a regra for removida — violando SM-3 diretamente. E o caminho de retry só é exercitável com um gerador determinístico injetável, que o PRD não exige.

**Correção:** consequência nova em FR-8: "a geração do código é uma dependência injetável, para que o teste force uma colisão determinística e verifique (a) que a segunda tentativa produz código distinto e (b) que nenhum erro chega ao Passageiro". Reescrever a consequência de unicidade como testável: "inserir duas Reservas com o mesmo código diretamente contra o banco viola a restrição" (teste de integração de uma linha, que realmente falha se o índice sair).

### A4. FR-9 esconde um requisito de frontend grande, sem teste e sem modelo de navegação

**Local:** FR-9, última consequência; §2.3 UJ-1 caso de borda; addendum §3 bullet 2.

> FR-9: "Recusa preserva os dados já digitados no formulário e recarrega o Mapa de Assentos com o estado atual."

Isso exige: estado compartilhado sobrevivendo à navegação entre Tela 3 e Tela 2, re-fetch de `GET /viagens/{id}`, tradução do erro 409 em uma transição de UI específica, e retorno à Tela 3 com os campos intactos após nova seleção. É o requisito de frontend mais complexo do PRD, e não há: teste exigido (os três testes de frontend não o cobrem), definição de para onde o usuário vai, nem menção em §10.1. Ele é o primeiro candidato a cair (C6) e não está marcado como cortável.

**Correção:** ou promover a FR próprio com consequências navegáveis explícitas e um 4º teste de frontend, ou rebaixá-lo a P2 declarado em §10.1 com fallback definido ("mensagem de erro e retorno ao mapa; perda dos dados aceita e declarada no README"). Escolher — não deixar como cláusula subordinada.

### A5. Validação do formulário além do CPF não é requisito em nenhum lugar

**Local:** FR-6 (só CPF); FR-7, 4ª consequência (só "campo obrigatório ausente", no servidor); spec §4.2 Tela 3; spec §4.3 teste 3.

A spec exige, na Tela 3: "**Validação dos campos no frontend**" (plural) e um teste "de validação do formulário de passageiro". O PRD especifica só CPF. Não há requisito para: formato de e-mail, nome não vazio / tamanho mínimo, e — ironicamente — **validade da data de nascimento**, campo que o próprio PRD adicionou por decisão deliberada (FR-7). Um implementador entrega validação de CPF e um `required` de HTML nos demais, e o teste de frontend exigido testa só CPF.

**Correção:** consequências novas em FR-7: e-mail validado por formato no cliente e no servidor; nome obrigatório com limite de tamanho; data de nascimento obrigatória, no passado, e com idade plausível (ex.: ≤ 120 anos). Declarar que o teste de frontend de validação cobre ao menos CPF inválido + e-mail inválido + campo vazio.

### A6. O contrato de requisição de `GET /viagens` não existe — origem/destino são texto livre ou id de Rota?

**Local:** §5 (tabela sem parâmetros); FR-2; FR-1, 2ª consequência.

> FR-1: "Serve para popular os campos de origem e destino da busca, evitando que o Passageiro digite um trecho inexistente."

§5 se declara "contrato" mas não lista um único parâmetro, corpo ou código de status concreto. Para FR-2, isso deixa aberto: `?origem=São Paulo&destino=Curitiba&data=2026-08-22` (texto), `?rotaId=3&data=...` (id), ou `?origemId=1&destinoId=2`. FR-1 insinua combobox ("popular os campos"), o que a spec **não** pede ("Formulário com: Origem, Destino, Data de ida"). Os três parâmetros são obrigatórios ou opcionais? Se obrigatórios, não existe "listar todas as viagens"; se opcionais, o comportamento default é indefinido.

**Por que importa (build errado concreto):** backend implementa busca por `rotaId`, frontend implementa campos de texto livre — integração quebra no dia da montagem, que é o pior dia possível numa entrega de uma semana.

**Correção:** §5 ganha uma coluna de contrato de entrada por endpoint (query params com tipo e obrigatoriedade, corpo de POST, códigos de status por caso). Decidir e escrever: origem/destino como *strings de cidade* (mais fiel à spec) com os três parâmetros obrigatórios, e FR-1 alimentando um `datalist`/select — ou o inverso, explicitamente.

### A7. Nenhum estado de erro de API no frontend — o implementador vai confundir falha com "sem resultados"

**Local:** FR-2, 3ª consequência; ausência em FR-3, FR-4, FR-7, FR-11, FR-12.

> FR-2: "A interface distingue três estados visualmente: carregando, com resultados, e sem resultados com mensagem explícita."

Três estados, e nenhum deles é "a API falhou". O caminho de menor esforço para um implementador é `catch { setResultados([]) }` — o que exibe "nenhuma viagem encontrada" quando o backend está fora, exatamente o comportamento que faz um avaliador perder confiança na entrega. O mesmo vazio existe para POST /reservas (erro 500 vs recusa de negócio) e para a consulta de reserva.

**Correção:** quarto estado obrigatório em FR-2 ("erro de comunicação, distinguível de vazio, com ação de tentar novamente") e uma consequência transversal em §5 ou §6: "falha de rede/5xx nunca é apresentada como resultado vazio ou sucesso".

### A8. Pipeline de CI desapareceu entre o brief e o PRD, sem entrar em §10.2

**Local:** brief §Escopo, "Camada de diferenciação"; PRD §10.1; PRD §10.2.

> brief: "Camada de diferenciação (a ambição declarada). Swagger publicado; screenshots ou gif; tratamento explícito de concorrência; testes de integração contra banco efêmero; **arquitetura em camadas com o domínio isolado**; **pipeline de CI rodando os testes**; seed de dados..."

CI não aparece em §10.1 (em escopo) **nem** em §10.2 (fora de escopo) — simplesmente evaporou. "Arquitetura em camadas" foi rebaixada a um bullet de §6 e depois é ativamente desincentivada por SM-C3 ("arquitetura hexagonal completa para seis endpoints sinaliza julgamento pior"). Duas apostas declaradas do brief perderam status sem uma decisão registrada.

**Por que importa:** itens que não estão nem dentro nem fora do escopo são os que reaparecem como discussão no meio da implementação. E CI verde num repo público é um dos sinais mais baratos e mais visíveis para o avaliador — cortá-lo pode ser certo, mas cortá-lo por omissão não é.

**Correção:** decidir CI explicitamente — DR-8 (P1: workflow que roda os testes de backend e frontend, badge no README) ou linha em §10.2 com razão. Reconciliar §6 "Domínio isolado" com SM-C3 definindo a fronteira aceita (ex.: "3 projetos: Domain, Infrastructure, Api — sem mediador, sem CQRS, sem repositório genérico").

### A9. O risco real de privacidade não é o que §7 trata

**Local:** §7, três bullets; FR-11; FR-12; §5 ("Nenhum endpoint exige autenticação").

§7 se declara proporcional e escolhe três guardrails: sem dado real no repo, não vazar existência de Reserva (que é o C1), e não logar PII. Fica de fora o que realmente existe nesta superfície:

- `GET /reservas/{codigo}` retorna **nome, CPF completo, e-mail e data de nascimento** a qualquer requisição não autenticada que apresente um código;
- `DELETE /reservas/{codigo}` **cancela a passagem de outra pessoa** sem nenhuma prova de titularidade;
- não há rate limiting, nem qualquer menção a enumeração de códigos, num espaço de códigos que o PRD escolheu deliberadamente ser curto e legível.

O PRD nomeia LGPD, cita "CPF é dado sensível de identificação", e então protege a informação menos sensível do endpoint (o status) e deixa o CPF aberto.

**Por que importa:** um avaliador que abra o Swagger enxerga isso em cinco segundos. Reconhecer e declarar (postura que o PRD adota corretamente em outros pontos) vale pontos; não mencionar parece não ter visto.

**Correção:** substituir o bullet 2 de §7 por: "a superfície aceita, por design de MVP sem contas, que o Código de Reserva é o único fator de autorização — consequência declarada no README (DR-2 e DR-6): CPF mascarado na resposta de FR-11 (`***.***.789-00`), rate limiting nomeado em DR-6 como primeira evolução de segurança".

### A10. O que vai para o repositório público é uma decisão não tomada

**Local:** DR-7, 1ª consequência; DR-4; ausência em §10.

> DR-7: "O repositório é **público** no GitHub ou GitLab, e acessível sem convite."

O diretório do projeto já contém `_bmad/`, `_bmad-output/` (brief, addendum, PRD, `.memlog.md`, esta revisão) e `.claude/`. Nenhum requisito decide se esses artefatos são versionados e publicados. As duas escolhas têm consequência forte e oposta: publicar mostra processo de planejamento (e também que ele foi assistido por IA, e ainda expõe uma revisão adversarial listando as fraquezas da própria entrega); não publicar exige `.gitignore` deliberado e um README que se sustente sozinho.

**Correção:** DR-7 ganha consequência: "define-se explicitamente o conteúdo publicado; `.claude/`, `_bmad/` e `_bmad-output/` são [incluídos|excluídos via `.gitignore`], decisão registrada no README". Tratar como decisão de entrega, com dono, antes do primeiro commit.

### A11. O caminho "sem Docker" é obrigatório no README e não tem requisito nem métrica

**Local:** DR-2 ("como rodar com e sem Docker"); DR-1; SM-1; SM-9; spec §5.

A spec exige documentar "Como rodar o projeto localmente (**com e sem Docker**)". DR-2 repete a frase e para aí. Não há requisito sobre como o banco existe sem Docker (instalar PostgreSQL local? LocalDB? connection string alternativa?), nem sobre servidor de dev do frontend — e DR-1 chega a proibir servidor de desenvolvimento *no container*, o que confunde. SM-1 e SM-9 só verificam o caminho Docker. Resultado previsível: uma seção de README escrita de memória, nunca executada, com comandos errados — o pior dos mundos, porque o avaliador que tentar esse caminho encontra a falha.

**Correção:** consequência em DR-2: "as instruções sem Docker são executadas ao menos uma vez em máquina limpa antes da entrega; se não forem, o README declara que só o caminho Docker foi validado". Definir a dependência mínima (ex.: `dotnet run` + PostgreSQL via container isolado ou connection string configurável; `npm run dev` no frontend).

### A12. O campo "assentos disponíveis" da entidade Viagem foi silenciosamente substituído por um modelo de 44 Assentos — e não se diz se Assento é persistido

**Local:** §3 Glossário (Viagem, Assento, Layout do Ônibus); spec §2 tabela de entidades; FR-3; FR-14.

> spec: "**Viagem** | Rota associada, data/hora de partida, preço base, **assentos disponíveis**"
> PRD §3: "**Viagem** — ...referencia uma Rota, tem data/hora de partida e preço base."

O PRD elimina o campo da entidade e o substitui por um Layout global fixo de 44. Isso é uma reinterpretação com consequências: (a) capacidade por Viagem deixa de ser representável, (b) "assentos disponíveis" passa a ser derivado, (c) e o PRD **nunca diz se Assento é uma entidade persistida** (44 linhas por Viagem, semeadas) ou uma projeção calculada de 1..44 menos Reservas Confirmadas. As duas implementações mudam a migration, o seed, o índice único de FR-9 e a resposta de FR-3.

Ao contrário de outras divergências (data de nascimento, fronteira de 2h), esta **não** está no índice de suposições como divergência da spec nem em DR-2 — o §13 item 7 só registra "Layout fixo: 44 Assentos", não a remoção do campo da entidade.

**Correção:** §3 declara: "Assento não é entidade persistida; o estado é derivado de (1..44) menos Reservas Confirmadas da Viagem" (ou o oposto, explicitamente). Registrar em §13 e DR-2: "o campo `assentos disponíveis` da entidade Viagem é modelado como valor derivado, não persistido — divergência deliberada da tabela de entidades".

### A13. "Passageiro identificado por CPF" não é um requisito — é uma armadilha de modelagem

**Local:** §3 Glossário (Passageiro); FR-7; §4.2 "Fora de escopo".

> §3: "**Passageiro** — Pessoa que viaja: nome, CPF, e-mail, data de nascimento. **Identificado por CPF**; não tem conta nem credencial."

"Identificado por" lê como chave. Nenhum FR define o que acontece na segunda Reserva com o mesmo CPF: reusa o Passageiro existente? Cria outro registro? E se o nome/e-mail vierem diferentes — atualiza (sobrescrevendo dado de outra compra) ou rejeita? Como o PRD exclui múltiplos Assentos por Reserva (§4.2), comprar duas passagens é *necessariamente* duas Reservas com o mesmo CPF — o caso comum, não a borda. Um índice único em CPF quebra isso ou produz um upsert silencioso.

**Correção:** trocar por "Passageiro é dado da Reserva, não entidade compartilhada: cada Reserva armazena seus próprios nome/CPF/e-mail/data de nascimento; CPF não é único" — ou definir dedup explícito com regra de conflito. Uma linha resolve; a ausência dela gera um bug de dados.

---

## MÉDIO

### M1. §0 e §13 contam errado os próprios itens do documento

**Local:** §0; §13 item 11; §12 Questão Aberta 6.

> §0: "Os requisitos de entrega (§8) usam numeração própria (`DR-1`..`DR-6`)"

§8 tem **DR-1 a DR-7**. E §12/§13 falam de "os oito critérios de §11" quando §11 tem nove métricas (SM-1..SM-9) mais três contra-métricas — o "oito" é herança dos oito critérios do brief, nunca reconciliada. Num documento cuja tese é rastreabilidade, contar errado o próprio conteúdo é o defeito mais barato de corrigir e o mais fácil de notar.

**Correção:** `DR-1..DR-7`; substituir "os oito critérios" por "as nove métricas de §11" nos dois lugares.

### M2. SM-6 estabelece uma barra que passa com dois commits

**Local:** SM-6; DR-4; spec §6.2.

> SM-6: "Histórico de git mostra construção incremental. **Mais de um commit**, mensagens legíveis, progressão reconhecível."

A spec avisa que "o histórico de git também será analisado", e o PRD converte isso numa métrica que um `git commit` inicial mais um final satisfaz literalmente. "Progressão reconhecível" não tem critério.

**Correção:** métrica verificável: "≥ 12 commits, distribuídos em ≥ 4 dias distintos, cada um compilando, mensagens no imperativo referenciando FR/DR; nenhum commit com mais de ~400 linhas de mudança fora de migrations/lockfiles".

### M3. SM-8 é infalsificável

**Local:** SM-8.

> SM-8: "Todo corte está declarado pelo próprio candidato. **Nenhuma omissão que o avaliador descubra sozinho.** Valida DR-6."

Não é possível verificar a ausência de uma omissão desconhecida antes de o avaliador ler. Como métrica, é uma aspiração; passa por default.

**Correção:** tornar operacional: "checklist final que percorre item a item spec §3.2, §3.3, §3.4, §4.2, §4.3, §5, §6, marcando implementado / parcial / não implementado, e a seção 'o que ficou de fora' do README é gerada dessa tabela".

### M4. SM-2 e SM-7 afirmam validar FRs que suas evidências não alcançam

**Local:** SM-2; SM-7.

> SM-2: "Fluxo completo percorrível fim a fim contra a API real. Buscar → selecionar Assento → informar dados → receber Código → consultar → cancelar, sem mock. **Valida FR-1 a FR-13.**"

O caminho felizmente percorrido não produz evidência de FR-10 (Viagem Realizada nem aparece na busca, por FR-2), nem da recusa de FR-13 (o fluxo cancela com sucesso), nem da concorrência de FR-9, nem da rejeição servidor-side de FR-6. "FR-1 a FR-13" é uma generalização preguiçosa que faz o mapa de cobertura mentir.

> SM-7: "Os três testes de frontend exigidos passam... **Valida FR-2, FR-4, FR-5, FR-6.**"

Esses testes usam mock de API por recomendação explícita da spec — não validam FR-2 (busca no backend) nem a metade servidor de FR-6. No máximo validam a camada de apresentação desses FRs.

**Correção:** SM-2 → "valida FR-1, FR-3, FR-4, FR-5, FR-7, FR-8, FR-11, FR-12 no caminho felizmente percorrido"; SM-7 → "valida o comportamento de UI de FR-2/FR-4/FR-5 e a validação cliente de FR-6". Adicionar SM explícito para os caminhos de recusa (FR-6 servidor, FR-9, FR-10, FR-13) verificados por testes de backend.

### M5. O Glossário proíbe "Passagem" e o próprio PRD usa a palavra três vezes

**Local:** §3 (Reserva); §1; §2.1.

> §3: "*Passagem* fica proibido como sinônimo." / "Introduzir sinônimo é violação de disciplina."
> §1: "permite que uma pessoa compre uma **passagem** de ônibus interestadual"
> §2.1: "conseguir provar que tenho a **passagem**, e desistir dela"

Além disso, a Tela 1 da spec se chama "Busca de **Passagens**" e o produto é "Sistema de Venda de **Passagens** Rodoviárias" — a palavra é obrigatória na interface e no domínio de negócio. A regra é impossível de cumprir e já está descumprida no mesmo documento, o que ensina ao leitor downstream que as regras do Glossário são retóricas.

**Correção:** "*Reserva* é o termo canônico **no modelo de dados e no código**; *passagem* permanece o termo de interface voltado ao usuário. Não são sinônimos livres: Reserva é o registro, passagem é como o Passageiro chama o que comprou."

### M6. FR-14 se chama "Semear Rotas e Viagens" mas exige Reservas e Passageiros

**Local:** FR-14 (título e 2ª consequência); §7 bullet 1.

> FR-14: "O conjunto semeado inclui pelo menos uma Viagem com Assentos parcialmente Ocupados"

Assento Ocupado só existe via Reserva Confirmada, que exige Passageiro. §7 já pressupõe isso ("o catálogo semeado usa Passageiros fictícios com CPFs sintéticos válidos"), mas FR-14 nunca diz que semeia Reservas — e o título ativamente sugere que não.

**Correção:** renomear para "Semear catálogo demonstrável" e listar as quatro classes de dado semeado: Rotas, Viagens (nas quatro classes temporais/ocupacionais — futura, <2h, realizada, esgotada), Passageiros fictícios, Reservas Confirmadas. Exigir que os CPFs sintéticos passem por FR-6 (dígito verificador válido e não sequência repetida).

### M7. Configuração e segredos: DR-1 exige "sem passo manual" e ninguém decide como

**Local:** DR-1; SM-9; §12.

"Nenhum passo manual não documentado" + "clonar em diretório novo e subir" implica credenciais de banco versionadas no `docker-compose.yml` ou num `.env` commitado. Isso é aceitável num desafio, mas é uma decisão com trade-off (segredo no repo público) que o PRD não registra, e é a causa mais comum de SM-9 falhar (`.env` no `.gitignore`, ninguém percebe).

**Correção:** consequência em DR-1: "toda configuração necessária para subir está versionada, com credenciais de desenvolvimento explicitamente marcadas como não-produtivas; nenhum `.env` fora do controle de versão é necessário — e o README declara a escolha".

### M8. FR-12 idempotente vs FR-13 "Viagem já partiu" — precedência indefinida

**Local:** FR-12, 2ª consequência; FR-13, 4ª consequência.

> FR-12: "Cancelar Reserva já Cancelada não gera efeito nem novo erro de estado — a operação é idempotente."
> FR-13: "Cancelamento de Reserva cuja Viagem já partiu é recusado."

Reserva já Cancelada cuja Viagem já partiu: sucesso idempotente ou recusa por janela? Também não há definição de código de status para o caso idempotente (200? 204? 409?), embora §5 exija distinguir recusa de negócio de validação e de não-encontrado por status HTTP.

**Correção:** ordem de avaliação explícita em FR-12/FR-13 ("existência → status já Cancelada (sucesso idempotente, 204) → Janela de Cancelamento (recusa 409/422)") e códigos de status por caso na tabela de §5.

### M9. Minimização de dados: o produto coleta dois campos sem nenhum consumidor e chama isso de postura proporcional

**Local:** §7; FR-7; §9 ("Não é um canal de comunicação"); addendum §1.2.

O e-mail é coletado (exigido pela spec) e §9 proíbe qualquer envio de e-mail — nenhum FR o consome. A data de nascimento é coletada por decisão do PRD, e o addendum §1.2 rejeitou explicitamente a opção "coletar e dar propósito". Ou seja: dois campos de dado pessoal armazenados sem uso, num documento que abre a seção de privacidade citando LGPD e "proporcional a um MVP".

**Correção:** uma linha em §7 reconhecendo a tensão: "e-mail e data de nascimento são coletados por exigência da especificação/entidade e não têm consumidor no MVP — minimização de dados é reconhecida como não atendida e declarada no README (DR-2)". Isso converte uma inconsistência em um sinal de julgamento.

### M10. Consequências de UI infalsificáveis

**Local:** FR-2 3ª consequência; FR-4 2ª consequência; FR-8 4ª consequência.

> FR-8: "A tela de sucesso exibe o Código de Reserva de forma **destacada e copiável**"
> FR-4: "distinguíveis **sem depender de cor isoladamente** (forma, borda ou rótulo)"
> FR-2: "A interface **distingue três estados visualmente**"

"Destacada", "visualmente", "distinguível" não têm mecanismo de verificação. Passam com qualquer implementação; falham com nenhuma. "Copiável" é o único que pode ser testado, se um botão de copiar for requisito — e não é dito que é.

**Correção:** transformar em asserções: "botão 'copiar código' presente e funcional (testado)"; "cada Assento expõe `aria-label` e `aria-disabled` refletindo o estado, e Ocupado tem marcador não-cromático (ex.: hachura ou ícone) — verificável no teste do Mapa de Assentos"; "cada estado da busca é identificável por um elemento próprio (`role=status` de carregando, lista de resultados, mensagem de vazio) e o teste de busca cobre os três".

### M11. FR-3 permite detalhar Viagem Realizada; FR-2 a esconde — estado alcançável só por URL, sem comportamento definido

**Local:** FR-3; FR-2 4ª consequência.

Deep link / refresh em `/viagens/{id}` de uma Viagem Realizada (que o seed *tem*, por FR-14) leva a uma Tela 2 funcional onde o Passageiro seleciona assento, preenche dados, confirma — e só então recebe a recusa de FR-10. Comportamento tecnicamente correto, experiência ruim, e nenhum FR o previu.

**Correção:** consequência em FR-3/FR-4: "detalhe de Viagem Realizada é retornado, mas a interface bloqueia a seleção e exibe 'viagem já realizada'" — o que, de bônus, torna FR-10 *parcialmente* demonstrável pela UI (mitigando C3).

### M12. A suposição §13/9 (DR-4) já está desatualizada

**Local:** §13 item 9; DR-4.

> "O diretório do projeto **ainda não é um repositório git** — `git init` é a primeira tarefa da implementação."

O diretório **já** tem `.git/` inicializado (branch `main`, zero commits). A suposição está meio-vencida, e a implicação prática mudou: a primeira tarefa não é `git init`, é decidir `.gitignore` (ver A10) e fazer o primeiro commit — de preferência já contendo os artefatos de planejamento, se a decisão for publicá-los, para que o histórico mostre planejamento antes de código.

**Correção:** atualizar o item: "repositório inicializado, zero commits; a primeira tarefa é definir `.gitignore` + primeiro commit".

---

## BAIXO

### B1. Afirmação técnica frágil sobre índice filtrado guiando a escolha de banco

**Local:** addendum §2.1, bullet 1.

> "Suporte a índice filtrado difere entre PostgreSQL e SQL Server; é insumo para a escolha do banco."

PostgreSQL tem índices parciais e SQL Server tem índices filtrados desde 2008; ambos suportam o caso de `(ViagemId, NumeroAssento) WHERE Status = Confirmada`, e o EF Core expõe `HasFilter` para os dois. Usar uma diferença inexistente como critério de decisão de arquitetura é o tipo de imprecisão que um avaliador atento percebe se ela chegar ao README.

**Correção:** substituir pelo critério real (imagem Docker, licenciamento, familiaridade) e citar `HasFilter` como suportado em ambos.

### B2. Autoelogio e retórica apresentados como insight

**Local:** §1; §3 preâmbulo; §4.2 e §4.3 descrições; FR-6 3ª consequência; §8 preâmbulo; addendum §4.

Amostra:

> §3: "Introduzir sinônimo é violação de disciplina."
> §4.3: "onde a entrega é ganha ou perdida"
> §4.2: "o coração da experiência e o único componente com decisão visual real"
> FR-6: "Este é o caso de borda que separa uma validação copiada de uma validação compreendida."
> §8: "eles são exatamente onde entregas de desafio se perdem"
> addendum §4: "Seção §8... **inventada** fora do Adapt-In Menu"

Nada disso é falso; nada disso é informação para quem vai construir. O caso mais custoso é FR-6: a observação sobre sequências repetidas é correta e útil, e vem embalada num juízo sobre o candidato que não pertence a um requisito. O documento gasta parte da sua credibilidade explicando o quanto suas próprias escolhas são boas — espaço que faltou para o contrato de entrada de `GET /viagens` (A6) e para a stack obrigatória (C2).

**Correção:** cortar os epítetos, manter os fatos. "Sequências de dígitos repetidos passam no algoritmo de dígito verificador e precisam de bloqueio explícito" é a frase inteira que interessa.

### B3. §5 mapeia um FR de frontend para uma linha de endpoint

**Local:** §5, linha `GET /viagens/{id}` → "FR-3, FR-4".

FR-4 é "Visualizar o Mapa de Assentos" — comportamento de UI, não capacidade de endpoint. Erro de categoria numa tabela que se declara contrato de API.

**Correção:** manter só FR-3 na linha e anotar consumidores de UI em coluna separada, se útil.

### B4. Jargão interno de ferramenta num documento destinado a repositório público

**Local:** §4.4 e §10.2 (`[NOTE FOR PM]`); §12 e §13 ("Levar para `bmad-architecture`"); addendum §4 ("Adapt-In Menu"); §0 (".memlog.md").

Se estes arquivos forem publicados (A10), o avaliador lê marcações de processo de uma ferramenta que ele não conhece, e referências a arquivos que talvez não existam no repo.

**Correção:** resolver junto com A10; se publicar, limpar as marcações ou explicá-las em uma linha de README.

### B5. §13 mistura estados no mesmo lugar sem máquina de estados

**Local:** §13, itens 4 e 6 (tachados), 5 e 7 ("Decidido por você").

O cabeçalho diz "Cada item precisa de confirmação explícita", mas quatro dos onze itens já foram resolvidos, sinalizados por três convenções diferentes (tachado + "Confirmado", parêntese "Decidido por você", texto corrido). Um leitor rápido não distingue o que ainda bloqueia.

**Correção:** coluna `Status: Aberto | Confirmado | Resolvido-como-requisito` e mover os resolvidos para uma sublista fechada.

### B6. Formato de armazenamento e de resposta do CPF indefinido

**Local:** FR-6 4ª consequência; FR-11 1ª consequência.

> FR-6: "CPF válido é aceito com ou sem máscara de pontuação."

Aceita as duas formas na entrada; não diz o que persiste (11 dígitos? com máscara?) nem como retorna em FR-11. Afeta comparação, eventual unicidade (A13) e o mascaramento sugerido em A9.

**Correção:** "CPF é normalizado para 11 dígitos na persistência e retornado mascarado em FR-11".

### B7. Nenhum NFR de desempenho, paginação ou forma de resposta — omissão razoável, não declarada

**Local:** §6.

Para seis endpoints e 44 assentos, ausência de requisito de performance é a decisão certa. Mas §6 se apresenta como "NFRs Transversais" e não diz que desempenho, paginação e volume foram considerados e dispensados — o que deixa o leitor sem saber se foi decisão ou esquecimento.

**Correção:** um bullet: "Desempenho, paginação e limites de volume estão fora dos NFRs: o catálogo semeado é de dezenas de registros e nenhuma resposta cresce sem limite. Declarado, não esquecido."

---

## Resumo por área de ataque

| Ângulo | Achado dominante |
|---|---|
| **Cobertura de requisitos** | Stack obrigatória ausente do PRD (C2); cinco regras contadas como quatro, FR-10 sem verificação (C3); validação de formulário além do CPF ausente (A5); campo `assentos disponíveis` reinterpretado sem registro (A12); caminho "sem Docker" sem requisito (A11) |
| **Contradições internas** | §7 vs FR-11/FR-12/UJ-2 (C1); §6 "sem infraestrutura" vs FR-9 "garantia do banco" (A2); princípio de FR-2 não aplicado a Viagem esgotada (A1); Glossário proíbe palavra que o próprio PRD usa (M5); DR-1..DR-6 vs DR-7 e "oito critérios" vs nove (M1) |
| **Consequências não testáveis** | FR-8 unicidade probabilística e retry intestável (A3); FR-13 sem relógio injetável (C5); "destacada", "visualmente", "sem depender de cor" (M10); SM-8 infalsificável (M3); SM-6 passa com dois commits (M2) |
| **Viabilidade em 1 semana** | Escopo máximo sem ordem de corte, mecanismo do brief descartado (C6); TestContainers implícito e obrigatório (A2); FR-9 esconde escopo grande de frontend (A4) |
| **Autoengano** | Retórica como insight (B2); "escopo fechado de propósito" sobre plano expansivo (C6); privacidade que protege o campo irrelevante e ignora o CPF aberto (A9); dois campos de PII sem consumidor chamados de proporcionais (M9) |
| **Segurança downstream** | Contrato de `GET /viagens` inexistente → backend e frontend divergem (A6); erro de API confundido com resultado vazio (A7); datas de seed fixas matam a demo (C4); "identificado por CPF" → índice único errado (A13); conteúdo do repo público não decidido (A10) |
