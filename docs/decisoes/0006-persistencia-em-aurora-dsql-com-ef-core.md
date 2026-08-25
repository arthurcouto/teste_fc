---
type: decisao
id: ADR-0006
title: Persistir em Aurora DSQL, acessado por EF Core com autenticação por identidade
status: aceita
data: 2026-08-18
tags: [persistencia, dsql, ef-core]
requisitos_afetados: [RNF-002, RNF-003, RNF-004]
---

# ADR 0006 — Persistir em Aurora DSQL, acessado por EF Core com autenticação por identidade

## Contexto

Os dois serviços precisam de persistência relacional. O modelo é pequeno e bem definido: lançamentos e registros de saída de um lado, apurações diárias e lançamentos apurados do outro. As restrições relevantes são a chave primária e a unicidade, que sustentam a imutabilidade do lançamento e a idempotência da apuração.

A carga de leitura é elástica por natureza — o pico da especificação ocorre em dias de movimento, não continuamente — e a de escrita é modesta e constante.

## Alternativas consideradas

**Instância relacional gerenciada de tamanho fixo.** Comportamento previsível e compatibilidade completa com PostgreSQL, incluindo chaves estrangeiras aplicadas pelo banco. Exige dimensionar a capacidade de antemão e ajustá-la manualmente, o que não acompanha uma carga elástica sem intervenção.

**Banco de documentos ou chave-valor.** Latência constante e escala horizontal natural, adequados à leitura de uma apuração por chave. Tornariam a modelagem menos direta e afastariam a solução do que se espera de um sistema transacional com invariantes de unicidade.

**Aurora DSQL.** Compatível com PostgreSQL, com capacidade elástica sem gestão de instância e autenticação por identidade da carga de trabalho, dispensando senha estática. Em contrapartida, tem restrições próprias em relação ao PostgreSQL convencional.

## Decisão

Persistir em Aurora DSQL, com uma base por serviço, acessado por EF Core.

A autenticação usa credencial de curta duração derivada da identidade da carga de trabalho, sem senha estática em código, imagem ou variável de ambiente.

As restrições de integridade essenciais são a chave primária de `daily_balance`, que é a data de competência, e a de `processed_entry`, que é o identificador do lançamento. São elas que sustentam a idempotência, e são aplicadas pelo banco.

O esquema é versionado em migrações aplicadas antes da entrada em serviço de uma nova versão da aplicação.

## Consequências

A autenticação por identidade elimina uma classe inteira de exposição: não há senha de banco para vazar em repositório, imagem ou variável de ambiente. É o que realiza o critério correspondente do requisito de segurança.

A capacidade acompanha a carga sem dimensionamento prévio, o que é adequado a um pico concentrado em dias específicos.

Em contrapartida, o motor não aplica chaves estrangeiras como o PostgreSQL convencional, e a integridade referencial entre lançamento e registro de saída passa a ser responsabilidade da aplicação, garantida pela transação que os grava juntos. Isso é aceitável porque o modelo é raso e as relações são poucas, mas é uma diferença real que precisa estar declarada.

O controle de concorrência é otimista, e transações concorrentes sobre a mesma linha podem ser rejeitadas e precisar de nova tentativa. Isso alcança diretamente a apuração diária, cuja linha é atualizada por todo lançamento do mesmo dia — o caminho de escrita mais disputado do sistema. A nova tentativa é implementada no consumidor, e é segura porque a incorporação é idempotente.

O ferramental de migração difere do fluxo convencional do EF Core, e as migrações são aplicadas por passo próprio no processo de implantação.

Bases separadas por serviço impedem consulta cruzada em transação única, consequência já assumida em [ADR 0001](0001-decomposicao-em-dois-servicos.md).

## Equivalência entre o ambiente local e o de produção

Esta é a consequência mais incômoda da decisão, e ela contradiz parcialmente um critério usado em [ADR 0007](0007-borda-com-api-gateway-cognito-e-fargate.md).

Ali, funções sob demanda foram rejeitadas em parte porque "o modelo de execução diferiria do ambiente local, enfraquecendo a equivalência entre o que se testa e o que se implanta". O mesmo critério, aplicado aqui, é desfavorável: executar os testes de integração contra PostgreSQL convencional e a produção contra um motor com controle de concorrência otimista, sem chaves estrangeiras e sem bloqueio pessimista significa que os testes **não** exercitam a semântica que mais importa — justamente a de concorrência, que é onde o caminho de escrita mais disputado do sistema opera.

