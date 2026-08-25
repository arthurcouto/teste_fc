---
type: requisito
id: RF-LAN-003
title: Listar lançamentos por período
classe: funcional
servico: Lançamentos
padrao_ears: event-driven
verificacao: teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0005-clean-architecture-e-ddd-tatico.md]
---

# RF-LAN-003 — Listar lançamentos por período

## Declaração

**Quando** for solicitada a listagem de lançamentos para um intervalo de datas de competência, o serviço de Lançamentos **deve** devolver os lançamentos do intervalo, ordenados por data de competência e, dentro do mesmo dia, por data de registro.

## Critérios de aceitação

- O intervalo é fechado nos dois extremos e a data inicial não é posterior à final.
- O resultado é paginado, com tamanho de página padrão de 50 e máximo de 200 itens.
- A resposta informa o total de itens do intervalo, para permitir navegação.
- **Se** o intervalo for inválido, **então** a requisição é rejeitada com indicação do campo em erro.
- Um intervalo sem lançamentos devolve coleção vazia, não erro.

## Verificação

Teste de integração cobrindo intervalo com resultados, intervalo vazio, intervalo invertido e limites de paginação.

## Rastreabilidade

- Vista: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
