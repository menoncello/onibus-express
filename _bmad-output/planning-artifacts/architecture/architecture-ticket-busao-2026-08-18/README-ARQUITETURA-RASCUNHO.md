---
title: 'Rascunho da seção de arquitetura do README — OniBus Express'
type: rendering
audience: avaliador técnico
serves: DR-2
created: '2026-08-18'
updated: '2026-08-18'
---

# Rascunho — seção de arquitetura do README

Este arquivo é **material para o README da entrega**, não parte da espinha. A espinha registra a decisão; aqui vive o *porquê*, que é o que DR-2 exige e o que o avaliador técnico lê. Recorte, ajuste o tom e cole no `README.md` quando o repositório existir. As três primeiras seções cobrem "tecnologias e bibliotecas usadas e por quê"; as demais cobrem "decisões de arquitetura relevantes" e "o que ficou de fora".

---

## Escolhas de stack e por quê

A especificação impôs .NET, EF Core, React com TypeScript, React Testing Library e Docker com Nginx. Essas não foram escolhas minhas. O que estava em aberto foi decidido assim:

**`net10.0`, não `net8.0`.** A especificação pede ".NET 8+". .NET 8 e .NET 9 chegam ao fim do suporte em **10 de novembro de 2026** — daqui a menos de três meses. .NET 10 é o LTS atual, suportado até novembro de 2028, e satisfaz o "+". Entregar sobre um runtime que expira antes do fim de um processo seletivo me pareceu a leitura errada de um requisito de versão mínima.

**PostgreSQL, não SQL Server.** Três razões, em ordem de peso: o índice único parcial que sustenta a regra de assento ocupado (abaixo) tem sintaxe direta e estável no PostgreSQL; a imagem `alpine` é substancialmente menor, o que importa quando o critério é subir do zero em máquina limpa com um comando; e o licenciamento não pede nenhuma ressalva num repositório público. Não encontrei nada no desafio que puxasse para o SQL Server.

**xUnit, não NUnit.** É o que o próprio tooling do .NET assume por padrão. Fiquei na linha `xunit.v3 3.1.0` em vez da `4.0.0`, que saiu três dias antes de eu começar e troca o caminho padrão de runner para o Microsoft Testing Platform v2 — numa janela de uma semana, isso é risco sem retorno.

**Zustand, não Context API nem Redux.** Redux seria excesso para este fluxo. Entre Context e Zustand a decisão foi por um motivo concreto, não por preferência: a store guarda o **rascunho do formulário do passageiro**, e é isso que faz os dados digitados sobreviverem à recusa por assento ocupado — o caso de borda mais frustrante do fluxo, em que o mapa de assentos precisa recarregar sem o usuário perder o que já preencheu. Com Context o mesmo é possível, mas re-renderiza toda a árvore consumidora.

**Vitest, não Jest.** O projeto é Vite, e o `vite.config.ts` já é a configuração do Vitest. Jest exigiria uma segunda cadeia de transformação e um segundo mapa de aliases, mantidos em paralelo e divergindo em silêncio. React Testing Library, que a especificação exige, funciona igual nos dois.

**Testcontainers, e nenhum SQLite.** A dica do desafio oferece "SQLite in-memory ou TestContainers". Escolhi Testcontainers e **não usei SQLite em nenhum teste**, nem nos que ele suportaria. O SQLite não reproduz o comportamento de concorrência nem o índice único parcial, então justamente o teste de maior peso não roda nele; e manter SQLite para os demais criaria uma segunda história de persistência — outro dialeto, outras migrações — que passa verde enquanto o PostgreSQL de produção falha. Uma suíte verde com a regra de concorrência sem garantia real seria o pior resultado possível.

**Swagger.** Aqui houve uma mudança de plataforma que vale registrar: o ASP.NET Core **removeu o Swashbuckle dos templates a partir do .NET 9**. Em .NET 10, o pacote nativo `Microsoft.AspNetCore.OpenApi` gera o documento OpenAPI 3.1 mas não entrega interface. Usei o pacote nativo para o documento e o Scalar para a UI, em vez de reintroduzir o Swashbuckle — escolher o caminho descontinuado sem necessidade seria o oposto do sinal que eu quero dar. Mantive um redirect de `/swagger` para a interface, porque é o endereço que a maioria das pessoas digita.

