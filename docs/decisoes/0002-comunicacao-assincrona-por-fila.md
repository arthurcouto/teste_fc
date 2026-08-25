---
type: decisao
id: ADR-0002
title: Comunicar os serviços exclusivamente por fila de mensagens
status: aceita
data: 2026-08-18
tags: [integracao, mensageria, disponibilidade]
requisitos_afetados: [RNF-001, RNF-004, RNF-005, RF-LAN-004]
---

# ADR 0002 — Comunicar os serviços exclusivamente por fila de mensagens

## Contexto

Decidida a separação em dois serviços ([ADR 0001](0001-decomposicao-em-dois-servicos.md)), resta definir como o Consolidado toma conhecimento dos lançamentos registrados. A escolha do mecanismo determina se o isolamento de falha se sustenta ou se é desfeito na primeira integração.

## Alternativas consideradas

**Chamada síncrona do Lançamentos para o Consolidado.** Simples e imediatamente consistente. Desfaz o requisito: a disponibilidade do registro passa a depender da disponibilidade da apuração. Mitigar com disjuntor e tempo limite reduz o dano, mas não elimina a dependência — apenas transforma indisponibilidade em latência adicional e falha parcial.

**Consulta periódica do Consolidado à base de lançamentos.** Elimina a dependência na direção crítica. Cria acoplamento ao esquema interno do outro serviço, que passa a não poder evoluir livremente, e impõe compromisso entre frequência de consulta e carga sobre a base de escrita.

**Fila de mensagens.** O produtor publica sem conhecer o consumidor; o consumidor processa sem que o produtor espere. A fila retém as mensagens durante a indisponibilidade e as entrega no restabelecimento. Introduz entrega ao menos uma vez, e portanto a necessidade de consumo idempotente.

**Barramento de eventos com múltiplos assinantes.** Ofereceria extensibilidade para consumidores futuros. Não há segundo consumidor, e a estrutura adicional não seria exercitada por nenhum requisito.

## Decisão

Comunicar os dois serviços exclusivamente por uma fila de mensagens, com fila de tratamento de exceção associada.

Não existe chamada síncrona entre os serviços, em nenhuma direção e em nenhum fluxo. A ausência dessa relação é verificada por teste de arquitetura, que falha se o serviço de Lançamentos passar a referenciar qualquer cliente do Consolidado.

A mensagem é autossuficiente: carrega todos os dados que a apuração precisa, mais o identificador do lançamento, que serve de chave de idempotência, e o identificador de correlação da requisição de origem. O contrato completo está na [vista de contêineres](../arquitetura/vistas/c2-conteineres.md).

A autossuficiência não é detalhe de formato. Se o consumidor precisasse consultar o produtor para completar a mensagem, haveria chamada síncrona entre os serviços e a decisão anterior estaria desfeita.

Mensagens que falham dez vezes são desviadas para a fila de tratamento de exceção, de modo que uma mensagem defeituosa não bloqueie o processamento das demais.

## Consequências

A queda do Consolidado deixa de afetar o Lançamentos, e as mensagens produzidas durante a indisponibilidade são entregues no restabelecimento. É o que realiza o requisito de isolamento e o de recuperabilidade.

A entrega é ao menos uma vez, não exatamente uma vez. Reprocessamento é comportamento normal do transporte, não excepcional, e por isso o consumo precisa ser idempotente — tratado em [ADR 0003](0003-outbox-transacional-e-consumo-idempotente.md). Sem essa contrapartida, a decisão de usar fila trocaria um modo de falha por outro.

Acompanhar o sistema passa a exigir observar a profundidade da fila e a quantidade de mensagens desviadas, sinais que não existiriam em uma integração síncrona. É a origem do requisito de analisabilidade.

Não haver barramento significa que um segundo consumidor exigirá revisitar esta decisão. O ponto de extensão é conhecido e está registrado em [evolucao/roadmap.md](../evolucao/roadmap.md).

## Requisitos afetados

- [RNF-001 — Isolamento de falha entre os serviços](../requisitos/transversais/rnf-001-isolamento-de-falha-entre-servicos.md)
- [RNF-004 — Recuperabilidade e convergência do consolidado](../requisitos/transversais/rnf-004-recuperabilidade-e-convergencia-do-consolidado.md)
- [RNF-005 — Analisabilidade da execução](../requisitos/transversais/rnf-005-analisabilidade-da-execucao.md)
- [RF-LAN-004 — Disponibilizar lançamentos para apuração](../requisitos/lancamentos/rf-lancamentos-004-disponibilizar-lancamentos-para-apuracao.md)
