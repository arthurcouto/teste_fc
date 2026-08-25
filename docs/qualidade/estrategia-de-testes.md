---
type: qualidade
title: Estratégia de testes
description: O que é testado, em que nível, e qual requisito cada nível demonstra.
status: ativo
---

# Estratégia de testes

Testes são requisito obrigatório da especificação. A questão relevante não é a existência de testes, mas o que cada um demonstra — uma suíte extensa que não exercita nenhum requisito não funcional deixaria o principal da especificação sem verificação.

Cada nível abaixo declara o que prova e o que não prova.

## Níveis

### Testes unitários

**Provam:** que as regras de negócio estão corretas.

Exercitam o agregado de Lançamento e a regra de apuração diária. Rodam sem banco, sem fila e sem rede — é o que os mantém rápidos o bastante para serem executados a cada alteração, e é a razão prática da organização em camadas de [ADR 0005](../decisoes/0005-clean-architecture-e-ddd-tatico.md).

Cobertura pretendida: as invariantes de [RF-LAN-001](../requisitos/lancamentos/rf-lancamentos-001-registrar-lancamento.md) individualmente, incluindo os limites — valor zero, valor negativo, competência do dia corrente, competência futura — e a apuração de [RF-CON-001](../requisitos/consolidado/rf-consolidado-001-apurar-saldo-diario.md) com apenas créditos, apenas débitos, mistura e dia sem lançamentos.

**Não provam:** que o mapeamento para o banco está correto, nem que a transação abrange o que deveria.

### Testes de arquitetura

**Provam:** que a estrutura declarada nas vistas corresponde ao código.

Transformam em restrição executável o que de outro modo seria convenção sujeita a erosão. Verificam que as camadas internas não referenciam pacote de infraestrutura, que tipos concretos públicos são selados, que toda operação assíncrona de porta aceita cancelamento, que o domínio não expõe escrita, e — o mais importante — que um serviço não referencia o outro.

A camada de apresentação já existe, e o que impede a camada de aplicação de alcançá-la é a regra que proíbe qualquer camada interna de referenciar montagem de ASP.NET Core — não uma regra nominal sobre projetos.

Essa última regra defende [RNF-001](../requisitos/transversais/rnf-001-isolamento-de-falha-entre-servicos.md) contra a erosão mais provável: alguém introduzir uma chamada síncrona entre os serviços por conveniência, meses depois, sem perceber que está desfazendo o requisito central do sistema.

O alcance do mecanismo precisa ser declarado com honestidade. Um teste sobre referências entre projetos detecta uma dependência de compilação; **não** detecta uma chamada por endereço montado em configuração, que é uma forma igualmente provável de erosão. A cobertura dessa segunda forma vem do ensaio de isolamento de falha, que derruba o Consolidado e observa o Lançamentos: se existir chamada oculta, o ensaio a expõe.

Nenhum dos dois mecanismos sozinho é suficiente. É a razão de existirem os dois.

**Não provam:** que o comportamento em execução corresponde à estrutura.

### Testes de integração

**Provam:** que os componentes funcionam juntos, com banco real e com o servidor HTTP real.

A suíte **não** provisiona banco: ela lê `LEDGER_DB_HOST` e `CONSOLIDATION_DB_HOST` do ambiente e executa contra os agrupamentos que estiverem ali. Sem essas variáveis, os testes que dependem do motor são ignorados e o comando reporta isso — decisão registrada no [README](../../README.md). A razão é a de [ADR 0006](../decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md): a semântica que estes testes existem para exercitar é a de concorrência do motor real, e um contêiner de PostgreSQL a reproduziria errado, o que é pior do que não a exercitar.

Os testes de borda — endpoints, autenticação, formato de erro — rodam contra o servidor HTTP real em memória e não precisam de motor nenhum.

Cobertura pretendida: persistência do lançamento e do registro de saída na mesma transação; reivindicação atômica sob consumo concorrente; endpoints com entradas válidas e inválidas; comportamento das consultas em datas sem movimento.

**Não provam:** o comportamento sob carga nem sob falha de infraestrutura.

### Ensaios de cenário

**Provam:** os requisitos não funcionais que nenhum dos níveis anteriores alcança.

São a parte da estratégia que responde diretamente à especificação, e estão detalhados abaixo.

## Ensaios de cenário

### Isolamento de falha

Demonstra [RNF-001](../requisitos/transversais/rnf-001-isolamento-de-falha-entre-servicos.md), o primeiro requisito não funcional da especificação.

1. Carga contínua de registro de lançamentos com os dois serviços no ar. Coleta da linha de base de taxa de sucesso e latência p95.
2. Derrubada completa do serviço de Consolidado, mantida a carga. Coleta dos mesmos indicadores.
3. Comparação: a taxa de sucesso deve permanecer igual à linha de base e a latência p95 variar no máximo 10%.

