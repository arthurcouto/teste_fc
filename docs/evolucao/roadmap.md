---
type: evolucao
title: Evoluções futuras
description: O que foi deixado de fora, e a condição concreta que traria cada item para dentro do escopo.
status: ativo
---

# Evoluções futuras

Esta página registra o que foi deliberadamente adiado, com uma regra: cada item declara **a condição que o traria para dentro do escopo**, não apenas o desejo de tê-lo. Sem essa condição, um roteiro de evolução vira lista de intenções.

Um item sem condição de entrada é uma lista de desejos. Um item com condição é uma decisão adiada, e o critério para revisitá-la está registrado.

## Correção de lançamentos

**O que falta:** lançamentos são imutáveis e não há operação de correção. Um erro de digitação permanece no histórico.

**Como resolver:** lançamento compensatório de sinal oposto, referenciando o original, com o par exibido em conjunto nas consultas. Preserva a imutabilidade e a auditabilidade, ao contrário de uma alteração destrutiva.

**Condição de entrada:** o primeiro relato de erro de registro por parte de quem usa. Não antes — a operação de correção sem histórico de necessidade real tende a ser desenhada errado.

## Expurgo dos lançamentos apurados

**O que falta:** a tabela que sustenta a idempotência cresce proporcionalmente ao volume de lançamentos, indefinidamente.

**Como resolver:** remover as entradas cuja data de competência esteja além da janela em que uma reentrega ainda é possível — a retenção do transporte mais uma margem. Passada essa janela, o registro não protege mais contra nada.

**Condição de entrada:** volume de lançamentos que torne o crescimento relevante para o desempenho das consultas de duplicidade. Com o volume de um comerciante, isso demora.

## Reconstrução da projeção

**O que falta:** a apuração é reconstruível em princípio ([ADR 0004](../decisoes/0004-consolidado-como-projecao-materializada.md)), mas não há operação que a reconstrua.

**Como resolver:** procedimento que reprocessa os lançamentos de um intervalo e recalcula as apurações correspondentes, executável por intervalo restrito e com o resultado comparável ao anterior antes de substituí-lo.

**Condição de entrada:** primeiro defeito encontrado na regra de apuração. A reconstrutibilidade é o que limita o dano desse defeito; a ferramenta é o que a torna utilizável sob pressão.

## Segundo consumidor dos lançamentos

**O que falta:** a fila tem um consumidor. Um segundo — apuração mensal, alerta de saldo negativo, exportação contábil — exigiria revisitar [ADR 0002](../decisoes/0002-comunicacao-assincrona-por-fila.md).

**Como resolver:** publicar em um mecanismo de distribuição com múltiplos assinantes, cada um com sua fila. O produtor não muda; muda o destino da publicação.

**Condição de entrada:** o segundo consumidor concreto. Antecipar a extensibilidade produziria uma estrutura não exercitada, e a forma certa dela depende de qual é o segundo consumidor.

## Autorização por papel

**O que falta:** há autenticação, não há autorização granular. Todo portador de credencial válida pode tudo.

**Como resolver:** papéis com permissões distintas, verificadas na camada de aplicação, com o escopo derivado da credencial.

**Condição de entrada:** o segundo tipo de usuário. Enquanto o sistema tem um ator, como declarado na [vista de contexto](../arquitetura/vistas/c1-contexto.md), a autorização granular não tem sujeito.

## Rastreamento distribuído

**O que falta:** a correlação entre os serviços é feita por identificador propagado no envelope da mensagem, sem instrumentação de rastreamento.

**Como resolver:** instrumentação padronizada, com propagação de contexto na travessia assíncrona e visualização do percurso completo.

**Condição de entrada:** um terceiro serviço, ou um salto assíncrono adicional. Com dois serviços e um salto, o identificador de correlação entrega o mesmo diagnóstico com muito menos aparato.

## Verificação automatizada dos ensaios de cenário

