---
title: OniBus Express
created: 2026-08-18
updated: 2026-08-18
status: final
---

# PRD: OniBus Express

## 0. Propósito do Documento

Este PRD é o contrato de **o quê** para o OniBus Express — MVP de venda de passagens rodoviárias online, entregue como desafio técnico avaliado. Ele existe para que arquitetura, épicos e implementação partam de um vocabulário único e de requisitos com consequências testáveis, em vez de reinterpretarem a especificação original a cada etapa.

**Estrutura:** o Glossário (§3) fixa o vocabulário, usado literalmente no resto do documento. As Features (§4) trazem descrição comportamental com FRs numerados globalmente (`FR-1`..`FR-14`), cada um com consequências verificáveis. O contrato de API (§5) registra a superfície pública e a semântica de resposta. As restrições (§7) separam o que a especificação **impõe** — a stack — do que ela deixa livre. Os requisitos de entrega (§8) usam numeração própria (`DR-1`..`DR-8`) para não misturar obrigação de avaliação com capacidade de produto. A ordem de corte (§10.3) define o que sai primeiro se a semana apertar. Suposições aparecem inline como `[ASSUMPTION]` e estão indexadas em §13.

**Insumos a montante** — este PRD constrói sobre eles e não os duplica:

- `../../briefs/brief-ticket-busao-2026-08-18/brief.md` — posicionamento, escopo em camadas, critérios de sucesso
- `../../briefs/brief-ticket-busao-2026-08-18/addendum.md` — **especificação original íntegra** (fonte canônica de requisitos) e catálogo de ambiguidades

Onde este PRD divergir da especificação original, a divergência é deliberada, está registrada em `.memlog.md` e deve ser documentada no README da entrega.

## 1. Visão

O OniBus Express permite que uma pessoa compre uma passagem de ônibus interestadual sem falar com ninguém: busca viagens por origem, destino e data, escolhe visualmente onde quer sentar, informa seus dados e sai com um código de reserva legível — `ABC-12345` — que ela consegue consultar e cancelar depois por conta própria.

O valor não está em nenhuma dessas etapas isoladamente, e sim no fluxo inteiro fechar sem intervenção humana. Hoje a OniBus Express vende no guichê e por telefone: o passageiro não vê disponibilidade real, não escolhe o assento, não tem comprovante consultável e depende de atendimento para cancelar. Cada venda carrega custo operacional, e toda venda que aconteceria fora do horário do guichê simplesmente não acontece.

Há um segundo leitor deste documento, e ele é quem de fato decide o destino do projeto: **o avaliador técnico**. Este é um desafio de candidatura, entregue em repositório público, com histórico de git analisado. Isso não muda o que o produto faz — muda onde vale investir esforço. Um assento que não pode ser vendido duas vezes sob requisições concorrentes, quatro testes que falham se a regra for removida e um README que expõe trade-offs valem mais, aqui, do que qualquer feature adicional. O escopo é fechado de propósito; o que está em disputa é a qualidade do julgamento visível dentro dele.

## 2. Usuário-Alvo

### 2.1 Jobs To Be Done

**Passageiro** (usuário do produto):
- **Funcional:** encontrar uma viagem que sirva para a data e o trecho que preciso, e garantir um lugar nela.
- **Funcional:** escolher onde vou sentar — janela, corredor, longe do banheiro — em vez de aceitar o que sobrou.
- **Funcional:** conseguir provar que tenho a passagem, e desistir dela, sem depender de atendimento.
- **Emocional:** sair da compra com certeza de que deu certo. Um código legível que eu consigo ditar por telefone faz mais por essa certeza do que um UUID.
- **Contextual:** comprar às 23h de um domingo, quando o guichê está fechado.

**Avaliador técnico** (usuário do artefato):
- Decidir, em pouco tempo e sem ajuda, se este candidato organiza código, sabe onde testar e defende decisões.
- Subir o projeto sem descobrir um passo manual não documentado.
- Encontrar as regras de negócio isoladas e cobertas, não espalhadas em controllers.
- Ver o que ficou de fora declarado pelo próprio candidato, em vez de descobrir sozinho.

### 2.2 Não-Usuários (v1)

- **Operador da OniBus Express.** Não há painel administrativo. Rotas e Viagens entram por dados semeados; ninguém as cadastra pela interface.
- **Atendente de guichê.** Nenhum fluxo assistido, nenhuma venda em nome de terceiro.
- **Passageiro autenticado com conta.** Não há cadastro, login ou histórico. Uma Reserva é localizável apenas pelo Código de Reserva.

### 2.3 Jornadas de Usuário

> `[ASSUMPTION]` As três jornadas abaixo foram redigidas a partir das quatro telas da especificação, não narradas pelo usuário. Protagonistas, motivações e contextos são plausíveis, mas inventados — corrija o que não corresponder à sua intenção.

**UJ-1. Rafael garante o lugar da janela para a viagem de sábado.**
Rafael, 29 anos, vai visitar a mãe em outra cidade e detesta viajar no corredor. Chega ao site sem conta e sem sessão — não existe autenticação. Preenche origem, destino e a data de sábado, e busca. Vê a lista de viagens do dia com horário de partida, preço e quantas vagas restam em cada uma; escolhe a das 8h porque ainda tem 12 lugares. Cai no mapa de assentos e enxerga de uma vez o que está livre e o que está ocupado: pega o 15, na janela. Informa nome, CPF, e-mail e data de nascimento, confere o resumo — trecho, data, hora, assento 15, preço — e confirma. **Clímax:** a tela de sucesso mostra `KRT-48210`. Ele tira um print. **Resolução:** sai do site com a passagem garantida e um código que consegue ditar por telefone se precisar. **Caso de borda:** se alguém tomou o assento 15 entre ele abrir o mapa e confirmar, a reserva é recusada com mensagem explícita e o mapa recarrega com o estado atual, sem perder os dados que ele já digitou.

**UJ-2. Carla desiste na véspera e cancela sozinha, às 22h.**
Carla comprou para segunda-feira, 7h, e o compromisso caiu. É domingo, 22h — guichê fechado; antes, ela teria que esperar até a manhã seguinte. Entra na consulta de reserva, digita `KRT-48210` e vê os detalhes: trecho, data, hora, assento, status Confirmada. Como ainda faltam nove horas para a partida, o botão de cancelar está disponível. Cancela. **Clímax:** o status vira Cancelada e o assento volta a ficar livre para outra pessoa. **Resolução:** resolveu sem falar com ninguém, fora do horário comercial. **Caso de borda:** se ela tentasse o mesmo às 6h de segunda, a menos de duas horas da partida, o cancelamento seria recusado com a razão explícita — a janela fechou.

