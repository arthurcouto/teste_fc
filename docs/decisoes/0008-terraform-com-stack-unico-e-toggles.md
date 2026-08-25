---
type: decisao
id: ADR-0008
title: Descrever a infraestrutura em Terraform, em stack único parametrizado por ambiente
status: aceita
data: 2026-08-18
tags: [infraestrutura, terraform, ambientes]
requisitos_afetados: [RNF-001, RNF-002, RNF-003]
---

# ADR 0008 — Descrever a infraestrutura em Terraform, em stack único parametrizado por ambiente

> **Estado.** Esta decisão está **parcialmente implementada**. O Terraform do repositório provisiona a camada de persistência e o estado remoto; rede, execução, borda, identidade e distribuição do front-end permanecem como desenho.

## Contexto

A [vista de implantação](../arquitetura/vistas/vista-de-implantacao.md) afirma isolamento de falha, escala horizontal e controles de segurança em pontos específicos. Uma vista que afirma isso sem código que a realize não é verificável — e a especificação avalia decisões, o que exige que elas sejam confrontáveis.

A infraestrutura não é pedida pela especificação, e — sendo honesto — **não passa** nos três testes do critério de suficiência de [escopo e limites](../escopo-e-limites.md). Nenhum requisito deixa de se sustentar sem ela: a vista de implantação já comunica o desenho, e o código apenas o torna confrontável.

Ela entra como **exceção declarada ao critério**, por decisão de quem conduz o projeto, e não por uma leitura elástica do teste dois. Registrar isso assim é preferível a alargar o critério até que ele acomode o que já se decidiu fazer — um critério que se ajusta ao resultado desejado deixa de ser critério.

## Alternativas consideradas

**Provisionamento manual, documentado em prosa.** Sem ferramenta e sem estado a manter. A descrição diverge do provisionado sem sinal, e nada garante que dois ambientes sejam iguais.

**Um conjunto de definições por ambiente.** Isolamento máximo entre ambientes. Duplica a descrição, e as cópias divergem — o ambiente onde se valida deixa de representar aquele onde se executa.

**Módulos reutilizáveis compostos por ambiente.** Boa fatoração quando há muitos ambientes e muitos consumidores. Introduz uma camada de indireção e um contrato de módulo a versionar, sem que a quantidade de ambientes justifique.

**Stack único parametrizado por ambiente.** Uma só descrição, um conjunto de variáveis por ambiente e isolamento pelo estado. Um erro na descrição alcança todos os ambientes, o que é mitigado pela ordem de aplicação.

## Decisão

Descrever toda a infraestrutura em Terraform, em um stack único, parametrizado por um conjunto de variáveis por ambiente. O isolamento entre ambientes é dado pela separação do estado remoto, não por duplicação da descrição.

Separar os recursos que guardam estado dos que são recriáveis, em arquivos distintos, de modo que a camada descartável possa ser removida e recriada sem alcançar os dados.

Controlar a ativação da camada descartável por sinalizadores por ambiente, cujo valor padrão é o de não criar. Um ambiente novo nasce sem carga em execução, e a ativação é ato explícito.

Decompor as definições por capacidade, com um arquivo por assunto de infraestrutura e um conjunto de variáveis por domínio de configuração.

O código não é comentado. A explicação de cada decisão vive nos registros de decisão e nas vistas, onde pode ser lida por quem não vai abrir o código.

## Consequências

A vista de implantação torna-se confrontável com código **na medida em que o código exista**, e hoje ele cobre apenas a camada de persistência. Onde há Terraform, a divergência entre o que se afirma e o que existe deixa de ser possível em silêncio; onde não há, o desenho continua sendo afirmação — e é por isso que as vistas e as decisões que descrevem borda, execução e identidade carregam aviso de estado.

A separação entre recursos persistentes e descartáveis permite destruir e recriar a camada de execução sem risco para os dados, o que torna o ambiente de trabalho barato de reconstruir e reduz a hesitação em recriá-lo.

Sinalizadores com padrão de não criar tornam a existência de carga em execução uma decisão explícita por ambiente, em vez de consequência de aplicar a descrição.

Em contrapartida, um stack único significa que um erro na descrição alcança todos os ambientes. A mitigação é a ordem de aplicação: a mudança é aplicada primeiro no ambiente de desenvolvimento, e a promoção é ato separado.

O escopo é declarado por camada. A camada de persistência **foi aplicada em conta real**, porque validar a decisão de [ADR 0006](0006-persistencia-em-aurora-dsql-com-ef-core.md) exigia o motor de verdade. As demais camadas vão até o plano de execução validado.

Esta decisão diz *stack único* e o repositório tem dois diretórios de Terraform. O `bootstrap` é exceção conhecida e inevitável: ele cria o próprio armazenamento de estado remoto, e por isso não pode viver no stack que depende desse armazenamento. Tudo o mais é um stack só.

## Requisitos afetados

- [RNF-001 — Isolamento de falha entre os serviços](../requisitos/transversais/rnf-001-isolamento-de-falha-entre-servicos.md)
- [RNF-002 — Capacidade de consulta em dias de pico](../requisitos/transversais/rnf-002-capacidade-de-consulta-do-consolidado.md)
- [RNF-003 — Segurança de acesso e de transporte](../requisitos/transversais/rnf-003-seguranca-de-acesso-e-transporte.md)
