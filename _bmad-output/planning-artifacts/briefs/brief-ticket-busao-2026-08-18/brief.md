---
title: "Product Brief: OniBus Express (ticket-busao)"
status: draft
created: 2026-08-18
updated: 2026-08-18
---

# Product Brief: OniBus Express

**Entrega:** Backend (.NET) + Frontend (React) · **Janela:** ~1 semana · **Postura:** obrigatório 100% cumprido, mais diferenciais deliberados

## Resumo Executivo

O OniBus Express é o MVP de um sistema de venda de passagens rodoviárias online: o passageiro busca viagens por origem, destino e data, escolhe um assento num mapa visual, informa seus dados e recebe um código de reserva legível que pode consultar ou cancelar depois. Backend em ASP.NET Core com Entity Framework Core sobre banco relacional; frontend em React 18 com TypeScript; ambos containerizados e sobem com um único comando.

O que este brief posiciona, porém, não é o produto — é a **entrega**. Os requisitos já vêm fechados por uma especificação de desafio técnico: endpoints, regras de negócio, telas e critérios de teste são dados, não descobertos. O trabalho real de produto aqui é outro: decidir onde investir esforço quando o que está sendo avaliado não é o volume de features, mas o **julgamento de engenharia** por trás delas.

A aposta é entregar as duas partes — que a especificação declara explicitamente como diferencial pontuado — com o núcleo obrigatório sólido e sem furos, e uma camada de diferenciais escolhidos por sinalizarem maturidade em vez de esforço: tratamento de concorrência na reserva de assento, testes de integração com banco real efêmero, e documentação que expõe trade-offs em vez de escondê-los.

## Contexto e Propósito

Este é um desafio técnico de candidatura para vaga de Desenvolvedor Full Stack, avaliado sobre .NET, ReactJS, Docker e testes. A entrega é um repositório público, e a especificação avisa que o **histórico de git também será analisado**.

O brief existe para uma coisa: impedir que a execução confunda o usuário da ficção com o usuário do artefato. Sem essa distinção travada por escrito, o esforço migra naturalmente para o que é visível e divertido de construir — e o que é pontuado fica para o final, apressado.

## O Problema

Este produto tem duas camadas de problema, e só uma delas é a que importa para as decisões de escopo.

**A camada ficcional.** A OniBus Express vende passagens por canais que não escalam — presencialmente no guichê ou por telefone. O passageiro não consegue ver disponibilidade real, não escolhe onde senta, não tem comprovante consultável, e cancelar exige contato humano. A empresa carrega custo operacional em cada venda e perde a venda que acontece fora do horário do guichê. Este é o problema que o software resolve na história.

**A camada real.** Um avaliador técnico vai abrir este repositório e precisa responder, em pouco tempo, se o candidato sabe organizar código, escolher onde testar, tomar decisões defensáveis e comunicá-las. O problema real é que **a maior parte dos sinais que respondem essa pergunta não está nas features** — está no README, no histórico de commits, na escolha de quais quatro coisas testar, e em como as ambiguidades da própria especificação foram tratadas. Entregas que atacam só a camada ficcional produzem código que funciona e não convence.

## A Solução

Um fluxo de compra completo, fim a fim, mais a consulta pós-venda:

**Backend** — API REST com seis endpoints (listar rotas; buscar viagens por origem/destino/data; detalhar viagem com assentos livres e ocupados; criar reserva; consultar reserva por código; cancelar reserva). Quatro regras de negócio guardam a integridade do domínio: assento ocupado não pode ser revendido, viagem já partida não aceita reserva, CPF é validado por formato e dígito verificador, e cancelamento só vale até duas horas antes da partida. O código de reserva é único e legível por humano no formato `ABC-12345`.

**Frontend** — Quatro telas: busca com estados de loading e vazio; mapa visual de assentos com os três estados (livre, ocupado, selecionado); formulário de passageiro com validação e resumo antes de confirmar, terminando na tela de sucesso com o código; e a consulta de reserva por código, com opção de cancelamento. A quarta tela é marcada como bônus pela especificação e está **dentro** do escopo desta entrega.

**Infraestrutura** — `docker-compose up --build` sobe API, banco e frontend do zero, sem passo manual.

## Quem Isso Serve

**Usuário primário: o avaliador técnico.** Lê código antes de rodar, e provavelmente roda antes de ler tudo. Sucesso para essa pessoa é chegar ao fim do README sem dúvida sobre como subir o projeto, encontrar testes que cobrem o que importa em vez de inflar cobertura, e ver o candidato reconhecendo o que não fez. A especificação diz literalmente que não esperam perfeição — esperam entender o raciocínio. Honestidade documentada pontua mais que escopo inflado.