**UJ-3. Marcos digita o CPF errado e o sistema o corrige antes de gastar o assento.**
Marcos está com pressa, chega ao formulário e erra um dígito do CPF. `[ASSUMPTION]` A validação dispara no frontend, ainda no campo, antes de qualquer chamada à API: mensagem clara de CPF inválido, e o botão de confirmar segue bloqueado. Ele corrige e confirma. **Clímax:** a reserva passa, e o servidor revalida o CPF de forma independente — o frontend cuida da experiência, o backend cuida da integridade. **Resolução:** nenhum registro sujo entrou no banco, e Marcos nunca viu um erro de servidor.

## 3. Glossário

Os termos abaixo são de uso obrigatório e literal no resto deste documento e nos artefatos derivados dele. Introduzir sinônimo é violação de disciplina.

- **Rota** — Par ordenado origem→destino com duração estimada. Uma Rota tem muitas Viagens.
- **Viagem** — Ocorrência datada de uma Rota: referencia uma Rota, tem data/hora de partida e preço base. Uma Viagem tem muitos Assentos e muitas Reservas. O campo "assentos disponíveis" da tabela de entidades da especificação é modelado como **valor derivado** — contagem de Assentos Livres, calculada a partir das Reservas Confirmadas — e não como contador persistido. Um contador persistido seria uma segunda fonte de verdade sobre a mesma informação, dessincronizável sob a concorrência que FR-9 exige tratar. Divergência deliberada da leitura literal da entidade, a documentar no README (DR-2).
- **Assento** — Posição numerada dentro de uma Viagem, de 1 a 44. Está **Livre** ou **Ocupado**; *Selecionado* é estado de interface, não de dados.
- **Layout do Ônibus** — Disposição fixa de 44 Assentos em 11 fileiras de 4 (2+2, corredor central). Idêntico para toda Viagem no MVP.
- **Mapa de Assentos** — Representação visual do Layout do Ônibus de uma Viagem específica, com o estado de cada Assento.
- **Passageiro** — Pessoa que viaja: nome, CPF, e-mail, data de nascimento. Não tem conta nem credencial. O CPF **identifica a pessoa mas não é chave única de Reserva**: a mesma pessoa pode ter várias Reservas, em Viagens diferentes ou até na mesma Viagem em datas distintas. Tratar CPF como identificador único de Reserva quebraria o uso mais comum do produto.
- **Reserva** — Vínculo entre uma Viagem, um Passageiro e um Assento, com Status e Código de Reserva. **Termo canônico:** a especificação original diz "Reserva/Passagem"; este PRD usa exclusivamente *Reserva*. *Passagem* fica proibido como sinônimo.
- **Código de Reserva** — Identificador público, único e legível por humano, no formato `AAA-00000`: três letras maiúsculas, hífen, cinco dígitos. É a única forma de localizar uma Reserva.
- **Status da Reserva** — `Confirmada` ou `Cancelada`. `[ASSUMPTION]` A especificação não enumera os valores; dois bastam para o MVP, já que não há embarque nem pagamento a modelar.
- **Janela de Cancelamento** — Intervalo em que uma Reserva Confirmada ainda pode ser cancelada: de sua criação até **mais de 2 horas antes** da data/hora de partida da Viagem. A fronteira é **exclusiva** — exatamente 2h00min antes da partida a janela já está fechada.
- **Viagem Realizada** — Viagem cuja data/hora de partida já passou. Não aceita novas Reservas.
- **Viagem Esgotada** — Viagem sem nenhum Assento Livre. Diferente de Viagem Realizada: continua aparecendo na busca, com indicação explícita de indisponibilidade, porque uma Reserva Cancelada pode reabri-la a qualquer momento.
- **Relógio da Aplicação** — Fonte única de "agora" usada por FR-10 e FR-13. É uma dependência injetável, não uma chamada direta ao relógio do sistema espalhada pelo código.

## 4. Features

> **As cinco regras de negócio da especificação e onde vivem.** A especificação lista **cinco**, não quatro: validação de CPF (FR-6), código de reserva único e legível (FR-8), assento ocupado (FR-9) e viagem já realizada (FR-10) ficam em §4.3; janela de cancelamento (FR-13) fica em §4.4. Dessas cinco, a especificação exige teste automatizado para **quatro** — FR-6, FR-8, FR-9 e FR-13 —, deixando FR-10 sem teste obrigatório. Este PRD exige teste para FR-10 mesmo assim: é a regra mais barata de cobrir e, depois da decisão de omitir Viagem Realizada da busca (FR-2), a única sem nenhuma outra forma de verificação.

### 4.1 Descoberta de Viagens

**Descrição:** ponto de entrada do produto. O Passageiro chega sem sessão, informa o trecho e a data, e recebe as Viagens que servem. A busca é o único lugar onde ele pode não encontrar nada, então o estado vazio é requisito, não detalhe — assim como o estado de carregamento.

#### FR-1: Listar Rotas disponíveis

Qualquer visitante pode obter a lista completa de Rotas cadastradas, sem autenticação. Realiza UJ-1.

**Consequências (testáveis):**
- A resposta traz origem, destino e duração estimada de cada Rota.
- Serve para popular os campos de origem e destino da busca, evitando que o Passageiro digite um trecho inexistente.
- Com o catálogo semeado (FR-14), a lista nunca vem vazia.

#### FR-2: Buscar Viagens por origem, destino e data

Qualquer visitante pode buscar Viagens informando origem, destino e data de partida. Realiza UJ-1.

**Contrato de requisição:** origem, destino e data são **todos obrigatórios**. Origem e destino correspondem a valores existentes em Rotas (FR-1); a data é um dia de calendário, e a busca retorna todas as Viagens que partem naquele dia.

