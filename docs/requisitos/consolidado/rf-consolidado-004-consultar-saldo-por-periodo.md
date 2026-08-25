---
type: requisito
id: RF-CON-004
title: Consultar o saldo consolidado por período
classe: funcional
servico: Consolidado diário
padrao_ears: event-driven
verificacao: teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0004-consolidado-como-projecao-materializada.md]
---

# RF-CON-004 — Consultar o saldo consolidado por período

## Declaração

**Quando** for solicitado o consolidado de um intervalo de datas, o serviço de Consolidado diário **deve** devolver a série de saldos diários do intervalo.

## Critérios de aceitação

- O intervalo é fechado nos dois extremos e a data inicial não é posterior à final.
- O intervalo é limitado a 366 dias por requisição.
- Dias sem movimento aparecem na série com totais zerados, preservando a continuidade da série.
- **Se** o intervalo for inválido ou exceder o limite, **então** a requisição é rejeitada com indicação do motivo.

## Racional dos dias sem movimento

Uma série temporal com lacunas obriga cada consumidor a preencher os dias ausentes por conta própria, e cada um o fará de um jeito. Devolver a série contínua move essa responsabilidade para onde ela pode ser resolvida uma única vez.

## Verificação

Teste de integração cobrindo intervalo contínuo, intervalo com dias sem movimento, intervalo invertido e intervalo acima do limite.

## Rastreabilidade

- Vista: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
- Decisão: [0004](../../decisoes/0004-consolidado-como-projecao-materializada.md)