---

## As decisões de arquitetura que importam

### Três projetos, não quatro camadas

`OniBus.Domain` (puro) → `OniBus.Api` (fatias, EF Core, adaptadores), mais dois projetos de teste. **Não existe projeto `Infrastructure` e não existe camada `Application`.**

Essa foi a decisão mais deliberada da entrega, e a mais fácil de errar na direção oposta. Arquitetura hexagonal completa para seis endpoints acrescenta três saltos de indireção que nenhum requisito exerce, e transforma leitura de código em trabalho. Ao mesmo tempo, um projeto único não permitiria eu afirmar que as regras estão isoladas.

Três é a menor estrutura em que o isolamento do domínio deixa de ser promessa e passa a ser **propriedade mecânica**: `OniBus.Domain.csproj` não tem nenhuma `PackageReference` nem `ProjectReference`. Se uma regra de negócio precisasse de EF Core, o arquivo não compilaria. A fronteira é imposta pelo csproj, não pela minha disciplina.

Pelo mesmo critério, existem exatamente **duas** portas invertidas — `IRelogio` e `IGeradorCodigoReserva` — e elas existem porque três regras exigem determinismo em teste. Não abstraí nada além disso.

### A regra de assento ocupado é do banco, não do código

Este é o item de maior peso do desafio e onde uma implementação plausível falha.

Verificar disponibilidade em memória e depois inserir passa em teste sequencial e vende o mesmo assento duas vezes sob requisições simultâneas. A garantia aqui é um **índice único parcial**:

```sql
CREATE UNIQUE INDEX ux_reservas_viagem_assento_confirmada
  ON reservas (viagem_id, numero_assento)
  WHERE status = 'Confirmada';
```

O predicado `WHERE` é o que faz esse índice satisfazer duas regras ao mesmo tempo: a linha cancelada fica **fora** do índice, então o assento volta a ser reservável depois de um cancelamento sem uma linha de código adicional. Considerei bloqueio pessimista na linha da viagem (serializa todas as reservas de uma viagem, e devolve a garantia ao código) e concorrência otimista por token de versão (produz falso conflito entre assentos diferentes). Nenhuma das duas resolve o cancelamento de graça.

Duas consequências que não são óbvias e estão implementadas:

1. **O status é persistido como texto**, não como inteiro. Se fosse inteiro, o filtro do índice seria `WHERE status = 0` — ilegível e quebrável por qualquer reordenação do enum.
2. **A violação é discriminada por nome de constraint.** O PostgreSQL levanta `SqlState 23505` para os *dois* índices únicos da tabela, e eles pedem tratamento oposto: colisão de código de reserva pede retry silencioso, colisão de assento pede recusa visível ao usuário. Capturar `23505` genericamente transformaria um dos dois no erro errado.

### Assentos e disponibilidade não são dados

A tabela de entidades do desafio lista "assentos disponíveis" na viagem. Modelei como **valor derivado**, e não como coluna.

Um contador persistido seria uma segunda fonte de verdade sobre a mesma informação, dessincronizável exatamente sob a concorrência que a regra de assento ocupado exige tratar. Pela mesma razão não existe tabela de assentos: os 44 vêm de uma constante de layout no domínio, e livre/ocupado é derivado das reservas confirmadas. Não há nada para dessincronizar.

Pelo mesmo princípio, o passageiro não é tabela — é valor gravado inline na reserva. Sem contas e sem histórico, os dados do passageiro são um *snapshot* do momento da compra, não uma entidade com ciclo de vida próprio. Isso também evita a armadilha de tratar o CPF como chave única: a mesma pessoa pode ter várias reservas, inclusive na mesma viagem.

### Unicidade do código de reserva é provada, não sorteada

O formato `AAA-00000` tem cerca de 1,76 bilhão de combinações. Um teste que gera N códigos e verifica que não repetiram **passa por acaso** — ele não prova nada.

Por isso o gerador é injetável: o teste injeta um gerador que devolve o mesmo código duas vezes e verifica que saem duas reservas com códigos diferentes, provando que o tratamento de colisão existe e funciona. Em produção o gerador usa `RandomNumberGenerator`, não `Random`: códigos previsíveis ou derivados do id abririam enumeração, e o código é a única credencial de acesso à reserva.