**Consequências (testáveis):**
- Cada Viagem retornada traz data/hora de partida, preço base e a quantidade de Assentos Livres.
- Requisição com qualquer um dos três parâmetros ausente é rejeitada como erro de validação — não é tratada como busca aberta.
- Busca sem correspondência retorna coleção vazia com sucesso — não é erro.
- A interface distingue três estados visualmente: carregando, com resultados, e sem resultados com mensagem explícita.
- **Viagem Realizada não aparece nos resultados de busca** — oferecer o que não pode ser reservado é ruído. Decisão confirmada, com uma consequência aceita: FR-10 deixa de ser demonstrável pela interface e só é exercitável via API. O catálogo semeado (FR-14) inclui uma Viagem Realizada justamente para isso, e o README (DR-2) precisa apontar o caminho.
- **Viagem Esgotada continua aparecendo**, marcada como indisponível, e não é selecionável. O princípio é o mesmo — não oferecer o irreservável —, mas "esgotada" é reversível (um cancelamento a reabre) e "realizada" não.

**Fora de escopo:** passagem de volta. A especificação pede apenas data de ida.

#### FR-3: Detalhar uma Viagem com o estado dos Assentos

Qualquer visitante pode obter os detalhes de uma Viagem, incluindo quais Assentos estão Livres e quais estão Ocupados. Realiza UJ-1.

**Consequências (testáveis):**
- A resposta identifica cada um dos 44 Assentos e seu estado.
- Traz Rota, data/hora de partida e preço base — dados que a tela de seleção exibe sem uma segunda chamada.
- Assento vinculado a Reserva Cancelada retorna como Livre.

### 4.2 Seleção de Assento

**Descrição:** o coração da experiência e o único componente com decisão visual real. O Passageiro vê o ônibus, não uma lista. Escolher onde sentar é a diferença entre este produto e comprar no guichê.

#### FR-4: Visualizar o Mapa de Assentos

O Passageiro pode ver o Mapa de Assentos da Viagem escolhida, com os Assentos Livres e Ocupados visualmente distinguíveis. Realiza UJ-1.

**Consequências (testáveis):**
- Renderiza 44 Assentos em 11 fileiras de 4, com corredor central entre a segunda e a terceira coluna.
- Os três estados de interface — Livre, Ocupado, Selecionado — são distinguíveis **sem depender de cor isoladamente** (forma, borda ou rótulo), para não excluir quem não diferencia cores.
- A tela exibe Rota, data, hora e preço da Viagem.

#### FR-5: Selecionar um Assento Livre

O Passageiro pode selecionar exatamente um Assento Livre e prosseguir com ele. Realiza UJ-1.

**Consequências (testáveis):**
- Clicar em Assento Ocupado não produz seleção e não emite erro — simplesmente não é interativo.
- Selecionar um segundo Assento substitui o primeiro; nunca há dois selecionados.
- O botão de prosseguir permanece bloqueado enquanto nenhum Assento estiver selecionado.

**Fora de escopo:** múltiplos Assentos numa mesma Reserva. Uma Reserva é um Passageiro em um Assento.

### 4.3 Reserva e Emissão

**Descrição:** onde vivem as regras que dão integridade ao domínio, e onde a entrega é ganha ou perdida. O frontend valida para dar boa experiência; o backend valida porque é a fonte de verdade — as duas existem por razões diferentes e nenhuma substitui a outra.

#### FR-6: Validar CPF por formato e dígito verificador

O sistema valida o CPF informado, no cliente e no servidor de forma independente, rejeitando CPF inválido antes de criar Reserva. Realiza UJ-3.

**Consequências (testáveis):**
- CPF com quantidade de dígitos diferente de 11 é rejeitado.
- CPF cujos dois dígitos verificadores não conferem é rejeitado.
- **Sequências de dígitos repetidos (`111.111.111-11`, `000.000.000-00` e as demais) são rejeitadas** — elas passam no algoritmo de dígito verificador e precisam de bloqueio explícito. Este é o caso de borda que separa uma validação copiada de uma validação compreendida.
- CPF válido é aceito com ou sem máscara de pontuação.
- A rejeição no cliente impede a chamada à API; a rejeição no servidor retorna erro de validação sem criar registro.

#### FR-7: Criar Reserva

O Passageiro pode criar uma Reserva informando nome, CPF, e-mail e data de nascimento para uma Viagem e um Assento Livre. Realiza UJ-1.

**Consequências (testáveis):**
- Reserva criada nasce com Status `Confirmada` e um Código de Reserva.
- O Assento passa a Ocupado e deixa de aparecer como Livre em FR-3.
- **A data de nascimento é coletada** no formulário e no payload. A tabela de entidades da especificação inclui o campo, e a lista literal de campos do formulário o omite; este PRD honra a entidade. Divergência deliberada, a documentar no README.
- Requisição com campo obrigatório ausente é rejeitada com erro de validação, sem criar Reserva.
- O resumo da compra é exibido para conferência **antes** da confirmação.

#### FR-8: Gerar Código de Reserva único e legível

O sistema gera, para cada Reserva, um Código de Reserva único no formato `AAA-00000`. Realiza UJ-1, UJ-2.

**Consequências (testáveis):**
- O código casa com o padrão de três letras maiúsculas, hífen e cinco dígitos.
- Duas Reservas nunca compartilham o mesmo código — garantido por restrição de unicidade no banco, não apenas por probabilidade no código da aplicação.
- **A unicidade é testável por construção, não por sorteio.** Gerar N códigos e verificar que não repetiram é teste fraco: com 1,76 bilhão de combinações, ele passa por acaso. O teste exigido força a colisão — o gerador é injetável, e um gerador de teste que devolve o mesmo código duas vezes deve resultar em duas Reservas com códigos diferentes, provando que o tratamento de colisão existe e funciona.
- Colisão na geração é tratada com nova tentativa, sem propagar erro ao Passageiro.
- Esgotadas as tentativas, a falha é explícita — nunca uma Reserva sem código ou com código duplicado.
- A tela de sucesso exibe o Código de Reserva de forma destacada e copiável, imediatamente após a confirmação — é o único artefato que o Passageiro leva da compra.

#### FR-9: Impedir Reserva de Assento Ocupado

O sistema recusa Reserva para Assento já Ocupado na mesma Viagem, **inclusive sob requisições concorrentes**. Realiza UJ-1.

**Consequências (testáveis):**
- Segunda Reserva para o mesmo Assento e Viagem é recusada com erro explícito.
- Duas requisições simultâneas para o mesmo Assento resultam em **exatamente uma** Reserva Confirmada; a outra é recusada. A garantia é do banco de dados — restrição de unicidade sobre (Viagem, Assento) entre Reservas Confirmadas — e não de verificação em memória antes da escrita.
- O Assento continua reservável depois que a Reserva anterior é Cancelada.
- A recusa preserva os dados já digitados no formulário e recarrega o Mapa de Assentos com o estado atual.

