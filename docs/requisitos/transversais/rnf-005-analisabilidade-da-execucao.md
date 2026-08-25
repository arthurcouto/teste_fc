---
type: requisito
id: RNF-005
title: Analisabilidade da execução
classe: nao-funcional
caracteristica_qualidade: Manutenibilidade / Analisabilidade
origem: derivado de RNF-002 e RNF-004
padrao_ears: ubiquitous
verificacao: inspecao, teste-integracao
status: aprovado
vistas: [arquitetura/vistas/c2-conteineres.md]
decisoes: [decisoes/0002-comunicacao-assincrona-por-fila.md, decisoes/0005-clean-architecture-e-ddd-tatico.md]
---

# RNF-005 — Analisabilidade da execução

## Origem

Pré-condição de [RNF-002](rnf-002-capacidade-de-consulta-do-consolidado.md) e [RNF-004](rnf-004-recuperabilidade-e-convergencia-do-consolidado.md): as metas dos dois só são requisitos se forem mensuráveis em execução. Este requisito existe para sustentar aquelas medições, e não como observabilidade genérica.

## Declaração

O sistema **deve** permitir determinar, a partir dos seus registros de execução, o resultado de uma requisição, o caminho que ela percorreu entre os dois serviços e o estado do acúmulo de mensagens pendentes.

## Critérios de aceitação

- Cada requisição recebe um identificador de correlação na borda, ou reaproveita o informado pelo cliente, e o devolve na resposta.
- O identificador de correlação acompanha a mensagem publicada e aparece nos registros do consumidor, ligando o registro do lançamento à sua apuração.
- Os registros de execução são estruturados, permitindo consulta por campo sem análise de texto livre.
- Cada serviço expõe verificação de vivacidade, que não depende de recurso externo, e verificação de prontidão, que verifica a persistência.

## O que este requisito não alcança hoje

Os três primeiros critérios são atendidos: o identificador de correlação é atribuído na borda do serviço, devolvido na resposta, viaja no evento e aparece nos registros do consumidor; os registros são estruturados.

O quarto é atendido pela metade: a prontidão verifica a persistência com uma consulta trivial, e **não** verifica o transporte de mensagens. É deliberado — a fila indisponível não impede a consulta do saldo de responder, e retirar a tarefa do encaminhamento por causa dela trocaria atraso de apuração por indisponibilidade de leitura.

Além disso, **não há métrica alguma**. Nenhum medidor, nenhuma instrumentação padronizada, nenhuma rota que exponha número. Daí decorrem duas lacunas concretas:

- a quantidade de mensagens pendentes e a quantidade desviadas para tratamento de exceção **não são observáveis**, o que retira de [RNF-004](rnf-004-recuperabilidade-e-convergencia-do-consolidado.md) a estimativa do tempo até a convergência;
- taxa de erro e latência por percentil **exigiriam instrumentação que não existe**, e por isso as metas de [metas e métricas](../../qualidade/metas-e-metricas.md) permanecem objetivos declarados, não números medidos.

O diagnóstico que o sistema entrega hoje é o rastro por identificador de correlação, e é só ele.

## Racional da separação entre vivacidade e prontidão

Uma verificação única que consulta a persistência faria o orquestrador reiniciar instâncias saudáveis durante uma indisponibilidade do banco, transformando uma falha de dependência em uma falha de disponibilidade. A vivacidade responde "o processo está vivo"; a prontidão responde "posso receber tráfego".

## Verificação

Teste de integração que segue um identificador de correlação do registro do lançamento até a apuração do saldo. Inspeção das verificações de saúde.

## Rastreabilidade

- Vista: [C2 — Contêineres](../../arquitetura/vistas/c2-conteineres.md)
- Requisitos relacionados: [RNF-002](rnf-002-capacidade-de-consulta-do-consolidado.md), [RNF-004](rnf-004-recuperabilidade-e-convergencia-do-consolidado.md)