### Tempo: instantes em UTC, dia de calendário no fuso do negócio

A pergunta "qual referência de tempo" tem duas respostas diferentes, e tratá-la como uma só produz dois bugs distintos.

**Instantes** são UTC de ponta a ponta (`timestamptz`, `DateTimeOffset`, `IRelogio.Agora`). As regras de viagem já realizada e de janela de cancelamento comparam instante contra instante, sem fuso no meio. Isso é o que torna o teste da fronteira de 2 horas determinístico nos três pontos — 2h01, 2h00 e 1h59 — em vez de intermitente.

**Dia de calendário** é outra coisa. A busca recebe um dia, não um instante, e "sábado" depende de onde a pessoa está. Fixei um único fuso de negócio, `America/Sao_Paulo`, usado só para traduzir a data da busca num intervalo meio-aberto em UTC, calculado em C# antes da consulta. Sem isso, a viagem das 22h de sábado apareceria na busca de domingo.

O frontend não faz aritmética de fuso: recebe instantes com offset e renderiza no fuso do navegador.

### Erros: `ProblemDetails` com código de máquina

Todo erro é RFC 9457 `ProblemDetails` com um membro `codigo` de vocabulário fechado (`ASSENTO_OCUPADO`, `VIAGEM_REALIZADA`, `JANELA_CANCELAMENTO_FECHADA`, …).

O motivo é concreto: o frontend precisa reagir de formas diferentes a "seu CPF está inválido" e a "alguém pegou esse assento agora", e um cliente que faz *string matching* em mensagem de erro é frágil por construção. `409` é deliberadamente distinto de `400` — entrada inválida é sobre o que foi digitado, recusa por regra é sobre o estado do mundo ter mudado.

### O frontend nunca conhece a URL da API

Todo `fetch` usa caminho relativo `/api/*`. No Docker, o Nginx faz reverse proxy; sem Docker, o proxy de dev do Vite faz o mesmo.

Isso resolve três coisas de uma vez. Elimina a armadilha clássica do Vite, em que `VITE_API_URL` é assado no bundle em tempo de *build* e a imagem fica amarrada a um host — o que quebraria a subida do projeto na máquina de outra pessoa. Elimina CORS por completo, porque nunca existe uma segunda origem. E faz o código do frontend ser idêntico nos dois caminhos de execução que o README documenta, em vez de dois caminhos que divergem.

### Migração e seed no boot, com healthcheck

Um comando tinha que bastar, então migração e seed rodam no boot da API, pelo mesmo caminho de código nos dois ambientes. O `depends_on` usa `condition: service_healthy` sobre o `pg_isready` do banco: sem isso, `depends_on` garante ordem de início mas não prontidão, e a API sobe antes do Postgres aceitar conexão — a falha intermitente mais comum desse tipo de setup.

Duas coisas no seed que parecem detalhe e não são:

- **As datas são relativas ao instante de execução**, nunca literais de calendário. Com datas fixas, toda viagem semeada se torna uma viagem já realizada com a passagem do tempo, deixa de aparecer na busca, e quem abrir o projeto semanas depois encontra uma busca que não retorna nada. O projeto pareceria quebrado sem estar.
- **A idempotência é por chave natural**, não por `if (!db.Rotas.Any())`. A segunda forma passa no teste de subir duas vezes mas nunca reconcilia quando o conjunto semeado muda.

---

## Onde estão os testes, e por que ali

Cinco regras de negócio, cinco testes — cada um escrito para **falhar se a regra for removida**, não apenas para exercitá-la. A especificação exige quatro; acrescentei o de viagem já realizada, porque optei por esconder viagens realizadas da busca e, com isso, ela ficou sendo a única regra sem nenhuma outra forma de verificação.

Duas camadas, e a fronteira entre elas é uma decisão, não uma consequência:

| Camada | O que roda | Cobre |
| --- | --- | --- |
| `OniBus.Domain.Tests` | xUnit puro, sem banco e sem host | CPF (incluindo sequências repetidas), formato do código, viagem realizada, janela de cancelamento nas três fronteiras |
| `OniBus.Api.Tests` | `WebApplicationFactory` + PostgreSQL real via Testcontainers | concorrência de assento, colisão forçada de código, contrato HTTP |