#### FR-10: Impedir Reserva de Viagem Realizada

O sistema recusa Reserva para Viagem cuja data/hora de partida já passou. Realiza UJ-1.

**Consequências (testáveis):**
- Reserva para Viagem com partida no passado é recusada com erro explícito.
- Reserva para Viagem com partida no futuro, ainda que a minutos dela, é aceita — a restrição de duas horas vale para cancelamento (FR-13), não para venda.
- A comparação usa o Relógio da Aplicação, permitindo teste determinístico dos dois lados da fronteira.
- **Esta regra tem teste automatizado obrigatório neste PRD**, ainda que a especificação não o exija. É a única das cinco regras sem superfície de interface (consequência de FR-2) e sem métrica própria; sem teste, ficaria inteiramente não verificada.

### 4.4 Consulta e Cancelamento

**Descrição:** o pós-venda que hoje exige atendimento humano. Sem conta e sem login, o Código de Reserva é a única chave.

#### FR-11: Consultar Reserva pelo Código de Reserva

Qualquer pessoa que possua um Código de Reserva pode consultar os dados daquela Reserva. Realiza UJ-2.

**Consequências (testáveis):**
- A resposta traz Viagem, Rota, data/hora de partida, número do Assento, Status e dados do Passageiro.
- **Reserva Cancelada é retornada normalmente, com Status `Cancelada`.** O portador do código é o titular; esconder dele o desfecho da própria Reserva tornaria a Tela 4 inútil e UJ-2 impossível.
- Código inexistente retorna `404` uniforme.
- A consulta é insensível a maiúsculas e minúsculas, e tolera o código com ou sem o hífen.
- A interface distingue visualmente Reserva Confirmada de Cancelada, e só oferece o botão de cancelar quando FR-12 e FR-13 permitem.

#### FR-12: Cancelar Reserva

O Passageiro pode cancelar uma Reserva Confirmada pelo Código de Reserva, desde que dentro da Janela de Cancelamento. Realiza UJ-2.

**Consequências (testáveis):**
- Cancelamento muda o Status para `Cancelada` e libera o Assento (verificável via FR-3).
- Cancelar Reserva já Cancelada não gera efeito nem novo erro de estado — a operação é idempotente.
- Cancelar código inexistente retorna "não encontrado".

#### FR-13: Fazer valer a Janela de Cancelamento

O sistema recusa cancelamento a menos de 2 horas da partida da Viagem. Realiza UJ-2.

**Consequências (testáveis):**
- Cancelamento a 2h01min da partida é aceito.
- **Cancelamento a exatamente 2h00min da partida é recusado.** A Janela de Cancelamento é exclusiva: ela fecha ao completar as 2 horas. Leitura conservadora, decidida deliberadamente — a especificação é silenciosa, e o teste do limite depende dela.
- Cancelamento a 1h59min da partida é recusado, com a razão explícita.
- Cancelamento de Reserva cuja Viagem já partiu é recusado.
- O teste do limite cobre os três pontos da fronteira — 2h01min, 2h00min e 1h59min — não apenas o caso confortável.
- A comparação usa o Relógio da Aplicação. **Sem relógio injetável este teste é inescrevível** de forma determinística: ou depende de dados semeados no instante certo, ou fica intermitente. É um requisito de testabilidade com consequência direta sobre um dos quatro testes exigidos pela especificação.
- **Precedência sobre a idempotência de FR-12:** cancelar uma Reserva **já Cancelada** cuja Viagem parte em menos de 2 horas **sucede silenciosamente**, sem erro. A Janela de Cancelamento guarda a *transição* de Confirmada para Cancelada; se não há transição a fazer, não há nada a impedir. A regra só recusa quando a Reserva está Confirmada.

**Notas:** `[NOTE FOR PM]` A referência de tempo — hora do servidor, UTC ou fuso local — é decisão de arquitetura, mas tem consequência de produto: o Passageiro raciocina no fuso dele. Levar para `bmad-architecture` (registrada em §12, item 1).

### 4.5 Catálogo Semeado

**Descrição:** sem Rotas e Viagens no banco, o produto não é demonstrável e o avaliador não consegue exercitar nada no primeiro `up`. Não é feature de produto no sentido usual, mas é pré-condição de todas as outras.

#### FR-14: Semear Rotas e Viagens ao iniciar

O ambiente sobe com Rotas e Viagens suficientes para exercitar todo o fluxo, sem passo manual.

**Consequências (testáveis):**
- Depois de `docker-compose up --build`, FR-1 retorna Rotas e FR-2 retorna Viagens sem nenhuma intervenção.
- **As datas de partida semeadas são relativas ao instante de execução do seed** — "hoje + 3 dias", "agora + 40 minutos" — nunca literais de calendário. Este é o requisito mais fácil de ignorar e o de pior consequência: com datas fixas, toda Viagem semeada vira Viagem Realizada com a passagem do tempo, FR-2 as esconde da busca por decisão explícita, e **o avaliador abre a aplicação semanas depois e encontra uma busca que não retorna nada**. A entrega inteira parece quebrada sem estar.
- O conjunto semeado inclui pelo menos uma Viagem com Assentos parcialmente Ocupados, para que o Mapa de Assentos mostre os dois estados na primeira visita.
- Inclui ao menos uma Viagem dentro da Janela de Cancelamento e uma fora dela, além de uma Viagem Realizada e uma Viagem Esgotada — tornando FR-9, FR-10 e FR-13 demonstráveis manualmente.
- O seed é idempotente: subir o ambiente duas vezes não duplica Rotas nem Viagens.

## 5. Contrato de API — Superfície Pública

A superfície abaixo é **dada pela especificação**, não escolhida. Vale como contrato: os caminhos e verbos não são livres, e o frontend consome exatamente esta forma.