Reconhecer a assimetria é preferível a escondê-la. A consequência aceita é: os testes que dependem de semântica de concorrência — reivindicação do publicador, atualização concorrente da apuração diária, conflito e nova tentativa — **não têm valor probatório contra PostgreSQL local** e precisam ser executados contra o motor real antes de a decisão ser considerada validada.

## Resultado do portão de validação

O portão foi executado contra um cluster real antes de qualquer código de aplicação. **Os três pontos passaram**, e a execução produziu quatro restrições que não estavam previstas e que alteram o desenho.

| Verificação | Resultado |
|---|---|
| Provedor EF Core opera contra o motor | Passou. Inserção, projeção, filtro por data e transação explícita funcionam |
| Precisão decimal preservada ponta a ponta | Passou. Soma exata, sem erro de ponto flutuante |
| Reivindicação por atualização condicional sob concorrência | Passou. O conflito é sinalizado no *commit* com o código `OC000`, e a transação perdedora refaz a tentativa |
| Chave primária sustenta a idempotência | Passou |

### Restrições descobertas

**Índice parcial não existe.** `CREATE INDEX ... WHERE` é rejeitado, inclusive na forma assíncrona. O índice sobre os registros de saída pendentes previsto na [vista de componentes](../arquitetura/vistas/c3-componentes.md) passa a ser índice comum sobre a coluna de publicação, e o filtro dos pendentes é feito na consulta.

**Índices exigem criação assíncrona.** A forma aceita é `CREATE INDEX ASYNC`, e o EF Core gera `CREATE INDEX`. Confirmado inspecionando o *script* que o próprio EF Core produz. **As migrações geradas automaticamente não se aplicam sem edição**: todo índice precisa de instrução escrita à mão.

**Transação explícita exige nível de isolamento declarado.** O padrão do EF Core é *read committed*, que o motor rejeita com `0A000`. Toda transação aberta explicitamente precisa declarar *repeatable read*. O caminho implícito, usado quando o contexto salva sem transação aberta à mão, funciona sem ajuste — o que torna o erro fácil de não encontrar em teste, porque só aparece onde há transação explícita, exatamente onde estão as duas garantias que mais importam.

**Sequências exigem tamanho de cache explícito.** Não afeta o desenho, que já usa identificadores gerados fora do banco, mas impede a estratégia padrão do EF Core caso alguém a introduza.

Chaves estrangeiras, como antecipado, não são suportadas.

### Um cluster por serviço

O motor aceita **um único banco por cluster**: `CREATE DATABASE` é rejeitado, e cada cluster expõe apenas `postgres`. `CREATE SCHEMA` funciona. Isso reduz a separação de dados exigida por [ADR 0001](0001-decomposicao-em-dois-servicos.md) a duas formas possíveis, e a escolha entre elas é decisão de arquitetura, não detalhe de provisionamento.

| | Isolamento | Custo |
|---|---|---|
| Um cluster por serviço | Falha e saturação de um não alcançam o outro | Dois endpoints, dois alvos de migração, dois conjuntos de permissão |
| Um cluster, um schema por serviço | Separação lógica, destino compartilhado | Um endpoint, uma migração |

**Decisão: um cluster por serviço.** Um cluster compartilhado seria o maior componente de destino compartilhado do sistema — a única peça capaz de derrubar os dois serviços simultaneamente — e colocá-lo no centro de uma arquitetura cuja razão de ser é o isolamento de falha contradiz o requisito que a origina.

Duas afirmações desta documentação dependem diretamente disso e seriam falsas com um cluster só: a linha do resumo de falhas em [fluxo de ponta a ponta](../fluxos/fluxo-de-ponta-a-ponta.md) que garante o registro funcionando com a base de apurações fora do ar, e a de [implantação](../arquitetura/vistas/vista-de-implantacao.md) segundo a qual nenhum recurso compartilhado alcança os dois serviços.

Há um efeito secundário que reforça a decisão: com clusters separados, consulta ou transação cruzada entre os serviços não é proibida, é **impossível**. A separação deixa de depender da disciplina de quem escreve o código e passa a ser garantida pela infraestrutura.

### Restrições descobertas ao implementar

O portão da fase 2 usou o cliente de linha de comando. Implementar o acesso pelo mapeador objeto-relacional revelou oito restrições que aquele portão não alcançava — todas encontradas por teste contra o motor real, nenhuma documentada de antemão.

