---
type: decisao
id: ADR-0001
title: Decompor o sistema em dois serviços independentes, um por capacidade de negócio
status: aceita
data: 2026-08-18
tags: [decomposicao, servicos, disponibilidade]
requisitos_afetados: [RNF-001, RF-LAN-004]
---

# ADR 0001 — Decompor o sistema em dois serviços independentes, um por capacidade de negócio

## Contexto

A especificação descreve duas capacidades — controle de lançamentos e consolidado diário — e impõe que a primeira permaneça disponível quando a segunda falhar.

Essa imposição é uma restrição de arquitetura, não uma preferência de estilo. Ela determina que as duas capacidades tenham modos de falha independentes, e o estilo de decomposição precisa ser escolhido em função disso.

## Alternativas consideradas

**Monólito único.** Menor superfície operacional, transação local entre as duas capacidades, sem consistência eventual. Falha o requisito de forma direta: um esgotamento de recursos causado pelo pico de leitura do consolidado degradaria o registro de lançamentos no mesmo processo. A independência seria nominal, não real.

**Monólito modular com módulos isolados no mesmo processo.** Preserva boa parte da simplicidade operacional e impõe fronteiras de código. Não resolve o requisito: processo compartilhado significa memória, escalonador e ciclo de vida compartilhados. Uma falha que derrube o processo derruba as duas capacidades, e escalar uma exige escalar a outra.

**Dois serviços independentes.** Cada capacidade com seu processo, sua base, sua política de escala e seu ciclo de implantação. Atende ao requisito por construção. Introduz consistência eventual entre o lançamento registrado e o saldo apurado, e uma travessia de rede que pode falhar.

**Decomposição mais granular.** Separar consulta de escrita, ou extrair a apuração do serviço que a serve. Multiplicaria unidades de implantação sem que nenhum requisito exigisse a separação adicional.

## Decisão

Decompor o sistema em dois serviços, alinhados às duas capacidades da especificação: Lançamentos e Consolidado diário.

Cada serviço tem processo próprio, base de dados própria, política de escala própria e ciclo de implantação próprio. Nenhum acessa a base do outro, e não há esquema compartilhado.

O consumidor da fila roda dentro do processo do serviço de Consolidado. Não há terceira unidade de implantação: quem apura o saldo é quem o serve.

## Consequências

O saldo consolidado passa a ser eventualmente consistente com os lançamentos. Um lançamento confirmado pode não estar refletido no saldo por um intervalo. Isso é aceitável para o domínio — um relatório de fluxo de caixa diário não exige leitura imediata da própria escrita — mas precisa ser explícito para quem consome a consulta, e por isso a resposta expõe o instante da última atualização.

Bases separadas impedem consultar lançamento e saldo em uma única transação. Nenhum requisito pede isso.

A duplicação da informação do lançamento entre as duas bases é intencional: é ela que permite que a consulta do consolidado não toque a base de lançamentos, mantendo os caminhos de leitura e de escrita independentes também na camada de dados.

Manter o consumidor no processo do serviço de leitura acopla a escala das duas responsabilidades. É desejável aqui, porque a idempotência da apuração torna seguro consumir em paralelo, e porque separá-los criaria uma unidade a mais para implantar, observar e escalar sem que nenhum requisito o exigisse.

## Requisitos afetados

- [RNF-001 — Isolamento de falha entre os serviços](../requisitos/transversais/rnf-001-isolamento-de-falha-entre-servicos.md)
- [RF-LAN-004 — Disponibilizar lançamentos para apuração](../requisitos/lancamentos/rf-lancamentos-004-disponibilizar-lancamentos-para-apuracao.md)
