---
type: indice
title: Catálogo de requisitos
description: Requisitos funcionais dos dois serviços e requisitos não funcionais transversais.
status: ativo
normas: [ISO/IEC/IEEE 29148, ISO/IEC 25010]
---

# Catálogo de requisitos

Os requisitos seguem a ISO/IEC/IEEE 29148: cada um é singular, verificável e não ambíguo, e está expresso em sintaxe EARS. Os requisitos não funcionais são classificados pelas características de qualidade da ISO/IEC 25010.

Cada arquivo declara a sua origem. Onde a origem é *especificação da especificação*, o texto original está transcrito no documento — o requisito é derivação direta, não interpretação.

## Requisitos funcionais — Lançamentos

| ID | Requisito | Padrão EARS |
|---|---|---|
| RF-LAN-001 | [Registrar lançamento](lancamentos/rf-lancamentos-001-registrar-lancamento.md) | event-driven |
| RF-LAN-002 | [Consultar lançamento por identificador](lancamentos/rf-lancamentos-002-consultar-lancamento.md) | event-driven |
| RF-LAN-003 | [Listar lançamentos por período](lancamentos/rf-lancamentos-003-listar-lancamentos-por-periodo.md) | event-driven |
| RF-LAN-004 | [Disponibilizar lançamentos para apuração](lancamentos/rf-lancamentos-004-disponibilizar-lancamentos-para-apuracao.md) | event-driven |

## Requisitos funcionais — Consolidado diário

| ID | Requisito | Padrão EARS |
|---|---|---|
| RF-CON-001 | [Apurar o saldo diário consolidado](consolidado/rf-consolidado-001-apurar-saldo-diario.md) | event-driven |
| RF-CON-002 | [Ignorar lançamento já apurado](consolidado/rf-consolidado-002-ignorar-lancamento-ja-apurado.md) | unwanted behaviour |
| RF-CON-003 | [Consultar o saldo de uma data](consolidado/rf-consolidado-003-consultar-saldo-de-uma-data.md) | event-driven |
| RF-CON-004 | [Consultar o saldo por período](consolidado/rf-consolidado-004-consultar-saldo-por-periodo.md) | event-driven |

## Requisitos não funcionais transversais

| ID | Requisito | Característica de qualidade | Origem |
|---|---|---|---|
| RNF-001 | [Isolamento de falha entre os serviços](transversais/rnf-001-isolamento-de-falha-entre-servicos.md) | Confiabilidade / Tolerância a falhas | especificação |
| RNF-002 | [Capacidade de consulta em dias de pico](transversais/rnf-002-capacidade-de-consulta-do-consolidado.md) | Eficiência de desempenho / Capacidade | especificação |
| RNF-003 | [Segurança de acesso e de transporte](transversais/rnf-003-seguranca-de-acesso-e-transporte.md) | Segurança / Confidencialidade e Autenticidade | objetivo da especificação |
| RNF-006 | [Integridade da cadeia de dependências](transversais/rnf-006-integridade-da-cadeia-de-dependencias.md) | Segurança / Integridade | objetivo da especificação |
| RNF-004 | [Recuperabilidade e convergência do consolidado](transversais/rnf-004-recuperabilidade-e-convergencia-do-consolidado.md) | Confiabilidade / Recuperabilidade | derivado de RNF-001 |
| RNF-005 | [Analisabilidade da execução](transversais/rnf-005-analisabilidade-da-execucao.md) | Manutenibilidade / Analisabilidade | derivado de RNF-002 e RNF-004 |

## Cobertura dos requisitos da especificação

| Texto da especificação | Requisitos que o realizam |
|---|---|
| Serviço que faça o controle de lançamentos | RF-LAN-001 a RF-LAN-004 |
| Serviço do consolidado diário | RF-CON-001 a RF-CON-004 |
| O serviço de lançamento não deve ficar indisponível se o consolidado cair | RNF-001, RNF-004, RF-LAN-004 |
| 50 requisições por segundo com no máximo 5% de perda | RNF-002, RF-CON-003 |

Os dois requisitos não funcionais da especificação geraram dois requisitos derivados — RNF-004 e RNF-005 — porque a verificabilidade dos originais depende deles. RNF-003 e RNF-006 derivam do objetivo da especificação, não dos requisitos não funcionais, e por isso são contados à parte. A derivação está justificada no início de cada arquivo.
