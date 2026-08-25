---
type: plano
title: Plano de execução
description: Fases de trabalho, critérios de saída e estado de andamento.
status: ativo
tags: [plano]
---

# Plano de execução

Documento de condução do trabalho. O que ele **não** contém, de propósito: as decisões arquiteturais, que estão em [decisoes/](decisoes/index.md); a estrutura da documentação, que está em [index.md](index.md); e o critério de escopo, que está em [escopo e limites](escopo-e-limites.md).

# Escopo

| Exigência da especificação | Onde é atendida |
|---|---|
| Desenho da solução | `docs/arquitetura/` |
| Implementação em C# | `back/` — .NET 10 |
| Testes | `back/tests/` |
| Boas práticas | `back/` e `docs/decisoes/` |
| README de execução local | `README.md` na raiz |
| Repositório público no GitHub | pendente: **o repositório ainda está privado** |
| Documentação de projeto no repositório | `docs/` |

O front-end e a infraestrutura como código não são exigidos pela especificação e entram como exceções declaradas, registradas em [escopo e limites](escopo-e-limites.md).

# Estrutura do backend

Dois contextos delimitados, com as dependências apontando para dentro, conforme [ADR 0005](decisoes/0005-clean-architecture-e-ddd-tatico.md).

```
back/
├── CashFlow.slnx
├── Directory.Build.props           analisadores, aviso como erro, auditoria de dependências
├── Directory.Packages.props        versões de pacote centralizadas
├── global.json                     versão do SDK e executor de testes
├── src/
│   ├── CashFlow.Contracts/                    contrato do evento de integração
│   ├── Ledger/
│   │   ├── CashFlow.Ledger.Domain/            ✔
│   │   ├── CashFlow.Ledger.Application/       ✔
│   │   ├── CashFlow.Ledger.Infrastructure/    ✔
│   │   └── CashFlow.Ledger.Api/               ✔
│   └── Consolidation/
│       ├── CashFlow.Consolidation.Application/     ✔
│       ├── CashFlow.Consolidation.Infrastructure/  ✔
│       └── CashFlow.Consolidation.Api/             ✔
└── tests/
    ├── CashFlow.Ledger.UnitTests/        ✔
    ├── CashFlow.Consolidation.UnitTests/ ✔
    ├── CashFlow.ArchitectureTests/       ✔
    └── CashFlow.IntegrationTests/        ✔
```

Doze projetos ao fim; **os doze existem hoje**, marcados acima. O `docker-compose.yml` e a execução local entraram na fase 5. O Consolidado tem três camadas e não quatro, não há *shared kernel*, e o consumidor da fila roda dentro do contêiner do Consolidado — as três reduções estão justificadas em [escopo e limites](escopo-e-limites.md).

# Fases

## Fase 1 — Documentação de arquitetura — concluída

Catálogo de requisitos, descrição arquitetural, vistas, decisões, metas de qualidade, fluxos e roadmap.

## Fase 2 — Portão de persistência — concluída

Validação da decisão de persistência contra um cluster real, antes de qualquer código apoiado nela. Resultado e restrições descobertas em [ADR 0006](decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md). Infraestrutura mínima provisionada em `infra/`.

## Fase 3 — Domínio, aplicação e testes de regra — concluída

Solução e projetos, com as verificações de construção que [RNF-006](requisitos/transversais/rnf-006-integridade-da-cadeia-de-dependencias.md) alcança: versões centralizadas, auditoria de dependências incluindo transitivas e analisadores com aviso tratado como erro. O que aquele requisito pede e não foi feito está nomeado no próprio requisito.

Em seguida o agregado de Lançamento com suas invariantes, a apuração diária, os casos de uso e as portas. Testes unitários e de arquitetura.

**Critério de saída:** as regras de negócio passam em testes que rodam sem banco, sem fila e sem rede.

## Fase 4 — Persistência, mensageria e integração — concluída

Mapeamento objeto-relacional com modelo de persistência próprio, de modo que o domínio permaneça livre do mapeador. Migrações em SQL versionado, aplicadas por migrador próprio que aguarda a conclusão dos índices assíncronos. Registro de saída transacional com reivindicação atômica, publicador e consumidor em segundo plano, com retentativa de conflito transitório separada do desvio de mensagem defeituosa.

Seis restrições novas do motor foram descobertas e tratadas, registradas em [ADR 0006](decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md).

**Critério atingido:** a suíte passa, e seis dos seus testes de integração foram executados contra os dois agrupamentos reais, cobrindo idempotência sob entrega concorrente, ausência de atualização perdida na linha do dia, e reversão completa quando a transação falha.

**O que não foi feito nesta fase:** o ensaio de ponta a ponta com o Consolidado fora do ar dependia da camada de execução. O mecanismo já estava implementado e coberto por teste; o ensaio foi executado na fase 5, sobre o ambiente local, e está registrado ali.

## Fase 5 — Borda, execução local e front-end — concluída

Endpoints, OpenAPI, tratamento global de erro, verificações de saúde, observabilidade, `Dockerfile` por serviço, `docker-compose` e README: **concluídos**. Um comando sobe o ambiente completo, e o fluxo foi verificado de ponta a ponta.

O ensaio de indisponibilidade, herdado da fase 4, foi executado sobre esse ambiente e passou nas duas direções: com o Lançamentos fora do ar o Consolidado continua servindo saldo; com o Consolidado fora do ar o registro segue sendo aceito, e a apuração converge no restabelecimento sem perder lançamento.

Três defeitos apareceram só na execução real, nenhum deles visível ao build ou à suíte: a imagem de runtime sem banco de fusos, a análise estática em Release rejeitando chamadas de log diretas, e o identificador de correlação sendo apagado pelo tratamento de exceção justamente nas respostas de erro.

A autenticação foi implementada nas duas APIs como verificação redundante à do gateway, falhando fechada, com os três ensaios que o RNF-003 exige. A mudança de posição em relação ao desenho original está registrada como emenda na [ADR 0007](decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md).

As duas telas da interface foram feitas por último: HTML, CSS e JavaScript sem framework, sem dependência e sem etapa de construção, servidos por um nginx que também repassa as duas APIs, de modo que tudo seja mesma origem e não haja CORS. Entram no mesmo `docker compose up`.

**Critério de saída:** um comando sobe o ambiente completo numa máquina limpa — **atendido**.

## Fase 6 — Infraestrutura restante e fechamento — não iniciada

Camada de execução, borda, identidade e distribuição do front-end no Terraform já existente — hoje ele provisiona apenas os dois agrupamentos de persistência. Identidade de implantação por OIDC. Revisão de aderência e **tornar o repositório público**.

**Critério de saída:** `terraform plan` limpo e requisitos obrigatórios da especificação todos atendidos.