**Critério de falha:** taxa de sucesso fora do intervalo de confiança de 95% da linha de base.

Medição sobre tráfego real é estocástica: exigir igualdade estrita produziria falha por ruído amostral, não por degradação. A banda de tolerância é o que torna o critério verificável — sem ela, o ensaio reprova na primeira execução e o requisito deixa de discriminar.

### Convergência após indisponibilidade

Demonstra [RNF-004](../requisitos/transversais/rnf-004-recuperabilidade-e-convergencia-do-consolidado.md), e é o que dá sentido ao ensaio anterior.

1. Consumidor interrompido por 30 minutos sob carga contínua de registro.
2. Restabelecimento do consumidor.
3. Medição do tempo até que a soma dos lançamentos registrados iguale o saldo apurado.

**Critério de falha:** qualquer lançamento confirmado que não apareça no saldo após a convergência. Tolerância zero.

### Capacidade sob pico

Demonstra [RNF-002](../requisitos/transversais/rnf-002-capacidade-de-consulta-do-consolidado.md), o segundo requisito não funcional da especificação.

1. Rampa até 50 requisições por segundo sobre a consulta de saldo de uma data.
2. Patamar sustentado por 10 minutos.
3. Medição de taxa de erro e de latência por percentil.
4. Ensaio adicional a 200 requisições por segundo — o dobro do limite de vazão previsto — para observar o comportamento sob sobrecarga.

**Critério de aceitação:** taxa de erro não superior a 5%, conforme a especificação. **Critério de projeto:** não superior a 1%, conforme [metas e métricas](metas-e-metricas.md).

O ensaio a 200 requisições por segundo não tem critério de aprovação. Ele roda claramente acima do limite de vazão para que a rejeição seja inequívoca; no limite exato, o resultado dependeria do algoritmo de balde e da rajada, e o ensaio não discriminaria o comportamento que se propõe a observar. Existe para verificar que a degradação é explícita — rejeição imediata — e não silenciosa, na forma de latência crescente e esgotamento de tempo.

Este quarto passo **ainda não é executável**: ele observa o controle de vazão do gateway, que pertence a uma camada de infraestrutura não provisionada. Enquanto o gateway não existir, não há limite a exceder e o ensaio não tem o que discriminar.

### Idempotência sob consumo paralelo

Demonstra [RF-CON-002](../requisitos/consolidado/rf-consolidado-002-ignorar-lancamento-ja-apurado.md), e sustenta a decisão de escalar o consumidor junto com a leitura em [ADR 0007](../decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md).

Várias réplicas consumindo a mesma fila, com reentrega deliberada das mesmas mensagens. O saldo final deve ser idêntico ao produzido por consumo único e sequencial.

## Verificações que não são testes

Duas verificações rodam junto da suíte e falham a construção, embora não sejam testes: a auditoria de dependências com vulnerabilidade conhecida de severidade moderada ou superior, incluindo transitivas; e os analisadores estáticos, com aviso tratado como erro. Vulnerabilidade de severidade baixa aparece como aviso, pelas razões em [RNF-006](../requisitos/transversais/rnf-006-integridade-da-cadeia-de-dependencias.md). As duas estão em `back/Directory.Build.props`.

Estão aqui porque compartilham a propriedade que importa: falham no instante em que o problema entra, e não em revisão posterior. O que as separa dos testes é que não exercitam comportamento — verificam propriedades do próprio código e das suas dependências.

As duas rodam na construção local e também na integração contínua, que executa restauração, construção e suíte a cada envio para a linha principal. O que **não** existe, e que [RNF-006](../requisitos/transversais/rnf-006-integridade-da-cadeia-de-dependencias.md) registra como lacuna: checagem de dependência descontinuada, alerta automático de vulnerabilidade no repositório e varredura de segredos.

## O que não é testado, e por quê

| Não testado | Motivo |
|---|---|
| Interface de usuário | A interface está fora do escopo da especificação e existe para tornar os serviços demonstráveis. São três arquivos sem dependência nem etapa de construção; testá-la não verificaria nenhum requisito |
| Provedor de identidade | Serviço externo. Os testes verificam a rejeição de credencial ausente, expirada e com assinatura inválida, não a emissão |
| Camada de apresentação isoladamente | Não contém regra de negócio. É exercitada pelos testes de integração através dos endpoints |
| Mapeamento objeto-relacional isoladamente | Exercitado pelos testes de integração contra banco real, onde o erro efetivamente aparece |

## Cobertura

A meta de cobertura incide sobre as camadas de domínio e de aplicação, onde vivem as regras. Cobertura alta em camada de apresentação mede exercício de código sem regra, e produz um número melhor sem produzir um sistema melhor.

Nenhuma meta de cobertura substitui os ensaios de cenário: é possível ter cobertura completa das regras e ainda assim falhar os dois requisitos não funcionais da especificação, porque eles não são propriedades de unidades de código.
