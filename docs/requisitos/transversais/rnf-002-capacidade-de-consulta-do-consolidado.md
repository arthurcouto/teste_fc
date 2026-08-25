---
type: requisito
id: RNF-002
title: Capacidade de consulta do consolidado em dias de pico
classe: nao-funcional
caracteristica_qualidade: Eficiência de desempenho / Capacidade
origem: especificação da especificação
padrao_ears: state-driven
verificacao: teste-de-carga
status: aprovado
vistas: [arquitetura/vistas/c2-conteineres.md, arquitetura/vistas/vista-de-implantacao.md]
decisoes: [decisoes/0004-consolidado-como-projecao-materializada.md, decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md, decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md, decisoes/0008-terraform-com-stack-unico-e-toggles.md]
---

# RNF-002 — Capacidade de consulta do consolidado em dias de pico

## Origem

Texto da especificação: *"Em dias de picos, o serviço de consolidado diário recebe 50 requisições por segundo, com no máximo 5% de perda de requisições."*

## Interpretação adotada

A especificação define a carga e o teto de perda, e não define latência. Duas leituras seriam possíveis: tratar os 5% como orçamento a ser consumido, ou como teto que a arquitetura deve evitar tocar. Adotamos a segunda — 5% é o limite de aceitação do ensaio, não a meta de projeto.

A meta de latência é definida aqui por ser necessária para tornar o requisito verificável: sem ela, uma resposta correta em dez segundos satisfaria a taxa de perda e ainda assim falharia o propósito.

## Declaração

**Enquanto** o serviço de Consolidado diário estiver sob carga de 50 requisições por segundo sustentadas, ele **deve** responder com taxa de erro não superior a 5% das requisições.

## Critérios de aceitação

- A carga de referência é de 50 requisições por segundo sustentadas por 10 minutos sobre a consulta de saldo de uma data.
- A taxa de erro considera respostas de falha do serviço e requisições sem resposta dentro do tempo limite. Rejeições explícitas por controle de vazão contam como perda.
- A consulta é atendida por leitura da apuração já materializada, mantendo custo constante em relação ao volume de lançamentos do dia.
- O serviço escala horizontalmente por número de instâncias, sem estado local que impeça a adição de réplicas.
- A degradação sob sobrecarga é explícita e limitada: o controle de vazão na borda rejeita o excedente em vez de permitir a saturação do serviço.

## Metas e medição

| Indicador | Meta | Limite de aceitação |
|---|---|---|
| Taxa de erro a 50 req/s sustentadas | ≤ 1% | ≤ 5% (especificação) |
| Latência p95 da consulta de uma data | ≤ 200 ms | ≤ 500 ms |
| Latência p99 da consulta de uma data | ≤ 400 ms | ≤ 1 s |
| Tempo até estabilizar após dobrar a carga | ≤ 3 min | — |

Os valores de meta são de projeto; os de aceitação são o que o ensaio precisa demonstrar.

## Verificação

Ensaio de carga com rampa até 50 requisições por segundo, patamar de 10 minutos e medição de taxa de erro e de latência por percentil. Ensaio adicional a **200** requisições por segundo — o dobro do limite de vazão previsto, e não o limite exato — para observar o comportamento sob sobrecarga e confirmar que a rejeição é explícita. A razão de rodar acima do limite, e não sobre ele, está em [estratégia de testes](../../qualidade/estrategia-de-testes.md).

O ensaio de sobrecarga depende do controle de vazão do gateway, que não está provisionado. Enquanto não estiver, ele permanece especificado e não executado.

## Rastreabilidade

- Vistas: [C2 — Contêineres](../../arquitetura/vistas/c2-conteineres.md), [Implantação](../../arquitetura/vistas/vista-de-implantacao.md)
- Decisões: [0004](../../decisoes/0004-consolidado-como-projecao-materializada.md), [0007](../../decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md)
- Requisito relacionado: [RF-CON-003](../consolidado/rf-consolidado-003-consultar-saldo-de-uma-data.md)