| Restrição | Efeito | Tratamento |
|---|---|---|
| `DISCARD` não é suportado | O cliente envia essa instrução ao devolver conexão ao conjunto; toda devolução falhava | Desativar a reinicialização de conexão ao fechar |
| `SAVEPOINT` não é suportado | O mapeador cria um ponto de salvamento ao salvar dentro de transação aberta | Desativar pontos de salvamento automáticos |
| Múltiplas instruções de definição de esquema numa transação não são suportadas | Um arquivo de migração com várias instruções falha inteiro | O migrador divide o arquivo e aplica uma instrução por vez |
| O conflito de concorrência chega com estado `40001` | Detectá-lo pelo código específico do motor citado na mensagem **nunca casa**, e o conflito escapa como erro definitivo | Detectar por `40001` e por deadlock, não pelo texto |
| Reverter uma transação já abortada pelo servidor lança exceção | A reversão explícita no caminho de retentativa mascarava o conflito original | Deixar o descarte reverter |
| Bloqueio consultivo (`pg_advisory_lock`) não existe | Duas réplicas subindo juntas não podem ser serializadas por bloqueio na aplicação das migrações | As migrações são idempotentes e o migrador trata o erro de objeto duplicado da corrida de definição de esquema como já aplicado, o que funciona nos dois motores |
| Bloqueio pessimista de linha (`FOR UPDATE SKIP LOCKED`) não existe | A reivindicação do lote do registro de saída não pode se apoiar em bloqueio | Reivindicar por atualização condicional que carimba o identificador da instância e reler o que ficou carimbado |
| Oito escritores simultâneos na mesma linha esgotam cinco tentativas | O saldo diário do dia corrente é a linha mais disputada do sistema | Doze tentativas com espera aleatória plena |

A última merece atenção porque **confirma por medição** o alerta da [vista de implantação](../arquitetura/vistas/vista-de-implantacao.md) de que escalar a leitura acrescenta consumidores que disputam a linha do dia. Com oito escritores concorrentes o orçamento de cinco tentativas não converge; com doze, converge. Isso quantifica o limite: não é conjectura, é o número medido.

### Duas armadilhas operacionais

**A criação de índice é assíncrona, e retorna antes de o índice existir.** A instrução devolve um identificador de trabalho e termina; o índice é construído depois. Um passo de migração que apenas execute a instrução termina com sucesso, a implantação prossegue e a aplicação entra em serviço consultando por varredura sequencial — a degradação aparece no primeiro tráfego real, no instante da implantação. O passo de migração precisa **aguardar a conclusão do trabalho** e reprovar a implantação se ele falhar.

**A credencial de acesso é de curta duração e a conexão tem tempo de vida limitado.** A credencial derivada da identidade da carga de trabalho expira em minutos, e a conexão é encerrada pelo motor após cerca de uma hora. Construir a cadeia de conexão uma única vez, no registro de dependências, faz toda conexão física aberta depois da expiração falhar — e como o conjunto inicial funciona, o defeito só aparece depois do aquecimento, em produção. A credencial precisa ser **renovada continuamente**, e não construída uma vez: a fonte de dados usa um provedor periódico que gera um novo token de quinze minutos a cada dez, com margem de cinco antes da expiração, e o tempo de vida das conexões carrega variação aleatória, para que não expirem todas juntas.

### Consequência para a estratégia de testes

A ausência de bloqueio pessimista e o isolamento por *snapshot* não são reproduzíveis em PostgreSQL convencional. Os testes que dependem de semântica de concorrência precisam ser executados contra o motor real, e o ambiente local serve para o resto.

## Condição de reversão

O portão acima foi o critério, e ele passou. A decisão está mantida.

Ela permanece reversível a custo baixo, e isso é propriedade do desenho, não sorte: as duas garantias que importam — a transação que grava lançamento e registro de saída juntos, e a chave primária que sustenta a idempotência — existem em qualquer motor relacional. Trocar o motor alcançaria a camada de infraestrutura e nenhuma outra.

Executar o portão antes de escrever a primeira linha de aplicação custou algumas horas e evitou descobrir a restrição de índices no meio da implementação, com código já apoiado nela.

## Requisitos afetados

- [RNF-002 — Capacidade de consulta em dias de pico](../requisitos/transversais/rnf-002-capacidade-de-consulta-do-consolidado.md)
- [RNF-003 — Segurança de acesso e de transporte](../requisitos/transversais/rnf-003-seguranca-de-acesso-e-transporte.md)
- [RNF-004 — Recuperabilidade e convergência do consolidado](../requisitos/transversais/rnf-004-recuperabilidade-e-convergencia-do-consolidado.md)