**O que falta:** os ensaios de [estratégia de testes](../qualidade/estrategia-de-testes.md) estão especificados em procedimento, não em código: não há gerador de carga versionado, nem roteiro executável, nem qualquer automação de integração — o repositório não tem uma. O ensaio de indisponibilidade foi conduzido à mão sobre o ambiente local, e é o único que chegou a rodar.

**Como resolver:** versionar os geradores de carga, e depois executá-los periodicamente contra um ambiente dedicado, com os resultados comparados às metas de [metas e métricas](../qualidade/metas-e-metricas.md).

**Condição de entrada:** existência de um ambiente estável e de uma rotina de entrega contínua. É a evolução mais valiosa da lista: sem ela, a aderência aos requisitos não funcionais é verificada pontualmente, e pode regredir sem sinal.

## Retenção e arquivamento de lançamentos

**O que falta:** os lançamentos crescem indefinidamente e todos permanecem na base de consulta.

**Como resolver:** particionamento por período e arquivamento dos períodos antigos em armazenamento de menor custo de acesso, mantendo as apurações — que são pequenas — integralmente disponíveis.

**Condição de entrada:** volume que degrade a listagem por período além da meta de latência.

## Consumidor em unidade de implantação própria

**O que falta:** o consumidor da fila roda dentro do contêiner do Consolidado, então escalar a leitura acrescenta consumidores que disputam a mesma linha de apuração do dia corrente.

**Como resolver:** separar o consumidor em serviço próprio, com escala pela idade da mensagem mais antiga em vez do volume de leitura.

**Condição de entrada:** volume de escrita em que o conflito de concorrência na linha do dia passe a aparecer nos registros. Com um comerciante e dezenas de lançamentos por dia, não aparece. Isso reabre [ADR 0001](../decisoes/0001-decomposicao-em-dois-servicos.md), que recusou a terceira unidade de implantação — e é por isso que a condição está escrita, e não a mudança.

## Objetivo de recuperação da persistência

**O que falta:** o motor é de região única e não há objetivo de ponto nem de tempo de recuperação declarado. Exclusão acidental ou corrupção não têm caminho de volta documentado.

**Como resolver:** exportação lógica periódica ou serviço gerenciado de cópia de segurança, com os dois objetivos declarados e um ensaio de restauração.

**Condição de entrada:** o primeiro dado real. Enquanto o sistema não guarda lançamento de ninguém, não há o que recuperar — mas essa condição é satisfeita no dia um de uso.

## Controles de custo

**O que falta:** não há orçamento nem alarme de gasto na conta, e as etiquetas de alocação de custo não estão ativas, o que torna o projeto invisível na fatura.

**Como resolver:** ativar as etiquetas que o provisionamento já emite, e criar orçamentos sobre custo e sobre uso bruto — o segundo porque crédito promocional esconde o primeiro.

**Condição de entrada:** imediata. É configuração de conta, não de projeto, e custa nada.

## Eliminação da segunda camada de borda

**O que falta:** o balanceador interno existe apenas para servir de alvo à integração privada do gateway.

**Como resolver:** a integração privada aceita também um registro de serviço, publicado nativamente pelo orquestrador. O gateway continua concentrando autenticação e vazão.

**Condição de entrada:** confirmar que as métricas de latência e erro por alvo, hoje obtidas do balanceador, são supridas pelo gateway e pelo orquestrador. Se forem, o balanceador é andaime.

## O que não está nesta lista

Event sourcing, segregação de bancos de leitura e escrita, orquestração autogerida de contêineres, particionamento por cliente, coordenação distribuída e versionamento de contrato não constam aqui como itens adiados, e sim como decisões de omissão.

Eles não resolvem problema que este sistema tenha. Estão em [escopo e limites](../escopo-e-limites.md), com a justificativa de cada exclusão.

A diferença entre as duas listas é deliberada: esta página contém o que falta; a outra contém o que não faz falta.
