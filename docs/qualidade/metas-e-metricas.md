---
type: qualidade
title: Metas de qualidade e métricas
description: Objetivos de nível de serviço derivados dos requisitos não funcionais, com o método de medição de cada um.
status: ativo
norma: ISO/IEC 25010
---

# Metas de qualidade e métricas

Este documento converte os requisitos não funcionais em objetivos mensuráveis. Uma meta sem método de medição não é meta — é intenção — e por isso cada linha declara como o número é obtido.

## Distinção entre meta e limite de aceitação

A especificação define um teto de perda de 5%. Duas leituras seriam possíveis: tratá-lo como orçamento a consumir, ou como limite que a arquitetura deve evitar tocar.

Adotamos a segunda. As **metas** abaixo são objetivos de projeto; os **limites de aceitação** são o que o ensaio precisa demonstrar para que o requisito seja considerado atendido. A distância entre os dois é a margem que absorve variação de ambiente.

## Objetivos de nível de serviço

### Disponibilidade

| Objetivo | Meta | Limite de aceitação | Origem |
|---|---|---|---|
| Sucesso do registro de lançamentos | 99,9% | 99,5% | RNF-001 |
| Sucesso da consulta do consolidado | 99,5% | 99,0% | RNF-002 |
| Sucesso do registro com o Consolidado fora do ar | igual à linha de base | dentro do IC de 95% da linha de base | RNF-001 |

As duas primeiras linhas **não derivam da especificação nem de uma composição medida**, e por isso são metas provisórias: o desenho é de região única, com o gateway de API reconhecido em [ADR 0007](../decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md) como ponto único de falha, e não há objetivo de recuperação declarado. Um número de disponibilidade só passa a ser meta quando composto a partir da disponibilidade publicada de cada componente do caminho, ou medido numa janela real — o que ainda não aconteceu. Até lá valem como referência, e é assim que devem ser lidas.

A terceira linha é diferente: ela expressa o requisito de isolamento da especificação. O que ela mede não é o valor absoluto, e sim a ausência de degradação atribuível à queda do outro serviço: se o número sair da banda da linha de base quando o Consolidado cai, o requisito não foi atendido.

### Desempenho

| Objetivo | Meta | Limite de aceitação | Origem |
|---|---|---|---|
| Latência p95 da consulta de uma data, a 50 req/s | 200 ms | 500 ms | RNF-002 |
| Latência p99 da consulta de uma data, a 50 req/s | 400 ms | 1 s | RNF-002 |
| Latência p95 do registro de lançamento | 300 ms | 800 ms | RNF-001 |
| Variação da latência p95 do registro com o Consolidado fora do ar | 0% | ≤ 10% | RNF-001 |
| Taxa de erro a 50 req/s sustentadas por 10 min | ≤ 1% | ≤ 5% | RNF-002 |

As metas de latência não constam da especificação. São interpretação declarada, e a razão de existirem é simples: sem elas, uma resposta correta em dez segundos satisfaria o teto de perda de 5% e ainda assim falharia o propósito do requisito.

### Consistência e recuperação

| Objetivo | Meta | Limite de aceitação | Origem |
|---|---|---|---|
| Lançamentos confirmados não apurados, em regime normal | 0 | 0 | RNF-004 |
| Atraso da consistência do saldo, em regime normal (p95) | ≤ 5 s | ≤ 30 s | RNF-004 |
| Convergência após 30 min de indisponibilidade do Consolidado | ≤ 10 min | ≤ 30 min | RNF-004 |
| Lançamentos perdidos após restabelecimento | 0 | 0 | RNF-004 |

A primeira e a última linha não admitem tolerância. Perda de requisição de leitura é recuperável por nova consulta e cabe no teto de 5%; perda de evento de escrita é irrecuperável e produz divergência permanente. A fronteira entre as duas é o que [ADR 0003](../decisoes/0003-outbox-transacional-e-consumo-idempotente.md) existe para marcar.

## Orçamento de erro

O limite de 5% de perda sobre a consulta do consolidado corresponde, à taxa de pico, a 2,5 requisições por segundo. Em uma janela de pico de uma hora, são 9.000 requisições.

O orçamento é consumido por qualquer resposta de falha e por qualquer requisição sem resposta dentro do tempo limite. **Rejeições explícitas por controle de vazão contam como perda**: a decisão de rejeitar em vez de saturar melhora o diagnóstico, mas não isenta as rejeitadas da contabilidade.

Consumo do orçamento acima de metade em uma janela de pico indica capacidade provisionada insuficiente, e a resposta é ajustar a política de escala, não ampliar o orçamento.

## Métricas e sua origem

