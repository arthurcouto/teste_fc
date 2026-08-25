---
type: requisito
id: RF-CON-003
title: Consultar o saldo consolidado de uma data
classe: funcional
servico: Consolidado diário
padrao_ears: event-driven
verificacao: teste-integracao, teste-de-carga
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md, arquitetura/vistas/vista-de-implantacao.md]
decisoes: [decisoes/0004-consolidado-como-projecao-materializada.md]
---

# RF-CON-003 — Consultar o saldo consolidado de uma data

## Declaração

**Quando** for solicitado o consolidado de uma data, o serviço de Consolidado diário **deve** devolver o total de créditos, o total de débitos, o saldo resultante e o instante da última atualização daquela data.

## Critérios de aceitação

- A consulta é atendida por leitura direta da apuração, sem agregar lançamentos em tempo de consulta.
- Uma data sem lançamentos apurados devolve saldo zero com os totais zerados, e não recurso não encontrado — a ausência de movimento é uma resposta válida.
- Numa data sem apuração, o instante da última atualização é ausente, e não uma data arbitrária. Ausente significa "nunca houve movimento neste dia"; qualquer valor preenchido afirmaria uma apuração que não ocorreu.
- A resposta expõe o instante da última atualização, permitindo ao consumidor avaliar a atualidade do dado.
- A consulta não depende do serviço de Lançamentos.

## Racional da leitura direta

Este é o requisito submetido ao pico de 50 requisições por segundo. Resolver a consulta por leitura de uma linha já apurada, em vez de agregar lançamentos a cada chamada, mantém o custo da consulta constante e independente do volume de lançamentos do dia.

## Verificação

Teste de integração cobrindo data com movimento e data sem movimento. Teste de carga sustentando a taxa e a margem de perda definidas em [RNF-002](../transversais/rnf-002-capacidade-de-consulta-do-consolidado.md).

## Rastreabilidade

- Vistas: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md), [Implantação](../../arquitetura/vistas/vista-de-implantacao.md)
- Decisão: [0004](../../decisoes/0004-consolidado-como-projecao-materializada.md)
- Requisito relacionado: [RNF-002](../transversais/rnf-002-capacidade-de-consulta-do-consolidado.md)