| Método | Caminho | Capacidade | FRs |
|---|---|---|---|
| `GET` | `/rotas` | Listar Rotas | FR-1 |
| `GET` | `/viagens` | Buscar Viagens por origem, destino e data | FR-2 |
| `GET` | `/viagens/{id}` | Detalhar Viagem com Assentos Livres/Ocupados | FR-3, FR-4 |
| `POST` | `/reservas` | Criar Reserva | FR-6..FR-10 |
| `GET` | `/reservas/{codigo}` | Consultar Reserva pelo Código de Reserva | FR-11 |
| `DELETE` | `/reservas/{codigo}` | Cancelar Reserva | FR-12, FR-13 |

**Semântica de resposta.** Os três tipos de recusa precisam ser distinguíveis pelo cliente sem interpretar texto de mensagem — FR-9 depende disso para recarregar o Mapa de Assentos preservando o formulário, e um cliente que faz *string matching* em mensagem de erro é frágil por construção:

| Situação | Status | Onde ocorre |
|---|---|---|
| Sucesso na leitura | `200` | FR-1, FR-2, FR-3, FR-11 |
| Reserva criada | `201`, com o recurso e o Código de Reserva | FR-7, FR-8 |
| Cancelamento efetivado | `204` | FR-12 |
| Entrada inválida (CPF, campo ausente, parâmetro de busca faltando) | `400` | FR-2, FR-6, FR-7 |
| Recurso inexistente (Código de Reserva ou Viagem desconhecidos) | `404` | FR-3, FR-11, FR-12 |
| **Recusa por regra de negócio** (Assento Ocupado, Viagem Realizada, Janela fechada) | `409` | FR-9, FR-10, FR-13 |

**Demais consequências transversais:**
- Nenhum endpoint exige autenticação — não há autenticação no MVP.
- Toda recusa carrega corpo de erro em formato consistente entre endpoints, com um identificador de motivo legível por máquina além da mensagem para humano.
- `409` é deliberadamente distinto de `400`: entrada inválida é culpa do que foi digitado, recusa por regra é o estado do mundo tendo mudado. O frontend reage de forma diferente a cada um.
- A superfície é documentada em Swagger navegável (DR-3).

`[ASSUMPTION]` A escolha específica de `409` para recusa de negócio, `201` com corpo e `204` no cancelamento é leitura minha — a especificação é silenciosa sobre status. Qualquer mapeamento coerente serve; o requisito real é que os três tipos sejam distinguíveis e que a escolha esteja documentada no README.

## 6. NFRs Transversais

- **Concorrência (o item de maior peso da entrega).** A unicidade de (Viagem, Assento) entre Reservas Confirmadas é garantida pelo banco de dados. Verificar disponibilidade em memória e depois inserir é implementação incorreta, ainda que passe em teste sequencial. FR-9 tem uma consequência dedicada a isso.
- **Testabilidade dirigida.** Os cinco testes de regra de negócio estão mapeados no preâmbulo de §4: quatro exigidos pela especificação (FR-6, FR-8, FR-9, FR-13) e o de FR-10, acrescentado por este PRD. A especificação avisa explicitamente que não é necessário testar cada linha. Cobrir esses cinco com clareza e justificar a fronteira é o requisito; perseguir percentual de cobertura, não.
- **Testes de comportamento no frontend.** Os três testes exigidos — busca, Mapa de Assentos, validação de formulário — verificam o que o usuário faz e vê, não detalhe de implementação. Mock da API é permitido e recomendado.
- **Reprodutibilidade.** Um único comando sobe o ambiente completo do zero, em máquina limpa, sem passo manual não documentado (DR-1).
- **Acessibilidade mínima.** Estados de Assento não podem depender exclusivamente de cor (FR-4). É o único requisito de acessibilidade assumido no MVP.
- **Relógio injetável.** FR-10 e FR-13 comparam contra o Relógio da Aplicação, nunca contra o relógio do sistema chamado direto. Sem isso, o teste da Janela de Cancelamento — um dos quatro exigidos — não é escrevível de forma determinística.
- **Domínio isolado, com uma fronteira honesta.** As regras que são pura decisão — validação de CPF (FR-6), aritmética da Janela de Cancelamento (FR-13), Viagem Realizada (FR-10), formato do Código de Reserva (FR-8) — ficam testáveis sem subir infraestrutura. **FR-9 é a exceção declarada:** sua garantia é a restrição de unicidade do banco, então prová-la exige banco real. Isolar o domínio não significa fingir que FR-9 é testável em memória; significa saber qual regra vive de que lado da fronteira. Essa distinção é, em si, o sinal de arquitetura que o avaliador procura.

## 7. Restrições e Guardrails

### 7.1 Restrições Tecnológicas (dadas, não escolhidas)

A especificação **impõe** a stack abaixo. Não são decisões de arquitetura e não pertencem ao addendum: um arquiteto livre para escolher poderia legitimamente propor Dapper, Next.js ou JavaScript sem tipos, e estaria descumprindo o desafio.

| Camada | Obrigatório | Livre |
|---|---|---|
| Backend | .NET 8+ (ASP.NET Core Web API) | — |
| Persistência | Entity Framework Core sobre banco relacional | PostgreSQL **ou** SQL Server |
| Testes backend | Framework de teste automatizado | xUnit **ou** NUnit |
| Frontend | React 18+ **com TypeScript** | — |
| Estado no frontend | — | Context API, Zustand, Redux, outro |
| Testes frontend | React Testing Library | Jest **ou** Vitest |
| Empacotamento | Docker + docker-compose; frontend servido por Nginx ou similar | — |

Os itens da coluna "Livre" são as únicas escolhas reais, e cada uma precisa de justificativa no README (DR-2). Estão detalhadas no addendum e listadas em §12, item 2.

### 7.2 Privacidade

O produto coleta nome, CPF, e-mail e data de nascimento — dado pessoal, e o CPF é identificador sensível sob a LGPD. Guardrails proporcionais a um MVP de desafio técnico:

- Nenhum dado pessoal real vai para o repositório público. O catálogo semeado (FR-14) usa Passageiros fictícios com CPFs sintéticos válidos.
- Dado pessoal não aparece em log de aplicação.
- **O Código de Reserva é a credencial de acesso à Reserva**, e é a única. Quem o possui é tratado como titular: consulta (FR-11) e cancelamento (FR-12) funcionam integralmente, inclusive para Reserva Cancelada — ver abaixo.
- O risco real não é vazamento por status, é **enumeração**. `AAA-00000` oferece cerca de 1,76 bilhão de combinações, o que resiste a adivinhação isolada e não resiste a varredura automatizada. Códigos são gerados de forma imprevisível — nunca sequenciais nem derivados do identificador da Reserva —, e `GET /reservas/{codigo}` responde `404` de forma uniforme para código inexistente, sem revelar nada sobre o espaço de códigos.

