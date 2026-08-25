---
type: requisito
id: RF-LAN-002
title: Consultar lançamento por identificador
classe: funcional
servico: Lançamentos
padrao_ears: event-driven
verificacao: teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0005-clean-architecture-e-ddd-tatico.md]
---

# RF-LAN-002 — Consultar lançamento por identificador

## Declaração

**Quando** for solicitada a consulta de um lançamento por seu identificador, o serviço de Lançamentos **deve** devolver o lançamento correspondente.

## Critérios de aceitação

- A consulta devolve tipo, valor, data de competência, descrição, identificador e data de registro.
- **Se** não existir lançamento com o identificador informado, **então** a consulta resulta em recurso não encontrado.
- A consulta não depende do serviço de Consolidado.

## Verificação

Teste de integração cobrindo lançamento existente e identificador inexistente.

## Rastreabilidade

- Vista: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
- Decisão: [0005](../../decisoes/0005-clean-architecture-e-ddd-tatico.md)
- Requisito relacionado: [RNF-001](../transversais/rnf-001-isolamento-de-falha-entre-servicos.md)