**Usuário da ficção: o passageiro.** Quer comprar uma passagem em poucos cliques, escolher onde vai sentar, e ter um código que funcione depois. Serve para manter as decisões de UX coerentes — não para justificar features que ninguém pediu.

**[ASSUMPTION]** Não há usuário administrativo. Nenhum requisito menciona cadastro de rotas ou viagens pela interface, então rotas e viagens entram por seed de dados.

## O Que Torna Isso Diferente

Nenhum moat técnico — seria desonesto inventar um. Um desafio técnico não tem vantagem competitiva; tem execução que se destaca ou não. Três apostas concretas:

- **Ambas as partes entregues.** A especificação declara isso como diferencial pontuado. É o único "diferencial" nomeado pelo próprio avaliador, e portanto o de retorno mais garantido.
- **Concorrência tratada onde ela realmente existe.** "Não reservar assento ocupado" é trivial de implementar como um `SELECT` seguido de `INSERT` — e errado sob duas requisições simultâneas. Tratar isso de verdade, e explicar por quê no README, é um sinal de senioridade que a maioria das entregas não dá.
- **Testes escolhidos, não medidos.** A especificação pede quatro cenários específicos e avisa que não é necessário testar cada linha. Cobrir exatamente esses quatro com clareza, e justificar a fronteira, demonstra o julgamento pedido.

## Critérios de Sucesso

Verificáveis, não aspiracionais:

1. `docker-compose up --build` sobe o ambiente completo do zero, em máquina limpa, sem passo manual não documentado.
2. Os seis endpoints respondem conforme a especificação, com Swagger navegável.
3. As quatro regras de negócio exigidas têm cada uma pelo menos um teste que falha se a regra for removida.
4. O fluxo das quatro telas é percorrível fim a fim contra a API real, não apenas contra mock.
5. Os três testes de frontend exigidos (busca, mapa de assentos, validação de formulário) passam, testando comportamento do usuário.
6. O README cobre os cinco itens obrigatórios e ao menos os três itens marcados como plus.
7. O histórico de git mostra commits incrementais e legíveis — não um commit único no final.
8. O que ficou de fora está declarado explicitamente, com o que seria feito com mais tempo.

## Escopo

**Núcleo obrigatório (não negociável).** Os seis endpoints, as quatro regras de negócio, os quatro testes de backend exigidos, `docker-compose` de um comando, as quatro telas, os três testes de frontend exigidos, README completo.

**Camada de diferenciação (a ambição declarada).** Swagger publicado; screenshots ou gif da aplicação rodando; tratamento explícito de concorrência na reserva de assento; testes de integração contra banco efêmero; arquitetura em camadas com o domínio isolado de infraestrutura; pipeline de CI rodando os testes; seed de dados que torna o app demonstrável no primeiro `up`.

**Explicitamente fora.** Autenticação e contas de usuário (nenhum requisito menciona). Pagamento — a reserva é o fim do fluxo. Múltiplos passageiros por reserva. Passagem de volta: a especificação pede apenas "Data de ida". Painel administrativo. Notificação por e-mail. Internacionalização. Tipos ou classes de assento. Remarcação de passagem.

**[ASSUMPTION]** A camada de diferenciação é cortável sob pressão de tempo, o núcleo não. Se a semana apertar, corta-se de baixo para cima na lista de diferenciação e declara-se o corte no README — comportamento que a própria especificação convida.

## Riscos e Decisões Abertas

**O risco principal é dispersão.** A ambição declarada é impressionar com diferenciais, e diferenciais competem com o núcleo pelo mesmo tempo. A mitigação é sequencial, não paralela: núcleo inteiro funcionando antes de qualquer item da camada de diferenciação.

**Este diretório ainda não é um repositório git.** Como o histórico será avaliado, isso é requisito de entrega, não detalhe de processo, e precisa ser a primeira ação da implementação.

**Conflito dentro da própria especificação.** O campo `data de nascimento` consta na entidade Passageiro, mas não aparece no formulário da Tela 3 nem no payload de `POST /reservas`. Precisa de decisão consciente e justificada — resolver essa ambiguidade bem é, em si, um sinal avaliado.

**Escolhas de stack deixadas em aberto pela especificação** (PostgreSQL vs SQL Server, xUnit vs NUnit, gerenciador de estado do React, Jest vs Vitest, estratégia de banco nos testes de integração, layout e capacidade do ônibus, valores possíveis do status da reserva) são decisões de arquitetura, não de produto. Estão catalogadas em `addendum.md` e serão resolvidas na fase de arquitetura, onde cada uma ganha justificativa registrável no README.

**[ASSUMPTION]** Não há data-limite rígida conhecida além da janela de ~1 semana informada, e nenhum peso relativo entre os critérios de avaliação foi divulgado — o plano trata os oito critérios de sucesso como igualmente obrigatórios.