> **Correção de um guardrail anterior.** Uma versão deste PRD exigia que Reserva Cancelada e código inexistente respondessem de forma indistinguível. Isso está **revogado**: tornava a Tela 4 inútil, contradizia UJ-2 (Carla precisa ver o Status para decidir) e destruía a base da idempotência de FR-12. Esconder do titular o que aconteceu com a própria Reserva não protege ninguém. Reserva Cancelada é retornada normalmente por FR-11, com Status `Cancelada`.

`[ASSUMPTION]` Conformidade formal com LGPD — base legal, retenção, direito de eliminação, consentimento — está fora de escopo. Reconhecer a questão e declarar o limite é a postura correta aqui; implementar um programa de privacidade num MVP de desafio seria excesso. Vale uma linha no README.

### 7.3 Superfície pública do repositório

O repositório é público (DR-7). Nenhuma credencial, string de conexão de ambiente real, chave ou dado pessoal real pode ser versionado. Credenciais de desenvolvimento usadas pelo `docker-compose` são valores locais óbvios e descartáveis, declarados como tal — e um `.env.example` documenta o que existe sem expor nada que importe.

## 8. Requisitos de Entrega e Avaliação

Esta seção não estava em nenhum template. Ela existe porque a especificação torna estes itens **obrigatórios e pontuados**, e eles não são feature de produto — se ficarem fora do PRD, não viram épico, não viram tarefa, e são exatamente onde entregas de desafio se perdem. Numeração própria (`DR-n`) para não contaminar o espaço de FRs.

### DR-1: Ambiente completo em um comando

`docker-compose up --build` sobe API, banco de dados e frontend, do zero, em máquina limpa.

**Consequências (testáveis):** nenhum passo manual não documentado; migrações aplicadas na inicialização; catálogo semeado disponível ao fim do comando (FR-14); o frontend é servido por servidor web em container (Nginx ou equivalente), não por servidor de desenvolvimento.

### DR-2: README completo

Cobre os cinco itens obrigatórios: como rodar com e sem Docker; tecnologias e bibliotecas usadas **e por quê**; decisões de arquitetura relevantes; o que foi implementado e o que ficou de fora; como rodar os testes.

**Consequências (testáveis):** documenta nominalmente cada divergência deliberada e cada leitura escolhida onde a especificação era silenciosa — data de nascimento coletada (FR-7), fronteira **exclusiva** de 2h no cancelamento (FR-13), Viagem Realizada omitida da busca e portanto FR-10 exercitável só via API (FR-2, FR-10), Layout do Ônibus fixo em 44 Assentos, enumeração de Status com dois valores, limite de conformidade LGPD (§7.2).

### DR-3: Documentação de endpoints

Swagger navegável, com link no README.

### DR-4: Histórico de git incremental

Commits regulares e legíveis, refletindo a construção. `[ASSUMPTION]` O diretório do projeto **ainda não é um repositório git** — `git init` é a primeira tarefa da implementação. Commit único ao final custa pontos num critério explicitamente avaliado.

### DR-5: Evidência visual

Screenshots ou gif da aplicação rodando, no README.

### DR-6: Pontos de melhoria declarados

O que seria implementado com mais tempo, e o que ficou de fora e por quê. A especificação convida a isso literalmente: honestidade documentada pontua mais que escopo inflado.

### DR-7: Submissão da entrega

O trabalho só conta se chegar ao avaliador na forma pedida. A especificação define o protocolo de submissão, e ele é parte da entrega — não um passo administrativo posterior.

**Consequências (testáveis):**
- O repositório é **público** no GitHub ou GitLab, e acessível sem convite.
- O link é enviado ao e-mail de recrutamento.
- O e-mail declara **qual(is) parte(s) foram entregues** — neste caso, backend e frontend — mais observações relevantes.
- A entrega não depende de nenhum artefato local não versionado para subir (verificável clonando o repositório em diretório novo e executando DR-1).

### DR-8: Execução local sem Docker

Existe um caminho documentado e **funcional** para rodar a aplicação sem Docker. A especificação exige que o README explique "como rodar o projeto localmente (com e sem Docker)" — logo, o caminho sem Docker precisa existir de fato, não apenas ser descrito.

**Consequências (testáveis):**
- O README traz os passos concretos: banco local, string de conexão, comando de migração, comando de execução da API, comando de execução do frontend.
- Os passos funcionam numa máquina que tenha apenas os SDKs instalados, sem Docker.
- O catálogo semeado (FR-14) também é aplicado por este caminho — caso contrário, quem seguir por aqui encontra a busca vazia.

`[ASSUMPTION]` Promovido de questão aberta a requisito. A obrigação estava implícita em DR-2 — o README tem de explicar o caminho — sem que nenhum requisito garantisse que ele funciona. Documentar um caminho quebrado é pior que não o oferecer.

## 9. Non-Goals

- **Não é uma plataforma de pagamentos.** A Reserva é o fim do fluxo. Não há cobrança, gateway, reembolso ou estorno.
- **Não é um sistema de contas.** Sem cadastro, login, sessão, recuperação de senha ou histórico de compras.
- **Não é um sistema de gestão da operadora.** Sem painel administrativo, sem CRUD de Rotas ou Viagens pela interface, sem relatório, sem gestão de frota ou motorista.
- **Não se torna um marketplace.** Uma única operadora. Sem comparação entre empresas, sem integração com terceiros.
- **Não é um canal de comunicação.** Sem e-mail de confirmação, SMS, push ou lembrete de embarque.
- **Não modela a viagem física.** Sem embarque, check-in, validação de bilhete a bordo ou controle de ocupação real.

## 10. Escopo do MVP

### 10.1 Em Escopo