Isolar o domínio **não** significa fingir que a regra de assento ocupado é testável em memória. A garantia dela é o índice do banco, então prová-la exige banco real. Saber qual regra vive de que lado dessa fronteira é a decisão; a estrutura de pastas é só a consequência.

No frontend, os três testes exigidos mockam o cliente HTTP tipado — uma costura só, em vez de três alturas diferentes de mock — e exercem comportamento do usuário: preencher, clicar, ver o resultado.

---

## O que ficou de fora, e por quê

Cada item abaixo foi considerado e recusado, não esquecido. A arquitetura foi desenhada para que nenhum deles exija remodelagem quando entrar.

| Fora de escopo | Por quê | O que muda para entrar |
| --- | --- | --- |
| **Múltiplos assentos por reserva** | O item mais defensável de todos — comprar duas passagens juntas é comportamento real e frequente. Ficou fora porque cada assento adicional multiplica o caso de concorrência e a semântica de cancelamento parcial | O índice único de assento continua correto; muda a agregação acima dele |
| **Layout de ônibus configurável por viagem** | Domínio mais rico e mais próximo de uma frota real, mas nenhum requisito o exerce, e o custo em migração, seed e frontend não caberia na semana | A constante de layout vira entidade e a viagem passa a referenciá-la |
| **Autenticação e contas** | Nenhum requisito do desafio menciona. O código de reserva é a única credencial | Nada reserva espaço para identidade hoje; seria adição, não refatoração |
| **Pagamento** | A reserva encerra o fluxo, por decisão de escopo | Novo status na reserva e uma etapa antes da confirmação |
| **Rate limiting na consulta por código** | O risco real do código público é enumeração, e respondi a ele com imprevisibilidade criptográfica em vez de throttling. Num MVP avaliado por inspeção, throttling seria escopo não pedido; num serviço exposto de verdade, é o primeiro item que eu adicionaria | Middleware, sem tocar em domínio |
| **Cache, paginação** | O catálogo tem 16 viagens. Introduzi-los agora seria abstração não exercida | — |
| **Deploy hospedado, observabilidade, staging** | Existem dois ambientes — Docker e local sem Docker — mais o CI. Log estruturado em stdout é o teto deliberado. Isso está declarado, não esquecido | — |
| **Conformidade formal com LGPD** | O produto coleta CPF, que é dado sensível. Reconhecer o limite e declará-lo me parece a postura correta num MVP de desafio; implementar base legal, retenção e direito de eliminação seria excesso. O que existe: nenhum dado pessoal real no repositório, CPFs sintéticos no seed, e dado pessoal nunca em log | Programa de privacidade, não código |

## Divergências deliberadas da especificação

Onde a especificação era ambígua ou silenciosa, escolhi — e a escolha está aqui em vez de escondida no código:

- **Data de nascimento é coletada.** A tabela de entidades a inclui; a lista de campos do formulário a omite. Honrei a entidade.
- **A janela de cancelamento é exclusiva:** exatamente 2h00min antes da partida já recusa. A especificação diz "até 2 horas antes" e não define a fronteira; adotei a leitura conservadora, e o teste cobre os três pontos.
- **Viagem já realizada não aparece na busca.** Oferecer o que não pode ser reservado é ruído. Consequência aceita: essa regra deixa de ser demonstrável pela interface e só é exercitável via API — o catálogo semeado inclui uma viagem realizada exatamente para isso.
- **Viagem esgotada continua aparecendo**, marcada como indisponível. O princípio é o mesmo, mas "esgotada" é reversível por um cancelamento e "realizada" não.
- **Layout fixo em 44 assentos**, 11 fileiras de 2+2 com corredor central. A especificação não define capacidade nem disposição.
- **Status da reserva tem dois valores**, confirmada e cancelada. Valores como "utilizada" ou "pendente de pagamento" pressupõem embarque ou cobrança, ambos fora de escopo.
- **Mapeamento HTTP:** `201` com corpo na criação, `204` no cancelamento, `409` para recusa por regra de negócio. A especificação é silenciosa; o requisito real era que os três tipos de recusa fossem distinguíveis sem interpretar texto.
- **A reserva cancelada é retornada normalmente na consulta**, com o status. Quem tem o código é o titular; esconder dele o desfecho da própria reserva tornaria a tela de consulta inútil.
