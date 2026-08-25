---
type: descricao-arquitetural
title: Descrição arquitetural do sistema de fluxo de caixa diário
description: Stakeholders, preocupações, pontos de vista, vistas, correspondências e glossário do domínio.
status: ativo
norma: ISO/IEC/IEEE 42010
---

# Descrição arquitetural

## Identificação

| Campo | Valor |
|---|---|
| Sistema de interesse | Controle de fluxo de caixa diário de um comerciante |
| Escopo | Registro de lançamentos e apuração do saldo diário consolidado |
| Convenção adotada | ISO/IEC/IEEE 42010, com as vistas expressas no modelo C4 |
| Documentos correlatos | [Plano de execução](../plano-de-execucao.md), [Escopo e limites](../escopo-e-limites.md), [Catálogo de requisitos](../requisitos/index.md), [Decisões](../decisoes/) |

Esta descrição existe para tornar a arquitetura verificável: cada preocupação declarada é endereçada por ao menos uma vista, e cada decisão relevante tem rationale registrado em documento próprio.

## Stakeholders

| Stakeholder | Papel | O que espera do sistema |
|---|---|---|
| Comerciante | Usuário e proprietário | Registrar entradas e saídas com confiança e consultar o saldo do dia |
| Time de desenvolvimento | Quem constrói e evolui o software | Alterar um serviço sem precisar entender o outro por inteiro |
| Responsável pela operação | Quem mantém o sistema em execução | Diagnosticar falhas e absorver variação de carga |

Os dois últimos são papéis **prospectivos**: o sistema ainda não tem time constituído nem operação em curso. Estão declarados porque a norma trata desenvolvedores e operadores como stakeholders do sistema, e porque as preocupações deles geraram requisitos concretos — não como suposição de uma estrutura organizacional que não existe.

O comerciante é o único stakeholder de negócio. Essa singularidade é determinante: ela retira do escopo particionamento por cliente, hierarquia de permissões e qualquer mecanismo cuja razão de existir seja a pluralidade de usuários.

## Preocupações

| ID | Preocupação | Stakeholders | Requisitos que a expressam |
|---|---|---|---|
| P1 | O saldo apurado corresponde aos lançamentos registrados | Comerciante | RF-CON-001, RF-CON-002, RNF-004 |
| P2 | O registro de lançamentos continua funcionando quando a apuração falha | Comerciante, Operação | RNF-001, RF-LAN-004 |
| P3 | A consulta do consolidado absorve o pico de dias de movimento | Comerciante, Operação | RNF-002, RF-CON-003 |
| P4 | Os dois serviços evoluem e são implantados de forma independente | Desenvolvimento | RNF-001 |
| P5 | O acesso é autenticado e os dados protegidos em trânsito e em repouso | Comerciante | RNF-003 |
| P6 | O comportamento em execução é diagnosticável | Operação | RNF-005 |
| P7 | O custo de entender e alterar o sistema é proporcional ao seu tamanho | Desenvolvimento, Operação | [Escopo e limites](../escopo-e-limites.md), [ADR 0005](../decisoes/0005-clean-architecture-e-ddd-tatico.md) |

P7 não deriva de requisito. Está declarada porque governa as decisões de omissão: é a preocupação que o [documento de escopo e limites](../escopo-e-limites.md) endereça, e é o contrapeso ao acúmulo de mecanismo que nenhum requisito exige.

## Pontos de vista

Cada ponto de vista declara as preocupações que enquadra e a notação que emprega. As vistas de contexto, contêineres e componentes seguem os três primeiros níveis do modelo C4; a de implantação é a projeção da vista de contêineres sobre a infraestrutura.

| Ponto de vista | Preocupações enquadradas | Notação | Vista |
|---|---|---|---|
| Contexto | P2, P5 | C4 nível 1, em Mermaid | [c1-contexto.md](vistas/c1-contexto.md) |
| Contêineres | P1, P2, P3, P4, P6 | C4 nível 2, em Mermaid | [c2-conteineres.md](vistas/c2-conteineres.md) |
| Componentes | P1, P7 | C4 nível 3, em Mermaid | [c3-componentes.md](vistas/c3-componentes.md) |
| Implantação | P2, P3, P5, P6 | Diagrama de implantação, em Mermaid | [vista-de-implantacao.md](vistas/vista-de-implantacao.md) |

O quarto nível do C4, de código, não é produzido. Ele seria gerado a partir do código-fonte e envelheceria a cada alteração; a estrutura interna é comunicada pela organização das camadas e verificada por testes de arquitetura, que não envelhecem em silêncio.

Não há vistas separadas de dados e de segurança. O modelo de dados é apresentado na vista de componentes, onde os agregados vivem, e a segurança na vista de implantação, onde os controles são efetivamente aplicados. Uma vista precisa de conteúdo suficiente para justificar a leitura autônoma; nenhuma das duas teria.

## Correspondência entre preocupações e vistas

Regra de correspondência: toda preocupação deve ser endereçada por ao menos uma vista. A tabela é o artefato de verificação de completude desta descrição.

| Preocupação | Contexto | Contêineres | Componentes | Implantação |
|---|---|---|---|---|
| P1 — Correção do saldo | | ● | ● | |
| P2 — Disponibilidade do registro | ● | ● | | ● |
| P3 — Capacidade de leitura | | ● | | ● |
| P4 — Independência de evolução | | ● | | |
| P5 — Segurança | ● | | | ● |
| P6 — Diagnóstico | | ● | | ● |
| P7 — Custo cognitivo | | | ● | |

