---
title: "Addendum — ticket-busao / OniBus Express"
status: draft
created: 2026-08-18
updated: 2026-08-18
---

# Addendum

Profundidade fornecida pelo usuário que pertence a documentos downstream (PRD, arquitetura, épicos e histórias) e não cabe no brief de 1-2 páginas.

## Origem do material

O conteúdo abaixo é a **especificação original do desafio técnico** colada pelo usuário na sessão de descoberta de 2026-08-18. É a fonte canônica de requisitos: o brief não a substitui, ele a posiciona. Toda regra de negócio, endpoint e requisito de teste aqui listado é **requisito dado, não inferido**.

---

## Especificação original: Desafio Técnico — Desenvolvedor Full Stack

**Stack avaliada:** .NET • ReactJS • Docker • Testes
**Projeto:** OniBus Express — Sistema de Venda de Passagens Rodoviárias

### 1. Sobre o desafio

Desafio técnico da OniBus Express, criado para avaliar habilidades práticas em desenvolvimento de software, cobrindo desde o design de APIs até a construção de interfaces modernas.

O candidato pode entregar **somente o Backend**, **somente o Frontend**, ou **ambos**. Entregar as duas partes é considerado diferencial e é avaliado positivamente na pontuação final.

Declaração explícita do avaliador: *"Não esperamos perfeição — queremos entender como você pensa, organiza o código e resolve problemas. Documente suas decisões e, se não terminar tudo, explique o que faria diferente com mais tempo."*

### 2. Contexto do projeto

A OniBus Express é uma empresa de transporte rodoviário que precisa modernizar seu sistema de vendas. O candidato foi "contratado" para construir o MVP do novo sistema, que deve permitir busca e compra de passagens de ônibus online.

#### Entidades principais

| Entidade | Campos |
|---|---|
| **Rota** | Origem, destino, duração estimada |
| **Viagem** | Rota associada, data/hora de partida, preço base, assentos disponíveis |
| **Passageiro** | Nome, CPF, e-mail, data de nascimento |
| **Reserva/Passagem** | Viagem, passageiro, número do assento, status, código de reserva |

### 3. Requisitos do Backend (.NET)

> Entregar se o foco da vaga é Back-End ou Full Stack.

#### 3.1 Tecnologias obrigatórias

- .NET 8+ (ASP.NET Core Web API)
- Entity Framework Core com banco relacional (PostgreSQL **ou** SQL Server)
- Docker + docker-compose para subir o ambiente
- Testes automatizados (xUnit **ou** NUnit)

#### 3.2 Endpoints mínimos esperados

```
GET    /rotas               — Listar todas as rotas disponíveis
GET    /viagens             — Buscar viagens por origem, destino e data
GET    /viagens/{id}        — Detalhes de uma viagem (assentos livres/ocupados)
POST   /reservas            — Criar reserva (nome, CPF, e-mail, viagem, assento)
GET    /reservas/{codigo}   — Consultar reserva pelo código gerado
DELETE /reservas/{codigo}   — Cancelar reserva
```

#### 3.3 Regras de negócio

- Não deve ser possível reservar um assento já ocupado
- Não deve ser possível reservar passagem para viagem já realizada
- CPF deve ser validado (formato **e** dígito verificador)
- O código de reserva deve ser único e legível (ex: `ABC-12345`)
- Cancelamento só permitido até **2 horas** antes da partida

#### 3.4 Requisitos de testes

Cobertura de testes unitários e/ou de integração para, no mínimo:

- Validação do CPF
- Regra de assento já ocupado
- Regra de cancelamento dentro do prazo
- Geração do código de reserva único

Dica do avaliador: usar banco em memória (SQLite in-memory ou TestContainers) para testes de integração. Não é necessário testar cada linha, mas mostrar que se sabe **onde os testes agregam valor**.

#### 3.5 Docker

O projeto deve ter um `docker-compose.yml` que suba toda a aplicação com um único comando:

```bash
docker-compose up --build
```

Incluir: API, banco de dados e, opcionalmente, migration automática ao iniciar.

### 4. Requisitos do Frontend (ReactJS)

> Entregar se o foco da vaga é Front-End ou Full Stack.

#### 4.1 Tecnologias obrigatórias

- React 18+ com TypeScript
- Gerenciador de estado à escolha (Context API, Zustand, Redux, etc.)
- Testes com React Testing Library + Jest **ou** Vitest
- Docker para servir a aplicação (Nginx ou similar)

#### 4.2 Telas requeridas

**Tela 1 — Busca de Passagens**
- Formulário com: Origem, Destino, Data de ida
- Botão de buscar
- Listagem de viagens disponíveis com preço, horário e vagas restantes
- Estado de loading e mensagem quando não há resultados

**Tela 2 — Seleção de Assento**
- Mapa visual dos assentos (livre / ocupado / selecionado)
- Exibir informações da viagem: rota, data, hora, preço
- Botão para prosseguir com o assento selecionado

**Tela 3 — Dados do Passageiro e Confirmação**
- Formulário: Nome completo, CPF, E-mail
- Validação dos campos no frontend
- Resumo da compra antes de confirmar
- Tela de sucesso com código da reserva após confirmação

**Tela 4 (Bônus) — Consulta de Reserva**
- Campo para digitar o código da reserva
- Exibir detalhes ou opção de cancelamento

#### 4.3 Requisitos de testes

No mínimo:

- Teste do componente de busca (simula preenchimento e busca)
- Teste do mapa de assentos (seleção, bloqueio de assentos ocupados)
- Teste de validação do formulário de passageiro

Dica do avaliador: preferir testar **comportamento do usuário** (clicar, preencher, ver resultado) em vez de detalhes de implementação. Mock da API é permitido e recomendado nos testes.

### 5. Requisitos do README

Obrigatório:

- Como rodar o projeto localmente (com e sem Docker)
- Quais tecnologias e bibliotecas foram usadas **e por quê**
- Decisões de arquitetura relevantes
- O que foi implementado e o que ficou de fora
- Como rodar os testes

Considerado plus:

- Screenshots ou gif da aplicação rodando
- Documentação dos endpoints (ou link para o Swagger)
- Pontos de melhoria que seriam implementados com mais tempo

### 6. Entrega

1. Criar repositório **público** no GitHub (ou GitLab)
2. Fazer **commits regulares** — o histórico de git também será analisado
3. Enviar o link do repositório para o e-mail de recrutamento
4. Incluir no e-mail: qual(is) parte(s) foram entregues e observações relevantes

---

## Observações para downstream

- **O repositório ainda não é um repo git.** O item 6.2 ("commits regulares... o histórico de git também será analisado") torna `git init` + estratégia de commits um requisito de entrega, não um detalhe de processo. Isso precisa entrar nos épicos.
- **Ambiguidades deixadas em aberto pela spec** (decisões do candidato, a serem resolvidas em arquitetura):
  - PostgreSQL vs SQL Server
  - xUnit vs NUnit
  - Gerenciador de estado do React
  - Jest vs Vitest
  - SQLite in-memory vs TestContainers para testes de integração
  - Layout/capacidade do ônibus (a spec não define número de assentos nem disposição do mapa)
  - `data de nascimento` do Passageiro consta na entidade mas **não** no formulário da Tela 3 nem no payload de `POST /reservas` — conflito a reconciliar
  - Status da Reserva: valores possíveis não enumerados pela spec
  - Autenticação: não mencionada em nenhum requisito — presumivelmente fora de escopo
