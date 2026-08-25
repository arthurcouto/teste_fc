---
type: requisito
id: RF-CON-001
title: Apurar o saldo diário consolidado
classe: funcional
servico: Consolidado diário
padrao_ears: event-driven
verificacao: teste-unitario, teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0004-consolidado-como-projecao-materializada.md, decisoes/0005-clean-architecture-e-ddd-tatico.md]
---

# RF-CON-001 — Apurar o saldo diário consolidado

## Declaração

**Quando** um lançamento registrado for recebido, o serviço de Consolidado diário **deve** incorporá-lo ao saldo consolidado da sua data de competência.

## Critérios de aceitação

- O saldo do dia é a soma dos créditos menos a soma dos débitos com aquela data de competência.
- A apuração mantém, por dia, o total de créditos, o total de débitos, o saldo resultante e a quantidade de lançamentos incorporados.
- Um lançamento com data de competência ainda sem apuração cria a apuração daquele dia.
- A apuração registra o instante da última atualização.
- A apuração de um dia não depende da apuração de nenhum outro dia.
- **Se** o lançamento recebido for inválido segundo o contrato de integração, **então** ele é encaminhado para tratamento de exceção sem interromper o processamento dos demais.

## Racional da independência entre dias

Manter cada dia autocontido, em vez de acumular saldo corrente, permite que uma mensagem atrasada seja incorporada ao dia a que pertence sem recalcular a série. Também torna a apuração reconstruível dia a dia a partir dos lançamentos.

## Verificação

Testes unitários da regra de apuração cobrindo apenas créditos, apenas débitos, mistura, e o dia sem lançamentos. Teste de integração com mensagens fora de ordem.

## Rastreabilidade

- Vista: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
- Decisão: [0004](../../decisoes/0004-consolidado-como-projecao-materializada.md)
