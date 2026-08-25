---
type: requisito
id: RNF-001
title: Isolamento de falha entre os serviços
classe: nao-funcional
caracteristica_qualidade: Confiabilidade / Tolerância a falhas
origem: especificação da especificação
padrao_ears: state-driven
verificacao: teste-integracao, teste-de-arquitetura
status: aprovado
vistas: [arquitetura/vistas/c2-conteineres.md, arquitetura/vistas/vista-de-implantacao.md]
decisoes: [decisoes/0001-decomposicao-em-dois-servicos.md, decisoes/0002-comunicacao-assincrona-por-fila.md, decisoes/0008-terraform-com-stack-unico-e-toggles.md]
---

# RNF-001 — Isolamento de falha entre os serviços

## Origem

Texto da especificação: *"O serviço de controle de lançamento não deve ficar indisponível se o sistema de consolidado diário cair."*

## Declaração

**Enquanto** o serviço de Consolidado diário estiver indisponível, o serviço de Lançamentos **deve** permanecer disponível para registrar e consultar lançamentos, sem degradação de latência atribuível à indisponibilidade.

## Critérios de aceitação

- O serviço de Lançamentos não realiza chamada síncrona ao serviço de Consolidado em nenhum fluxo, de leitura ou de escrita.
- Os dois serviços são unidades de implantação independentes: reiniciar, escalar ou derrubar um não afeta o ciclo de vida do outro.
- Os dois serviços não compartilham esquema de persistência. Nenhuma consulta de um alcança tabela do outro.
- Com o Consolidado inteiramente fora do ar, a taxa de sucesso do registro de lançamentos permanece dentro do intervalo de confiança de 95% da linha de base, e a latência no percentil 95 varia no máximo 10%.
- As mensagens produzidas durante a indisponibilidade são retidas pelo transporte e entregues no restabelecimento, sem perda.

## Meta e medição

| Indicador | Meta | Como medir |
|---|---|---|
| Disponibilidade do registro de lançamentos com o Consolidado fora do ar | dentro do IC de 95% da linha de base | ensaio de integração com o consumidor derrubado |
| Variação da latência p95 do registro na mesma condição | ≤ 10% | comparação com a linha de base do mesmo ensaio |
| Mensagens perdidas durante a indisponibilidade | zero | contagem de lançamentos registrados versus apurados após o restabelecimento |

## Verificação

Ensaio de integração em três etapas: linha de base com os dois serviços no ar; derrubada do Consolidado com carga contínua de registro; restabelecimento e verificação da convergência do saldo. Complementado por teste de arquitetura que falha se o serviço de Lançamentos passar a referenciar qualquer cliente do Consolidado.

## Rastreabilidade

- Vistas: [C2 — Contêineres](../../arquitetura/vistas/c2-conteineres.md), [Implantação](../../arquitetura/vistas/vista-de-implantacao.md)
- Decisões: [0001](../../decisoes/0001-decomposicao-em-dois-servicos.md), [0002](../../decisoes/0002-comunicacao-assincrona-por-fila.md)
- Requisitos relacionados: [RF-LAN-004](../lancamentos/rf-lancamentos-004-disponibilizar-lancamentos-para-apuracao.md), [RNF-004](rnf-004-recuperabilidade-e-convergencia-do-consolidado.md)
