---
type: decisao
id: ADR-0004
title: Manter o consolidado como projeção materializada por data de competência
status: aceita
data: 2026-08-18
tags: [modelo-de-leitura, desempenho, projecao]
requisitos_afetados: [RF-CON-001, RF-CON-003, RF-CON-004, RNF-002]
---

# ADR 0004 — Manter o consolidado como projeção materializada por data de competência

## Contexto

A consulta do saldo consolidado é a operação submetida ao pico de 50 requisições por segundo. Como esse saldo é obtido determina se a capacidade exigida é uma propriedade do desenho ou algo a ser perseguido depois por otimização.

## Alternativas consideradas

**Agregar os lançamentos a cada consulta.** Não duplica informação e não admite divergência entre o saldo e sua origem. O custo da consulta cresce com o volume de lançamentos do dia, o que torna o desempenho dependente do movimento do comerciante — exatamente a variável que aumenta nos dias de pico. Exigiria ainda alcançar a base do outro serviço, contrariando [ADR 0001](0001-decomposicao-em-dois-servicos.md).

**Agregar sob demanda, com resultado em cache.** Mantém o custo baixo no caso comum. Desloca o problema para a invalidação: um lançamento novo invalida o dia, e um dia muito movimentado é justamente o que mais invalida e mais é consultado. A primeira consulta após cada lançamento paga o custo integral.

**Registro contábil com saldo corrente acumulado.** Permite obter o saldo de qualquer data por diferença. Torna cada dia dependente da cadeia anterior: uma mensagem atrasada obriga a recalcular a série a partir dali, e a apuração deixa de ser independente entre dias.

**Projeção materializada por dia, atualizada na chegada do lançamento.** A consulta lê uma linha já pronta, com custo constante e independente do volume. Introduz duplicação da informação e consistência eventual, ambas já assumidas em [ADR 0001](0001-decomposicao-em-dois-servicos.md).

## Decisão

Manter uma apuração por data de competência, atualizada no momento em que o lançamento é recebido, contendo o total de créditos, o total de débitos, a quantidade de lançamentos incorporados e o instante da última atualização.

O **saldo não é armazenado**: ele é a diferença entre os dois totais, calculada na leitura e devolvida na resposta. Guardá-lo criaria uma terceira coluna que precisa concordar com as outras duas em toda atualização, e portanto um jeito novo de o consolidado divergir de si mesmo — o oposto do que esta decisão existe para dar.

A apuração de cada dia é independente das demais. A consulta é leitura direta, sem agregação em tempo de consulta.

**Não há camada de cache, e essa é a decisão — não uma omissão.** A especificação sugere considerar estratégias de cache para sustentar o pico de leitura. Cache existe para tornar barata uma leitura cara; aqui a leitura já é uma busca por chave primária numa linha pequena, e antepor um cache acrescentaria um estado a invalidar sem tornar nada mais barato. Pior: o dia mais consultado é o dia corrente, que é justamente o mais invalidado, de modo que o cache erraria exatamente onde precisaria acertar. A projeção materializada **é** a estratégia de cache deste sistema, com a diferença de que ela é invalidada por construção, na própria escrita, e não por política.

Há um caso em que o cache seria gratuito e vale registrar: **a apuração de uma data passada é imutável.** Uma vez encerrado o dia, nenhum lançamento novo pode alterá-lo, porque a competência não é futura. Isso torna a consulta de datas anteriores candidata natural a `Cache-Control` com validade longa e `ETag` derivado do instante da última atualização, empurrando a leitura repetida para o cliente e para a borda, sem invalidação a gerir. Não está implementado, e a razão é honesta: o volume previsto não exige. Fica registrado como o primeiro movimento a fazer se a carga de leitura crescer, antes de qualquer réplica adicional.

Uma data sem lançamentos devolve saldo zero com totais zerados, e não recurso não encontrado — a ausência de movimento é uma resposta válida sobre o dia, não uma falha de localização. Na consulta por período, dias sem movimento aparecem na série com totais zerados.

## Consequências

O custo da consulta passa a ser constante em relação ao volume de lançamentos, o que torna previsível o comportamento sob o pico: escalar a leitura é adicionar réplicas, não otimizar consulta. É o que sustenta a meta do requisito de capacidade.

A independência entre dias permite que uma mensagem atrasada seja incorporada ao dia a que pertence sem recalcular a série, e é o que torna a incorporação comutativa — a ordem de chegada não afeta o resultado. Sem ela, a recuperabilidade após indisponibilidade exigiria reprocessamento ordenado.

A projeção é derivada, e portanto reconstruível: dado o histórico de lançamentos, ela pode ser recalculada dia a dia. Isso limita o dano de um defeito na apuração, que deixa de ser corrupção permanente e passa a ser reprocessamento.

Em contrapartida, o saldo pode divergir dos lançamentos por um intervalo, e existe a possibilidade de divergência permanente por defeito. A primeira é mitigada expondo o instante da última atualização; a segunda, pela reconstrutibilidade.

Devolver a série contínua nas consultas por período move para o serviço a responsabilidade de preencher dias sem movimento. Feita uma vez aqui, ela não precisa ser refeita — de formas divergentes — por cada consumidor.

## Requisitos afetados

- [RF-CON-001 — Apurar o saldo diário consolidado](../requisitos/consolidado/rf-consolidado-001-apurar-saldo-diario.md)
- [RF-CON-003 — Consultar o saldo de uma data](../requisitos/consolidado/rf-consolidado-003-consultar-saldo-de-uma-data.md)
- [RF-CON-004 — Consultar o saldo por período](../requisitos/consolidado/rf-consolidado-004-consultar-saldo-por-periodo.md)
- [RNF-002 — Capacidade de consulta em dias de pico](../requisitos/transversais/rnf-002-capacidade-de-consulta-do-consolidado.md)
