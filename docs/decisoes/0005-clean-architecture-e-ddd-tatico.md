---
type: decisao
id: ADR-0005
title: Organizar os serviços em camadas com dependências para dentro, aplicando DDD tático seletivamente
status: aceita
data: 2026-08-18
tags: [clean-architecture, ddd, camadas, testabilidade]
requisitos_afetados: [RF-LAN-001, RF-LAN-002, RF-LAN-003, RF-CON-001, RNF-005]
---

# ADR 0005 — Organizar os serviços em camadas com dependências para dentro, aplicando DDD tático seletivamente

## Contexto

Boas práticas de desenvolvimento, padrões de arquitetura e SOLID são requisito obrigatório. Isso admite duas leituras opostas: aplicar o maior número possível de padrões, ou aplicar os que a regra de negócio justifica. A primeira produz um repositório maior e um projeto pior.

O sistema tem duas capacidades de naturezas diferentes. O registro de lançamentos tem invariantes reais: valor positivo, tipo válido, competência não futura, imutabilidade após o registro. A apuração do saldo é uma agregação idempotente sem ciclo de vida a proteger.

## Alternativas consideradas

**Camada única com acesso direto a dados.** Menos indireção e leitura imediata para um sistema pequeno. As regras de negócio ficariam acopladas ao mecanismo de persistência, exigindo banco para testá-las e transformando a suíte de regras em suíte de integração.

**Clean Architecture aplicada uniformemente aos dois serviços.** Simétrica e previsível. Produziria no Consolidado uma camada de domínio para hospedar uma soma — estrutura sem conteúdo, indireção que não protege invariante nenhuma.

**Camadas com dependências para dentro, com DDD tático onde há invariante.** Quatro camadas no Lançamentos, três no Consolidado. Assimétrico, e a assimetria precisa ser justificada para não ser lida como descuido.

## Decisão

Organizar os dois serviços em camadas com dependências apontando para dentro. A camada de domínio não conhece persistência, transporte nem apresentação; a de aplicação declara as portas; a de infraestrutura as implementa.

Aplicar DDD tático onde existe invariante real. No Lançamentos, o agregado protege as suas quatro invariantes no ato da construção, e os objetos de valor tornam irrepresentável um valor ou um tipo inválido.

No Consolidado, manter três camadas. A regra de apuração vive na camada de aplicação, como componente próprio e testável isoladamente.

Verificar a regra de dependência por teste automatizado, não por convenção.

## Consequências

As regras de negócio passam a ser exercitáveis sem banco, sem fila e sem rede, porque não dependem de nada disso. É o que mantém a suíte unitária rápida o bastante para rodar a cada alteração — e essa velocidade é a razão prática da decisão, mais do que a pureza do desenho.

Um agregado que valida no construtor não admite estado inválido em nenhum momento da sua existência. O erro deixa de ser detectado por verificação espalhada pelos casos de uso e passa a ser impossível de representar.

O custo é indireção: um caso de uso simples atravessa três camadas e uma porta. Em troca, trocar o mecanismo de persistência não alcança a regra de negócio.

A assimetria entre os serviços é a consequência mais visível, e é deliberada. Está registrada aqui, na [vista de componentes](../arquitetura/vistas/c3-componentes.md) e em [escopo e limites](../escopo-e-limites.md), para que seja lida como decisão e não como inconsistência.

Verificar a regra de dependência por teste transforma a declaração em restrição executável: uma referência indevida quebra a construção em vez de sobreviver em revisão.

## Requisitos afetados

- [RF-LAN-001 — Registrar lançamento](../requisitos/lancamentos/rf-lancamentos-001-registrar-lancamento.md)
- [RF-LAN-002 — Consultar lançamento por identificador](../requisitos/lancamentos/rf-lancamentos-002-consultar-lancamento.md)
- [RF-LAN-003 — Listar lançamentos por período](../requisitos/lancamentos/rf-lancamentos-003-listar-lancamentos-por-periodo.md)
- [RF-CON-001 — Apurar o saldo diário consolidado](../requisitos/consolidado/rf-consolidado-001-apurar-saldo-diario.md)
- [RNF-005 — Analisabilidade da execução](../requisitos/transversais/rnf-005-analisabilidade-da-execucao.md)