- Os seis endpoints de §5, com Swagger.
- **As cinco regras de negócio da especificação:** FR-6, FR-8, FR-9, FR-10, FR-13.
- Os quatro testes de backend exigidos (FR-6, FR-8, FR-9, FR-13), **mais o teste de FR-10**, que este PRD acrescenta por ser a única regra sem outra forma de verificação.
- As quatro telas: busca, Mapa de Assentos, dados do Passageiro com confirmação e sucesso, consulta de Reserva. **A Tela 4, marcada como bônus pela especificação, está dentro do escopo.**
- Os três testes de frontend exigidos: busca, Mapa de Assentos, validação de formulário.
- DR-1 a DR-8.
- Catálogo semeado com datas relativas (FR-14).
- Tratamento de concorrência garantido pelo banco (FR-9, §6).
- **Pipeline de CI** executando os testes a cada push. Estava na camada de diferenciação do brief e chegou a sumir deste PRD sem entrar nem em escopo nem em não-escopo; entra explicitamente aqui, na Faixa 3 de §10.3.

### 10.2 Fora de Escopo no MVP

§9 lista o que este produto **não é**; a lista abaixo é o que ele poderia ser e não será agora. Os dois primeiros itens aparecem nas duas listas de propósito.

- **Autenticação e contas** — nenhum requisito da especificação a menciona.
- **Pagamento** — a Reserva encerra o fluxo.
- **Passagem de volta** — a especificação pede apenas data de ida.
- **Múltiplos Assentos ou Passageiros por Reserva.** `[NOTE FOR PM]` É o item deferido mais defensável de todos: comprar duas passagens juntas é comportamento real e frequente. Vale nomear em DR-6 como primeira evolução.
- **Layout do Ônibus configurável por Viagem** — modelo mais rico e mais próximo do real, rejeitado pelo custo em migração, seed e frontend dentro da janela de uma semana. Vale nomear em DR-6.
- **Tipos ou classes de Assento** (leito, semi-leito, executivo) e preço variável por Assento.
- **Painel administrativo**, notificação por e-mail, internacionalização, remarcação de passagem.
- **Conformidade formal com LGPD** — ver §7.2.

### 10.3 Ordem de corte — o que sai primeiro se a semana apertar

O brief definiu a regra e este PRD a torna operacional: **corta-se de baixo para cima, e todo corte é declarado no README (DR-6).** Sem esta ordem escrita antes de começar, o corte acontece sob pressão, na última noite, e cai sobre o que estava inacabado em vez do que era dispensável.

**Faixa 1 — núcleo inegociável.** Os seis endpoints, as cinco regras, os quatro testes exigidos, as Telas 1 a 3, os três testes de frontend, DR-1, DR-2, DR-4, DR-7, seed com datas relativas. Se isto não fecha, a entrega não acontece.

**Faixa 2 — o que decide a nota.** FR-9 com garantia de banco e teste de concorrência real; Swagger (DR-3); Tela 4 de consulta e cancelamento. A Tela 4 é bônus na especificação, mas sem ela FR-11, FR-12 e FR-13 não têm superfície de interface.

**Faixa 3 — diferenciação.** Pipeline de CI; evidência visual (DR-5); teste acrescentado de FR-10; pontos de melhoria elaborados (DR-6).

**Faixa 4 — primeiro a cair.** Refinamento visual além do funcional; qualquer generalização não exercida por FR (ver SM-C3).

`[NOTE FOR PM]` Se o corte chegar à Faixa 2, o cálculo muda de natureza: entregar as duas partes é o diferencial que a própria especificação nomeia, mas backend com FR-9 sólido e sem Tela 4 provavelmente pontua melhor que ambas as partes rasas. Decisão a revisitar no meio da semana, com código na mão — não agora.

## 11. Métricas de Sucesso

Este produto não vai a mercado; ele vai a avaliação. As métricas são, portanto, verificáveis por inspeção e execução — não por telemetria.

**Primárias**

- **SM-1: Ambiente sobe em um comando.** `docker-compose up --build` em máquina limpa resulta em aplicação funcional, sem passo manual não documentado. Binário. Valida DR-1, FR-14.
- **SM-2: Fluxo completo percorrível fim a fim contra a API real.** Buscar → selecionar Assento → informar dados → receber Código de Reserva → consultar → cancelar, sem mock. Valida FR-1, FR-2, FR-3, FR-4, FR-5, FR-7, FR-8, FR-11, FR-12. **Não valida FR-6, FR-9, FR-10 nem FR-13:** o caminho feliz não exercita CPF inválido, Assento tomado por concorrência, Viagem Realizada nem a fronteira da Janela de Cancelamento. Essas quatro são cobertas por SM-3 e SM-4, e é justamente por isso que existem como métricas separadas.
- **SM-3: Cada uma das cinco regras de negócio tem teste que falha se a regra for removida.** Cinco regras, cinco testes que *provam* a regra — não que apenas a exercitam. Valida FR-6, FR-8, FR-9, FR-10, FR-13. (Quatro são exigidos pela especificação; o de FR-10 é acréscimo deste PRD.)
- **SM-4: Reserva concorrente do mesmo Assento produz exatamente uma Reserva Confirmada.** Verificável por teste que dispara requisições simultâneas. Valida FR-9.

**Secundárias**

- **SM-5: README cobre os cinco obrigatórios e ao menos três itens marcados como plus.** Valida DR-2, DR-3, DR-5, DR-6.
- **SM-6: Histórico de git mostra construção incremental ao longo da semana.** Commits distribuídos em pelo menos três dias distintos, com mensagens que descrevem a mudança e não o arquivo tocado, e nenhum commit único contendo mais da metade do código. Barra deliberadamente concreta: "mais de um commit" passaria com dois, e dois commits não demonstram nada. Valida DR-4.
- **SM-7: Os três testes de frontend exigidos passam, verificando comportamento do usuário.** Valida FR-2, FR-4, FR-5, FR-6.
- **SM-8: Todo item cortado das Faixas 2 a 4 de §10.3 aparece nominalmente no README.** Verificável por comparação direta entre §10.3 e a seção "o que ficou de fora" do README — se um item da faixa foi cortado e não está lá, a métrica falha. Substitui a formulação anterior ("nenhuma omissão que o avaliador descubra sozinho"), que era infalsificável. Valida DR-6.
- **SM-9: O repositório clonado em diretório novo sobe com DR-1 sem ajuste.** Prova que nada essencial ficou fora do versionamento. Valida DR-7.

**Contra-métricas (não otimizar)**

