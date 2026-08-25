---
type: requisito
id: RNF-004
title: Recuperabilidade e convergência do consolidado
classe: nao-funcional
caracteristica_qualidade: Confiabilidade / Recuperabilidade
origem: derivado de RNF-001
padrao_ears: event-driven
verificacao: teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c2-conteineres.md]
decisoes: [decisoes/0002-comunicacao-assincrona-por-fila.md, decisoes/0003-outbox-transacional-e-consumo-idempotente.md, decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md]
---

# RNF-004 — Recuperabilidade e convergência do consolidado

## Origem

Consequência direta de [RNF-001](rnf-001-isolamento-de-falha-entre-servicos.md). Tolerar a queda do Consolidado só tem valor se, ao voltar, ele reconstituir o saldo correto. Sem este requisito, o isolamento de falha protegeria o Lançamentos ao custo de um saldo permanentemente errado.

## Declaração

**Quando** o serviço de Consolidado diário for restabelecido após indisponibilidade, ele **deve** incorporar todos os lançamentos registrados durante o período e convergir para o saldo correto.

## Critérios de aceitação

- Nenhum lançamento confirmado no serviço de Lançamentos deixa de ser incorporado ao consolidado.
- A ordem de chegada das mensagens não afeta o saldo final, dado que a apuração de cada dia é independente e a incorporação é comutativa.
- A reentrega de uma mensagem já processada não altera o saldo, conforme [RF-CON-002](../consolidado/rf-consolidado-002-ignorar-lancamento-ja-apurado.md).
- O consumidor distingue **falha da mensagem** de **falha de dependência**. Diante de indisponibilidade da persistência, ele suspende o consumo em vez de consumir e falhar, de modo que a indisponibilidade não consuma as tentativas das mensagens.
- Mensagens que falham de forma determinística são desviadas para tratamento de exceção após o número definido de tentativas, sem bloquear a fila.
- Uma mensagem desviada para tratamento de exceção pode ser reprocessada sem duplicar o efeito no saldo, porque a incorporação é idempotente.

## Metas e medição

| Indicador | Meta |
|---|---|
| Lançamentos perdidos após restabelecimento | zero |
| Tempo de convergência após indisponibilidade de 30 minutos sob carga nominal | ≤ 10 min |
| Retenção de mensagens não entregues | ≥ 14 dias |
| Tentativas antes do desvio para tratamento de exceção | 10, contadas apenas sobre falha determinística da mensagem |

## Lacunas conhecidas

Dois critérios que este requisito pediria não têm mecanismo, e é preferível declará-los a fingi-los:

- **Não há operação de reenvio** das mensagens desviadas de volta à fila principal. O reprocessamento seria seguro — a idempotência garante isso —, mas quem quiser fazê-lo hoje precisa mover a mensagem por fora do sistema.
- **O acúmulo pendente não é observável.** Nada no sistema publica a profundidade da fila nem a idade da mensagem mais antiga, e por isso não há como estimar o tempo restante até a convergência sem consultar o transporte à mão. A lacuna e a sua consequência estão em [RNF-005](rnf-005-analisabilidade-da-execucao.md).

A retenção de 14 dias vale para as duas filas, e ambas a declaram explicitamente no roteiro que as cria — o padrão do serviço é de quatro dias, insuficiente para a janela de indisponibilidade que este requisito prevê. Os valores estão em [metas e métricas](../../qualidade/metas-e-metricas.md).

## Verificação

Ensaio que interrompe o consumidor por período definido sob carga contínua de registro, restabelece o serviço e compara a soma dos lançamentos registrados com o saldo apurado, medindo o tempo até a igualdade.

## Rastreabilidade

- Vista: [C2 — Contêineres](../../arquitetura/vistas/c2-conteineres.md)
- Decisões: [0002](../../decisoes/0002-comunicacao-assincrona-por-fila.md), [0003](../../decisoes/0003-outbox-transacional-e-consumo-idempotente.md)
