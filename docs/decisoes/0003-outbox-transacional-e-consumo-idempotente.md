---
type: decisao
id: ADR-0003
title: Publicar por registro de saída transacional e consumir de forma idempotente
status: aceita
data: 2026-08-18
tags: [integracao, consistencia, idempotencia]
requisitos_afetados: [RF-LAN-004, RF-CON-002, RNF-004]
---

# ADR 0003 — Publicar por registro de saída transacional e consumir de forma idempotente

## Contexto

Com a comunicação por fila ([ADR 0002](0002-comunicacao-assincrona-por-fila.md)), persistir o lançamento e publicá-lo são duas operações sobre sistemas distintos, sem transação que as abranja. Isso abre duas janelas de inconsistência em direções opostas.

Publicar depois de confirmar a transação: se a publicação falhar, o lançamento existe e nunca será apurado. O saldo diverge de forma permanente e **silenciosa** — não há erro a observar, porque a operação que o comerciante viu foi bem-sucedida.

Publicar antes de confirmar: se a transação falhar, foi publicado um lançamento que não existe, e o saldo passa a incluir valor sem lastro.

A entrega ao menos uma vez do transporte acrescenta um terceiro caso: a mesma mensagem chegar mais de uma vez, contando o mesmo lançamento duas vezes.

## Alternativas consideradas

**Publicar após a confirmação, com nova tentativa em memória.** Reduz a probabilidade da primeira janela sem eliminá-la: a nova tentativa se perde se o processo terminar. Mantém a falha silenciosa, que é o pior atributo do modo de falha.

**Transação distribuída entre a base e o transporte.** Eliminaria a janela. Exige coordenação com o serviço de mensageria, indisponível na maioria dos transportes gerenciados, e introduz um coordenador cuja indisponibilidade bloqueia a escrita — o oposto do que o requisito de isolamento pede.

**Registro de saída gravado na mesma transação, publicado depois por processo separado.** Fecha a janela: ou o lançamento e o registro de saída existem, ou nenhum existe. A publicação passa a ser reexecutável, e a falha vira atraso em vez de perda. Custa uma tabela, um índice e um processo de fundo, e não elimina a duplicidade — ela é tratada no consumo.

**Reconciliação periódica comparando as duas bases.** Detectaria divergências. Detecta depois de ocorridas, com atraso proporcional à periodicidade, e exige acesso cruzado entre bases que a decisão anterior separou.

## Decisão

Gravar o evento de integração em uma tabela de registros de saída, **na mesma transação** que persiste o lançamento. Um processo de fundo lê os registros pendentes, publica na fila e marca os publicados.

No consumo, verificar se o lançamento já foi apurado e, não tendo sido, incorporá-lo ao saldo — verificação e atualização **na mesma transação**. A mensagem só é confirmada após a confirmação dessa transação.

A identificação de reprocessamento usa o identificador do lançamento, atribuído no registro. O descarte de uma mensagem já processada é registrado para diagnóstico e não é tratado como erro.

## Consequências

Nenhum lançamento confirmado é perdido. É a garantia que sustenta o requisito de recuperabilidade, e a razão de a decisão existir apesar de a fila já cobrir a indisponibilidade do consumidor: a fila protege contra o consumidor cair, o registro de saída protege contra a publicação falhar.

Vale ser exato sobre o limite dessa garantia, porque a diferença entre "não é perdido" e "é sempre apurado" é onde moram os defeitos deste tipo de mecanismo. A entrega é tentada repetidamente, e o número de tentativas é **limitado** — sem limite, uma mensagem que o transporte rejeita de forma determinística monopolizaria o lote para sempre, e as mensagens atrás dela é que deixariam de ser apuradas. Ao cruzar o limite, o registro **permanece na base**, deixa de ser selecionado e é anunciado em nível crítico, nomeando o identificador.

Ou seja: o dado não se perde, mas a apuração daquele lançamento passa a exigir intervenção. É a mesma escolha que o lado do consumidor faz ao desviar para a fila de exceção, com a diferença de que aqui a mensagem desviada permanece na própria tabela em vez de mudar de fila. O limite é generoso de propósito — dezenas de tentativas —, para que uma indisponibilidade de transporte de alguns minutos não estacione mensagem íntegra.

Manter a verificação de duplicidade e a atualização do saldo na mesma transação é o que impede que uma falha entre as duas permita dupla contagem na nova tentativa. Separá-las reintroduziria o defeito que a idempotência deveria eliminar.

Confirmar a mensagem apenas após a transação torna a reentrega possível e a perda impossível. É a troca correta: reentrega é inofensiva sob consumo idempotente, perda não é recuperável.

O publicador roda em toda réplica do serviço, e o motor de persistência escolhido não oferece bloqueio pessimista para coordená-los. A coordenação é por reivindicação com atualização condicional, descrita na [vista de componentes](../arquitetura/vistas/c3-componentes.md). Ela torna a publicação duplicada excepcional em vez de sistemática, mas não a elimina — e não precisa eliminar, porque o consumo é idempotente. Reconhecer isso é o que dispensa a transação distribuída descartada acima.

O custo é uma tabela adicional com índice sobre os pendentes, um processo de fundo, e a latência entre a confirmação do lançamento e sua publicação — que é o intervalo de varredura, e é o principal componente do atraso da consistência eventual.

A tabela de lançamentos apurados cresce proporcionalmente ao volume de lançamentos. Ela guarda apenas o fato da incorporação, não o valor, para não criar segunda fonte de verdade do saldo. Sua política de expurgo está em [evolucao/roadmap.md](../evolucao/roadmap.md).

## Requisitos afetados

- [RF-LAN-004 — Disponibilizar lançamentos para apuração](../requisitos/lancamentos/rf-lancamentos-004-disponibilizar-lancamentos-para-apuracao.md)
- [RF-CON-002 — Ignorar lançamento já apurado](../requisitos/consolidado/rf-consolidado-002-ignorar-lancamento-ja-apurado.md)
- [RNF-004 — Recuperabilidade e convergência do consolidado](../requisitos/transversais/rnf-004-recuperabilidade-e-convergencia-do-consolidado.md)
