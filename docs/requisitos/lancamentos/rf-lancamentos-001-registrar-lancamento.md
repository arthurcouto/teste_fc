---
type: requisito
id: RF-LAN-001
title: Registrar lançamento
classe: funcional
servico: Lançamentos
padrao_ears: event-driven
verificacao: teste-unitario, teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c3-componentes.md]
decisoes: [decisoes/0005-clean-architecture-e-ddd-tatico.md]
---

# RF-LAN-001 — Registrar lançamento

## Declaração

**Quando** o comerciante submeter um lançamento contendo tipo, valor, data de competência e descrição, o serviço de Lançamentos **deve** validar o lançamento, persisti-lo e devolver o identificador atribuído.

## Critérios de aceitação

- O tipo do lançamento é `credit` ou `debit`. Qualquer outro valor é rejeitado.
- O valor é estritamente maior que zero e possui no máximo duas casas decimais. O sinal é dado pelo tipo, nunca pelo valor.
- O valor é representado em decimal de precisão fixa, nunca em ponto flutuante binário, e a moeda é única e implícita.
- A data de competência é uma data sem horário, interpretada no fuso do comerciante.
- A data de competência não é posterior à data corrente **no fuso do comerciante**.
- A descrição tem no máximo 200 caracteres e é opcional.
- O identificador é atribuído pelo serviço e é único.
- O lançamento registrado é imutável: não há operação de alteração nem de exclusão.
- A resposta de sucesso devolve o lançamento persistido com seu identificador e a data de registro.

## Racional do fuso horário

A data de competência decide em qual dia o lançamento entra no saldo — ou seja, decide o produto do sistema. Um lançamento às 22h de um comerciante em horário de Brasília cai no dia seguinte se a data for derivada em UTC.

Por isso a data de competência é **data pura**, não instante, e é resolvida no fuso do comerciante antes de qualquer persistência. Instantes — a data de registro, o momento da apuração — são armazenados em UTC, porque servem a ordenação e diagnóstico, não a competência.

## Racional da representação monetária

Ponto flutuante binário não representa exatamente valores decimais como 0,10, e somas de milhares de lançamentos acumulam erro. Num sistema cujo produto é um saldo, isso é defeito de correção, não de precisão de exibição. O valor é decimal de precisão fixa da origem ao total apurado.

## Racional da imutabilidade

Um livro de lançamentos é um registro histórico. Alterar um lançamento já registrado invalidaria qualquer saldo apurado a partir dele e tornaria a apuração não reproduzível. A correção de um erro é feita por lançamento compensatório, tratado em [evolucao/roadmap.md](../../evolucao/roadmap.md).

## Verificação

Testes unitários sobre o agregado cobrindo cada invariante isoladamente, e teste de integração exercitando o endpoint com casos válidos e inválidos.

## Rastreabilidade

- Vista: [C3 — Componentes](../../arquitetura/vistas/c3-componentes.md)
- Decisões: [0001](../../decisoes/0001-decomposicao-em-dois-servicos.md), [0005](../../decisoes/0005-clean-architecture-e-ddd-tatico.md)
