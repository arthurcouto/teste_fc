---
type: requisito
id: RF-CON-002
title: Ignorar lançamento já apurado
classe: funcional
servico: Consolidado diário
padrao_ears: unwanted-behaviour
verificacao: teste-unitario, teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0003-outbox-transacional-e-consumo-idempotente.md]
---

# RF-CON-002 — Ignorar lançamento já apurado

## Declaração

**Se** um lançamento já incorporado ao consolidado for recebido novamente, **então** o serviço de Consolidado diário **deve** descartá-lo sem alterar o saldo apurado.

## Critérios de aceitação

- A identificação de reprocessamento usa o identificador do lançamento, atribuído pelo serviço de Lançamentos.
- O descarte é registrado para fins de diagnóstico e não é tratado como erro.
- A verificação de duplicidade e a atualização do saldo ocorrem na mesma transação, de modo que uma falha após a atualização não permita dupla contagem em nova tentativa.
- Processar a mesma mensagem N vezes produz o mesmo saldo que processá-la uma vez.

## Racional

O transporte entre os serviços garante entrega ao menos uma vez, não exatamente uma vez. Sem esta regra, uma reentrega — evento normal, não excepcional — corromperia o saldo de forma silenciosa.

## Verificação

Teste unitário aplicando o mesmo lançamento repetidamente à apuração. Teste de integração que reentrega a mesma mensagem e compara o saldo antes e depois.

## Rastreabilidade

- Vista: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
- Decisão: [0003](../../decisoes/0003-outbox-transacional-e-consumo-idempotente.md)
- Requisito relacionado: [RF-LAN-004](../lancamentos/rf-lancamentos-004-disponibilizar-lancamentos-para-apuracao.md)