## Correspondência entre requisitos, vistas e decisões

| Requisito | Vista que o endereça | Decisão que o realiza |
|---|---|---|
| RF-LAN-001 | Componentes | 0005 |
| RF-LAN-002 | Componentes | 0005 |
| RF-LAN-003 | Componentes | 0005 |
| RF-LAN-004 | Contêineres, Componentes | 0001, 0002, 0003 |
| RF-CON-001 | Componentes | 0004, 0005 |
| RF-CON-002 | Componentes | 0003 |
| RF-CON-003 | Componentes, Implantação | 0004 |
| RF-CON-004 | Componentes | 0004 |
| RNF-001 | Contêineres, Implantação | 0001, 0002, 0008 |
| RNF-002 | Contêineres, Implantação | 0004, 0006, 0007, 0008 |
| RNF-003 | Contexto, Implantação | 0006, 0007, 0008 |
| RNF-004 | Contêineres | 0002, 0003, 0006 |
| RNF-005 | Contêineres | 0002, 0005 |
| RNF-006 | Implantação | nenhuma — ver observação abaixo |

RNF-006 é o único requisito sem decisão associada, e a ausência é deliberada. Ele exige que dependência vulnerável não entre no artefato; **não havia alternativa arquitetural a registrar**, porque a verificação é do ferramental de construção e não do desenho do sistema. Um registro de decisão para essa escolha documentaria a seleção de uma ferramenta, não uma decisão de arquitetura — e o critério deste repositório é que decisão sem alternativa descartada não é decisão.

## Decisões arquiteturais

As decisões são registradas individualmente em [decisoes/](../decisoes/), cada uma com contexto, alternativas consideradas, decisão, consequências e os requisitos afetados. O índice está em [decisoes/index.md](../decisoes/index.md).

A ISO/IEC/IEEE 42010 exige que as decisões que afetam a arquitetura tenham rationale registrado e sejam rastreáveis até as preocupações que as motivaram. A coluna *Decisão* da tabela acima e a seção *Requisitos afetados* de cada registro fecham essa rastreabilidade nos dois sentidos.

## Inconsistências conhecidas

A ISO/IEC/IEEE 42010 exige que a descrição registre as inconsistências conhecidas entre vistas e modelos. Registrá-las é o que separa uma descrição verificada de uma descrição afirmada.

| Inconsistência | Situação |
|---|---|
| As vistas descrevem o gateway de API e o seu autorizador, que não existem em código nem em infraestrutura | Conhecida e intencional. Endpoints, publicador e consumidor, antes nesta linha, existem hoje; o gateway e o autorizador não. O estado de cada camada está em [plano de execução](../plano-de-execucao.md) |
| A vista de implantação descreve rede, borda e execução que ainda não estão no Terraform | Conhecida. Apenas a camada de persistência foi provisionada, conforme [ADR 0006](../decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md) |
| A vista de implantação atribui ao gateway o controle de vazão, e o [contrato da API](contrato-de-api.md) não prevê resposta de rejeição por excedente | Conhecida. O contrato descreve o que os serviços respondem hoje; o controle de vazão pertence a uma camada que não foi provisionada, e enquanto não for, não há rejeição por vazão a documentar |
| As metas de [metas e métricas](../qualidade/metas-e-metricas.md) pressupõem métricas de gateway, de balanceador e de fila que nenhuma instrumentação produz hoje | Conhecida. Os serviços emitem registros estruturados com identificador de correlação, e nada além disso. Os objetivos permanecem declarados como objetivos, não como medições realizadas |

### Método de verificação

A completude é verificada pelas duas matrizes de correspondência acima. A consistência entre documentação e código é verificada por auditoria, e as divergências encontradas viram correção ou entram nesta tabela — não permanecem tácitas.

## Glossário do domínio

| Termo | Definição |
|---|---|
| Lançamento | Registro individual de uma movimentação financeira, de crédito ou de débito, com valor, data de competência e descrição |
| Crédito | Lançamento que aumenta o saldo |
| Débito | Lançamento que reduz o saldo |
| Data de competência | Dia ao qual o lançamento pertence para efeito de apuração. É data pura, sem horário, resolvida no fuso do comerciante. Distinta da data de registro |
| Data de registro | Instante em que o lançamento foi persistido, armazenado em UTC. Usada para ordenação e auditoria, nunca para apuração |
| Fuso do comerciante | Fuso horário único do sistema, no qual a data de competência é resolvida. Não há comerciantes em fusos distintos |
| Saldo consolidado | Resultado da soma dos créditos menos a soma dos débitos de uma data de competência |
| Apuração | Registro do saldo consolidado de um dia, com os totais de crédito e débito e a quantidade de lançamentos incorporados |
| Projeção materializada | Apuração mantida pronta e atualizada a cada lançamento recebido, em vez de calculada no momento da consulta |
| Idempotência | Propriedade pela qual incorporar o mesmo lançamento mais de uma vez produz o mesmo saldo que incorporá-lo uma vez |
| Outbox | Registro do evento de integração gravado na mesma transação do lançamento, publicado em seguida por processo separado |
| Identificador de correlação | Valor que acompanha uma requisição por todo o seu percurso, incluindo a travessia assíncrona entre os serviços |
| Lançamento compensatório | Lançamento de sinal oposto usado para corrigir um lançamento anterior, já que lançamentos são imutáveis |
