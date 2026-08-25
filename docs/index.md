---
type: indice
title: Documentação do sistema de fluxo de caixa diário
description: Mapa de leitura da documentação de projeto.
status: ativo
---

# Documentação do sistema de fluxo de caixa diário

Sistema de controle de fluxo de caixa para um comerciante, composto por dois serviços: **Lançamentos**, que registra débitos e créditos, e **Consolidado diário**, que apura e serve o saldo consolidado por dia.

## Por onde começar

Depende do que se quer verificar. Os atalhos por objetivo estão no [README](../README.md#documentação), que é a porta de entrada do repositório; repeti-los aqui só criaria duas listas a manter em acordo. Abaixo está o catálogo completo.

## Documentos

### Escopo e condução

- [Plano de execução](plano-de-execucao.md) — escopo, decisões fixadas, estrutura do repositório e fases de trabalho.
- [Escopo e limites](escopo-e-limites.md) — o critério de suficiência, o que foi omitido e a justificativa de cada omissão.

### Arquitetura

- [Descrição arquitetural](arquitetura/descricao-arquitetural.md) — stakeholders, preocupações, pontos de vista, matrizes de correspondência e glossário do domínio.
- [C1 — Contexto](arquitetura/vistas/c1-contexto.md) — o sistema como caixa preta, seus atores e sistemas externos.
- [C2 — Contêineres](arquitetura/vistas/c2-conteineres.md) — as unidades executáveis, as bases e o modo como se comunicam.
- [C3 — Componentes](arquitetura/vistas/c3-componentes.md) — organização interna dos dois serviços e o modelo de dados de cada um.
- [Implantação](arquitetura/vistas/vista-de-implantacao.md) — projeção sobre a infraestrutura, controles de segurança e mecanismos de escala.
- [Contrato da API](arquitetura/contrato-de-api.md) — rotas, formato de erro e mapeamento entre falha de domínio e resposta.

### Requisitos

- [Catálogo de requisitos](requisitos/index.md) — requisitos funcionais dos dois serviços e não funcionais transversais, com a cobertura da especificação.

### Decisões

- [Registros de decisão](decisoes/index.md) — as oito decisões que moldaram a arquitetura, com alternativas consideradas e consequências.

### Comportamento e qualidade

- [Fluxo de ponta a ponta](fluxos/fluxo-de-ponta-a-ponta.md) — o caminho de um lançamento e o comportamento sob cada falha.
- [Metas e métricas](qualidade/metas-e-metricas.md) — objetivos de nível de serviço e como cada um é medido.
- [Estratégia de testes](qualidade/estrategia-de-testes.md) — o que cada nível de teste prova, e o que não prova.

### Evolução

- [Evoluções futuras](evolucao/roadmap.md) — o que falta, com a condição que traria cada item para dentro do escopo.

## Convenções

Todo documento carrega *frontmatter* YAML com o campo `type`. Os diagramas são escritos em Mermaid e embutidos no markdown, para que sejam versionados como texto e renderizados sem ferramenta adicional.

A descrição arquitetural segue a **ISO/IEC/IEEE 42010**, com as vistas expressas nos três primeiros níveis do modelo **C4**. As duas são complementares e nenhuma bastaria sozinha: a norma fornece a estrutura — stakeholders, preocupações, pontos de vista, correspondências e rationale — mas não prescreve notação; o C4 fornece uma notação clara e progressiva, mas não oferece critério para demonstrar que nada foi esquecido. É a matriz de correspondência, que só a norma exige, que torna a lacuna visível.

Os requisitos seguem a **ISO/IEC/IEEE 29148**, em sintaxe EARS, e os não funcionais são classificados pelas características de qualidade da **ISO/IEC 25010**. Os dois requisitos não funcionais da especificação estão escritos em prosa e não são verificáveis como estão; a norma obriga a torná-los singulares e mensuráveis, e a sintaxe EARS obriga a explicitar gatilho e resposta. As interpretações necessárias para isso são declaradas em seção própria de cada requisito, para que se possa conferi-las contra o texto original — que vem transcrito no documento sempre que a origem é a especificação.

Os links entre documentos são relativos, e formam a rede de rastreabilidade: requisito aponta vista e decisão, vista declara preocupações, decisão aponta requisitos afetados.
