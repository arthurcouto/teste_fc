---
type: requisito
id: RF-LAN-004
title: Disponibilizar lançamentos registrados para apuração
classe: funcional
servico: Lançamentos
padrao_ears: event-driven
verificacao: teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c2-conteineres.md, arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0001-decomposicao-em-dois-servicos.md, decisoes/0002-comunicacao-assincrona-por-fila.md, decisoes/0003-outbox-transacional-e-consumo-idempotente.md]
---

# RF-LAN-004 — Disponibilizar lançamentos registrados para apuração

## Declaração

**Quando** um lançamento for registrado com sucesso, o serviço de Lançamentos **deve** disponibilizá-lo de forma assíncrona para consumo pelo serviço de Consolidado diário, sem depender da disponibilidade deste.

## Critérios de aceitação

- A disponibilização ocorre somente após a confirmação da transação que persistiu o lançamento.
- Um lançamento registrado é disponibilizado ao menos uma vez, mesmo diante de falha temporária do mecanismo de transporte.
- **Enquanto** o serviço de Consolidado estiver indisponível, o registro de novos lançamentos permanece funcional e as mensagens pendentes são retidas.
- A mensagem carrega todos os dados necessários à apuração — identificador, tipo, valor, data de competência e instante de registro — de modo que o consumidor nunca precise consultar este serviço para completá-la.
- A mensagem carrega o identificador de correlação da requisição que originou o lançamento.
- A mensagem carrega a versão do contrato, permitindo evolução sem quebrar o consumidor.

O contrato completo está declarado na [vista de contêineres](../../arquitetura/vistas/c2-conteineres.md).
- O serviço de Lançamentos não realiza chamada síncrona ao serviço de Consolidado em nenhum fluxo.

## Verificação

Teste de integração que interrompe o transporte após a confirmação da transação e verifica que o lançamento é entregue no restabelecimento. Teste de arquitetura que proíbe referência do serviço de Lançamentos a qualquer cliente do Consolidado.

## Rastreabilidade

- Vistas: [C2 — Contêineres](../../arquitetura/vistas/c2-conteineres.md), [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
- Decisões: [0002](../../decisoes/0002-comunicacao-assincrona-por-fila.md), [0003](../../decisoes/0003-outbox-transacional-e-consumo-idempotente.md)
- Requisito relacionado: [RNF-001](../transversais/rnf-001-isolamento-de-falha-entre-servicos.md)