| Métrica | Onde é obtida | Objetivo que sustenta |
|---|---|---|
| Taxa de erro por operação | Gateway de API | Disponibilidade, orçamento de erro |
| Latência por percentil, por operação | Gateway e balanceador interno | Desempenho |
| Rejeições por controle de vazão | Gateway | Orçamento de erro |
| Profundidade da fila principal | Transporte de mensagens | Atraso de consistência, convergência |
| Idade da mensagem mais antiga na fila | Transporte de mensagens | Convergência |
| Quantidade na fila de tratamento de exceção | Transporte de mensagens | Perda de eventos |
| Registros de saída pendentes há mais de um minuto | Base de lançamentos | Falha do publicador |
| Tarefas em execução por serviço | Orquestrador de contêineres | Efetividade da política de escala |

**Nenhuma destas métricas é produzida hoje.** Não há instrumentação no código — nem medidor próprio, nem instrumentação padronizada, nem rota que exponha métrica — e as origens da coluna do meio são recursos gerenciados que o Terraform não provisiona. A tabela declara de onde cada número deve vir quando existir; enquanto não existir, os objetivos desta página são objetivos, e não medições.

A profundidade da fila e a idade da mensagem mais antiga medem coisas diferentes: a primeira mostra volume acumulado, a segunda mostra se o consumo está progredindo. Uma fila com volume estável e idade crescente indica consumo parado, e só a segunda métrica revela isso.

## Retenção e crescimento

Valores que precisam ser fixados no provisionamento, e que sem declaração explícita nascem com padrões inadequados. A coluna de estado distingue o que já está fixado do que continua com o padrão — e "fixado" quer dizer fixado no único lugar onde as filas existem hoje, o roteiro de criação do ambiente local, já que nenhuma fila está provisionada em nuvem:

| Item | Valor | Estado | Por quê |
|---|---|---|---|
| Retenção da fila principal | 14 dias | fixado | Cobre a janela de indisponibilidade prevista em RNF-004 com folga |
| Retenção da fila de exceção | 14 dias | fixado | O padrão do serviço é de quatro dias, abaixo do que RNF-004 exige |
| Tentativas antes do desvio | 10 | fixado | Ver a distinção entre falha transitória e determinística abaixo |
| Retenção dos registros de execução | 7 dias | **pendente** — depende da camada de observabilidade, que não foi provisionada | O padrão é não expirar, e o custo cresce indefinidamente sem que ninguém note |
| Expurgo dos registros de saída publicados | após 7 dias | **pendente** — não há rotina nem recurso que o execute | O índice sobre os pendentes é comum, não parcial, e cobre também as linhas já publicadas |

### Falha transitória não pode consumir tentativa

O motor de persistência sinaliza conflito de concorrência no *commit*, e esse conflito é **transitório** — é mais provável justamente quando há mais consumidores. Contá-lo como tentativa de entrega faria mensagens legítimas irem para a fila de exceção sob carga, e o alarme sem limiar passaria a disparar por ruído até ser ignorado.

Conflito de concorrência e esgotamento de tempo são refeitos no próprio processo, com espera crescente e sem devolver a mensagem à fila — é assim que o conflito deixa de custar tentativa de entrega. Esgotadas as tentativas em processo, aí sim a mensagem volta à fila com espera, e essa devolução custa a tentativa que o recebimento já havia contado.

## Recuperação

O motor de persistência é de região única e não há objetivo de recuperação declarado. Isso é lacuna conhecida, não decisão: um sistema de fluxo de caixa em produção precisaria de objetivo de ponto e de tempo de recuperação, e de um mecanismo que os realize. Está em [roadmap](../evolucao/roadmap.md) com a condição de entrada.

## Alarmes

| Condição | O que indica |
|---|---|
| Fila de tratamento de exceção com qualquer mensagem | Mensagem defeituosa recorrente ou defeito no consumidor |
| Idade da mensagem mais antiga acima de 5 minutos | Consumo parado ou insuficiente |
| Registros de saída pendentes há mais de 5 minutos | Publicador parado |
| Taxa de erro acima de 5% por 5 minutos | Limite de aceitação da especificação ultrapassado |
| Prontidão falhando em todas as tarefas de um serviço | Indisponibilidade de dependência |

O alarme sobre a fila de exceção dispara na primeira mensagem, sem limiar. Uma mensagem que esgotou as dez tentativas representa um lançamento que não será apurado sem intervenção, e o objetivo correspondente é zero.

Nenhum destes alarmes existe. Eles dependem das métricas da tabela acima, e nenhuma delas é publicada hoje. A lista é o que precisa ser alarmado, não o que está alarmado.

## Verificação

Os objetivos são verificados pelos ensaios descritos em [estratégia de testes](estrategia-de-testes.md). Cada objetivo desta página tem ali o ensaio que o demonstra; um objetivo sem ensaio correspondente é uma lacuna, não uma meta aspiracional.