- **SM-C1: Percentual de cobertura de testes.** A especificação diz explicitamente que não é necessário testar cada linha e que o que importa é mostrar *onde* os testes agregam valor. Perseguir cobertura produz testes triviais que diluem os quatro que importam. Contrabalança SM-3.
- **SM-C2: Quantidade de features entregues.** Todo escopo além de §10.1 compete com o núcleo pelo mesmo tempo. Uma feature extra vale menos que FR-9 feito corretamente. Contrabalança SM-2 e a ambição declarada de diferenciais.
- **SM-C3: Sofisticação arquitetural.** Camadas, padrões e abstrações que não são exercidos por nenhum FR viram custo de leitura para o avaliador. Domínio isolado é o suficiente; arquitetura hexagonal completa para seis endpoints sinaliza julgamento pior, não melhor. Contrabalança §6.

## 12. Questões Abertas

1. **Referência de tempo** para Viagem Realizada (FR-10) e Janela de Cancelamento (FR-13): hora do servidor, UTC ou fuso local do Passageiro? Decisão de arquitetura com consequência de produto — o Passageiro raciocina no fuso dele (ver a nota em FR-13). Levar para `bmad-architecture`.
2. **Escolhas de stack em aberto na especificação** — a coluna "Livre" de §7.1: PostgreSQL vs SQL Server, xUnit vs NUnit, gerenciador de estado do React, Jest vs Vitest, e ainda SQLite in-memory vs TestContainers. São decisões de arquitetura, catalogadas no addendum do brief; cada uma precisa de justificativa registrável no README (DR-2).
3. **Formato de erro da API**: um envelope consistente entre endpoints está em §5 como consequência, mas a forma concreta é decisão de arquitetura.
4. **Corpo do `DELETE /reservas/{codigo}`**: a recusa por Janela de Cancelamento (FR-13) precisa comunicar a razão. Confirmar em arquitetura que o verbo escolhido pela especificação suporta isso adequadamente.
5. **Volume do catálogo semeado** (FR-14): quantas Rotas e Viagens tornam a demonstração convincente sem inflar a migração?
6. **Vaga e pesos de avaliação** não são conhecidos. Os nove critérios de §11 estão tratados como igualmente obrigatórios — se você souber que a vaga pende para backend ou frontend, isso muda a priorização dentro da semana e a leitura da `[NOTE FOR PM]` em §10.3.
7. **Validação do formulário além do CPF** (FR-6 cobre só o CPF): que regra vale para nome e e-mail? E-mail precisa de verificação de formato; nome precisa de comprimento mínimo. Um dos três testes de frontend exigidos é "validação do formulário de passageiro" — sem estas regras definidas, o teste não tem o que asserir além do CPF.
8. **Estados de erro de API no frontend.** As telas têm estado de carregamento e estado vazio definidos (FR-2), mas nenhum requisito descreve o que o Passageiro vê quando a API falha ou está fora do ar. Sem isso, o implementador provavelmente confundirá "erro" com "vazio" — e são coisas diferentes para quem está comprando.
9. ~~**O caminho "sem Docker"** é obrigatório no README (DR-2) e não tem requisito correspondente em nenhum FR ou DR.~~ **Resolvido nesta sessão:** promovido a requisito próprio — ver DR-8.

## 13. Índice de Suposições

Cada item aberto precisa de confirmação explícita. Suposição não confirmada que chega em implementação vira retrabalho. Itens riscados já foram resolvidos nesta sessão e permanecem aqui como registro.

1. **§2.3** — As três jornadas de usuário foram redigidas por mim a partir das telas da especificação, não narradas por você. Protagonistas e contextos são inventados.
2. **§2.3 (UJ-3)** — Validação de CPF dispara no frontend ainda no campo, antes de qualquer chamada à API.
3. **§3** — Status da Reserva tem exatamente dois valores, `Confirmada` e `Cancelada`. A especificação não os enumera.
4. ~~**§4.1 (FR-2)** — Viagem Realizada é omitida dos resultados de busca.~~ **Resolvido nesta sessão: confirmado.** Consequência aceita: FR-10 fica demonstrável apenas via API, coberto pelo seed e apontado no README (DR-2).
5. ~~**§4.3 (FR-7)** — Data de nascimento é coletada no formulário e no payload, honrando a tabela de entidades contra a lista literal de campos do formulário.~~ **Resolvido nesta sessão:** deixa de ser suposição e passa a ser requisito de FR-7, a declarar no README (DR-2).
6. ~~**§4.4 (FR-13)** — A fronteira de exatamente 2h00min antes da partida é permitida (limite inclusivo).~~ **Resolvido nesta sessão: a fronteira é exclusiva** — 2h00min já recusa. Deixa de ser suposição e passa a ser requisito, a declarar no README (DR-2).
7. ~~**§3, §4.2** — Layout do Ônibus fixo: 44 Assentos, 2+2, 11 fileiras, idêntico para toda Viagem.~~ **Resolvido nesta sessão:** é requisito de FR-4, a declarar no README (DR-2).
8. **§7.2** — Conformidade formal com LGPD está fora de escopo; reconhecer e declarar o limite é a postura adotada.
9. **§8 (DR-4)** — O diretório do projeto ainda não é repositório git; `git init` é a primeira tarefa da implementação.
10. **§2.2** — Não existe usuário administrativo; Rotas e Viagens entram exclusivamente por seed.
11. **§11** — Nenhum peso relativo entre critérios de avaliação é conhecido; os nove critérios são tratados como igualmente obrigatórios.
12. **§3 (Viagem)** — O campo "assentos disponíveis" da entidade é modelado como valor derivado, não como contador persistido. Divergência deliberada da leitura literal da tabela de entidades.
13. **§4.1 (FR-2)** — Viagem Esgotada continua aparecendo na busca, marcada como indisponível, ao contrário de Viagem Realizada. A especificação não trata o caso.
14. **§5** — O mapeamento de status HTTP (`409` para recusa de negócio, `201` com corpo, `204` no cancelamento) é leitura minha; a especificação é silenciosa. O requisito real é distinguibilidade e documentação.
15. **§4.4 (FR-13)** — Cancelar Reserva já Cancelada fora da Janela de Cancelamento **sucede silenciosamente**: a janela guarda a transição, não o estado final. Resolve a interação antes indefinida entre a idempotência de FR-12 e a janela de FR-13.
16. **§4.5 (FR-14)** — Datas de partida semeadas são relativas ao instante de execução, nunca literais de calendário.
17. **§6, §4.3 (FR-8)** — O gerador de Código de Reserva é injetável, para que a colisão possa ser forçada em teste em vez de esperada por acaso.
