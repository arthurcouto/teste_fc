---
type: indice
title: Registros de decisão arquitetural
description: Decisões que moldaram a arquitetura, com contexto, alternativas consideradas e consequências.
status: ativo
---

# Registros de decisão arquitetural

A ISO/IEC/IEEE 42010 exige que as decisões que afetam a arquitetura tenham rationale registrado e sejam rastreáveis até as preocupações que as motivaram. Cada registro traz contexto, alternativas consideradas, decisão, consequências e os requisitos afetados.

As alternativas descartadas são descritas com o que tinham de bom. Um registro que só apresenta desvantagens das opções não escolhidas não documenta uma decisão — documenta uma justificativa construída depois.

Todos os registros aqui são decisões **sobre o sistema**. As escolhas de convenção documental — seguir a ISO/IEC/IEEE 42010, expressar as vistas em C4, catalogar requisitos em EARS — estão descritas em [index.md](../index.md) e não ocupam posição nesta lista: são decisões sobre o aparato, e misturá-las com as que moldam a arquitetura dilui o que quem lê veio procurar.

| ID | Decisão | Requisitos afetados | Estado |
|---|---|---|---|
| [0001](0001-decomposicao-em-dois-servicos.md) | Decompor o sistema em dois serviços independentes, um por capacidade de negócio | RNF-001, RF-LAN-004 | implementada |
| [0002](0002-comunicacao-assincrona-por-fila.md) | Comunicar os serviços exclusivamente por fila de mensagens | RNF-001, RNF-004, RNF-005, RF-LAN-004 | implementada |
| [0003](0003-outbox-transacional-e-consumo-idempotente.md) | Publicar por registro de saída transacional e consumir de forma idempotente | RF-LAN-004, RF-CON-002, RNF-004 | implementada |
| [0004](0004-consolidado-como-projecao-materializada.md) | Manter o consolidado como projeção materializada por data de competência | RF-CON-001, RF-CON-003, RF-CON-004, RNF-002 | implementada |
| [0005](0005-clean-architecture-e-ddd-tatico.md) | Organizar os serviços em camadas com dependências para dentro, aplicando DDD tático seletivamente | RF-LAN-001, RF-LAN-002, RF-LAN-003, RF-CON-001, RNF-005 | implementada |
| [0006](0006-persistencia-em-aurora-dsql-com-ef-core.md) | Persistir em Aurora DSQL, acessado por EF Core com autenticação por identidade | RNF-002, RNF-003, RNF-004 | implementada |
| [0007](0007-borda-com-api-gateway-cognito-e-fargate.md) | Expor os serviços por gateway de API com autorizador de credencial, executando-os em contêineres gerenciados | RNF-002, RNF-003 | parcial — verificação de credencial existe; borda não provisionada |
| [0008](0008-terraform-com-stack-unico-e-toggles.md) | Descrever a infraestrutura em Terraform, em stack único parametrizado por ambiente | RNF-001, RNF-002, RNF-003 | parcial — apenas a camada de persistência |

## Dependências entre decisões

Algumas decisões só se sustentam em conjunto, e desfazer uma reabre o problema que outra fechou.

- **0003 depende de 0002.** Comunicar por fila introduz entrega ao menos uma vez. Sem consumo idempotente, a decisão de usar fila troca um modo de falha por outro.
- **0007 depende de 0003.** Manter o consumidor no processo que escala horizontalmente só é seguro porque a incorporação é idempotente.
- **0004 depende de 0001.** A projeção materializada existe porque a consulta não pode alcançar a base do outro serviço.
- **0003 depende de 0001.** O registro de saída transacional só é necessário porque produtor e consumidor não compartilham transação.
- **0008 realiza 0001 e 0007.** É o código que torna verificável o que as duas afirmam.

## Decisões de omissão

O que foi deliberadamente **não** construído está registrado em [escopo e limites](../escopo-e-limites.md), com o critério que sustenta cada omissão, e em [evolucao/roadmap.md](../evolucao/roadmap.md), com a condição que traria cada item para dentro do escopo.
