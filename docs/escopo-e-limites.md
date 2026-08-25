---
type: escopo
title: Escopo, limites e critérios de suficiência
description: O que foi deliberadamente construído, o que foi deliberadamente omitido, e o critério usado para decidir entre os dois.
status: ativo
tags: [escopo, limites, decisoes]
---

# Por que este documento existe

Todo sistema acumula mecanismo que ninguém pediu. Malha de serviços, *event sourcing*, bancos de leitura e escrita segregados, rastreamento distribuído completo — cada um resolve um problema real, e nenhum deles é problema **deste** sistema. Adotá-los aqui acrescentaria peças a manter, entender e operar, sem que nenhum requisito passasse a ser atendido.

A omissão, porém, precisa de critério. Sem ele, "não fizemos" e "não era necessário" ficam indistinguíveis, e a diferença entre as duas coisas é justamente o que sustenta uma arquitetura.

Este documento registra a linha que separa o necessário do excedente, e o critério que a define.

# Critério de suficiência

Um mecanismo entra no projeto se, e somente se, atender a um destes três testes:

1. **Deriva de um requisito da especificação.** Existe um RF ou RNF em `requisitos/` que o exige.
2. **É pré-condição de um requisito obrigatório.** Sem ele, um requisito declarado não se sustenta.
3. **É custo marginal de uma decisão já tomada.** Não adiciona superfície nova.

Um mecanismo que não passa em nenhum dos três vai para `evolucao/roadmap.md`, onde é descrito com o rationale de por que foi adiado. Registrar o adiamento com a condição que o reverteria é o que impede que a omissão vire dívida esquecida.

# O que foi construído, e sob qual autoridade

| Mecanismo | Teste | Requisito de origem |
|---|---|---|
| Dois serviços em unidades de implantação separadas | 1 | RNF de isolamento de falha: o Lançamentos não pode cair junto com o Consolidado |
| Fila entre os serviços, sem chamada síncrona | 2 | Pré-condição do isolamento. Uma chamada síncrona acoplaria a disponibilidade dos dois. Publicador e consumidor existem em código; a fila existe no ambiente local, sobre um emulador, e **não** está provisionada em nuvem |
| Consumo idempotente da projeção | 2 | Entrega *at-least-once* da fila implica reprocessamento; sem idempotência o saldo fica errado |
| Consolidado como projeção materializada | 1 | RNF de capacidade: 50 req/s de leitura resolvidos por leitura direta, sem agregação em tempo de consulta |
| Clean Architecture com camadas explícitas | 1 | Requisito obrigatório de boas práticas e padrões de arquitetura |
| DDD tático no agregado de Lançamento | 1 | Requisito de boas práticas, onde há invariante real a proteger |
| Testes unitários, de integração e de arquitetura | 1 | Requisito obrigatório de testes |
| Descrição arquitetural com vistas C4 | 1 | Requisito obrigatório de desenho da solução |
| ADRs | 1 | Requisito obrigatório de documentação de projeto |
| Outbox transacional | 2 | Ver a discussão dedicada abaixo |

O **autoescalonamento horizontal do Consolidado** aparecia nesta tabela e saiu dela: ele é desenho da [vista de implantação](arquitetura/vistas/vista-de-implantacao.md), não coisa construída. Nada no Terraform provisiona política de escala, e a razão pela qual ele nunca foi mecanismo exigido pelo requisito está na mesma vista — o dimensionamento mínimo é que precisa absorver o pico.

## As duas exceções declaradas

Dois itens deste repositório **não passam** nos três testes acima, e estão aqui assim mesmo: a infraestrutura como código e o front-end.

Nenhum requisito deixa de se sustentar sem eles. Entram por decisão de quem conduz o projeto — a infraestrutura para tornar a vista de implantação confrontável com código, a interface para tornar os dois serviços operáveis sem cliente HTTP.

São exceções, e estão nomeadas como exceções. A alternativa seria alargar o critério até que os acomodasse, e um critério que se ajusta ao que já se decidiu fazer não filtra nada.

## O caso do outbox

É o único mecanismo cuja inclusão exige defesa, porque é o que está mais próximo da linha — e a defesa está numa distinção que a margem de perda obriga a fazer.

A especificação estabelece margem de perda de 5% para o Consolidado. Essa margem é aceitável para **requisição de leitura**, recuperável por nova consulta, e não é aceitável para **evento de escrita**, cuja perda é irrecuperável e silenciosa. O outbox marca essa fronteira, ao custo de uma tabela e um publicador em segundo plano. O mecanismo e as alternativas estão em [ADR 0003](decisoes/0003-outbox-transacional-e-consumo-idempotente.md).

# O que foi deliberadamente omitido

| Omitido | Por que não passa no critério |
|---|---|
| Event sourcing | O consolidado diário é derivável dos lançamentos por soma. O histórico de eventos não é requisito, e o custo em complexidade de leitura, versionamento e reconstrução não tem contrapartida na especificação |
| CQRS com bancos de leitura e escrita segregados | A projeção materializada já separa o modelo de leitura do de escrita. Segregar a infraestrutura adicionaria replicação e consistência eventual interna sem requisito que a exija |
| Kubernetes ou service mesh | Dois serviços. O orquestrador de contêineres gerenciado atende, e a malha resolveria um problema de topologia que não existe aqui |
| Multi-tenancy | A especificação descreve **um** comerciante. Particionar por *tenant* seria inventar requisito |
| Rastreamento distribuído completo | Com dois serviços e um salto assíncrono, a correlação por identificador de requisição propagado no envelope da mensagem entrega o mesmo diagnóstico |
| API Gateway com cache, WAF e planos de uso | O controle de vazão no gateway é o que o RNF de capacidade pede. O resto é superfície de configuração sem requisito |
| *Saga* ou coordenação distribuída | Não há transação de negócio abrangendo os dois serviços. O consolidado é consequência, não participante |
| Versionamento de API além do prefixo de rota | Não há consumidor externo estabelecido para preservar |
| Autorização por papel ou permissão fina | O sistema tem um ator. A autenticação é verificada no serviço, e no gateway do desenho; a autorização granular não tem sujeito |
| *Runbook* operacional e alertas de plantão | Não há operação. As verificações de saúde existem porque a orquestração de contêineres as exige; os alarmes de fila estão especificados em [metas e métricas](qualidade/metas-e-metricas.md) e não construídos |
| Conciliação bancária, meios de pagamento, relatórios além do consolidado | Fora do descritivo da solução |

Seis destes itens — event sourcing, segregação de bancos de leitura e escrita, orquestração autogerida, particionamento por cliente, coordenação distribuída e versionamento de contrato — **não** estão em `evolucao/roadmap.md`, e a ausência é deliberada: eles não resolvem problema que este sistema tenha, e listá-los como adiados sugeriria o contrário. Os demais estão lá, com a condição que os traria para dentro do escopo.

# Limites das camadas

A simetria entre os dois serviços é intencionalmente incompleta: quatro camadas no Lançamentos, três no Consolidado. É decisão, não inconsistência, e aplicar o mesmo número aos dois produziria um repositório mais simétrico e um projeto pior. O argumento está em [ADR 0005](decisoes/0005-clean-architecture-e-ddd-tatico.md).

# Uma duplicação deliberada

O migrador de esquema, a fábrica de fonte de dados e a unidade de trabalho existem **duas vezes**, uma em cada serviço. São cerca de duzentas linhas de encanamento repetidas, e um revisor tem razão de notar.

A alternativa seria uma biblioteca de infraestrutura compartilhada. Ela criaria a única coisa que este projeto existe para evitar: uma dependência de compilação entre os dois serviços, que passariam a ser versionados e implantados em conjunto. Duzentas linhas de encanamento duplicado custam menos do que desfazer o isolamento que sustenta o requisito central da especificação.

Há também duplicação de **regra**, e negá-la seria fácil de desmentir: valor positivo, teto de valor e limite de duas casas decimais existem em `Money`, no domínio de Lançamentos, e de novo em `DailyBalance`, na aplicação do Consolidado, com as mesmas constantes.

Isso não é descuido. O Consolidado recebe lançamentos por uma fila, e mensagem que chega por fila não é entrada confiável: pode vir de uma versão anterior do contrato, de um produtor com defeito, ou corrompida no caminho. Um serviço que incorpora ao saldo o que quer que apareça na fila não tem invariante — tem esperança. A validação na fronteira do Consolidado é o que garante que o saldo permanece íntegro mesmo quando o produtor erra.

O custo é real e vale nomear: os dois conjuntos de limites precisam ser mantidos em acordo, e nada no repositório force isso. A alternativa — extrair os limites para o contrato compartilhado — criaria a dependência de compilação que o isolamento existe para evitar, e faria uma mudança de regra no Lançamentos exigir reimplantação do Consolidado. Entre um acoplamento que o requisito central proíbe e dois números a manter iguais, a escolha é a segunda.

# Limites da infraestrutura

O Terraform está no repositório porque a vista de implantação precisa ser verificável — um desenho de arquitetura que afirma escalabilidade horizontal e isolamento de falha deve poder ser confrontado com o código que os provisiona.

O limite é por camada, não por comando. A **camada de persistência foi efetivamente aplicada** numa conta real, porque validar a decisão de persistência exigia um motor de verdade — o resultado está em [ADR 0006](decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md), e ele derrubou quatro suposições que nenhum `plan` teria revelado.

As demais camadas — rede, execução, borda, identidade e distribuição do front-end — **não existem no Terraform**. O que está escrito provisiona os dois agrupamentos de persistência e o parâmetro com o endereço de cada um, e nada mais. A [vista de implantação](arquitetura/vistas/vista-de-implantacao.md) descreve o desenho dessas camadas; confrontá-lo com código, no caso delas, ainda não é possível.

# Limite do front-end

A interface não é requisito. Ela existe para tornar os dois serviços operáveis sem um cliente HTTP à mão, e por isso é mantida em duas telas: registrar e listar lançamentos, consultar o saldo diário. Não há framework, etapa de construção nem dependência de terceiros — é HTML, CSS e JavaScript servidos por um nginx que também repassa as duas APIs, de modo que tudo seja mesma origem e não haja CORS a configurar. Zero dependência significa zero cadeia de suprimentos a auditar. A renderização usa apenas `textContent`, nunca `innerHTML`, porque a descrição do lançamento é texto livre do usuário.

# Como este documento deve ser lido

Se um mecanismo esperado estiver ausente, a ausência é deliberada e está justificada aqui ou em `evolucao/roadmap.md`. Se algo estiver presente sem passar nos três testes do critério de suficiência, é um erro de julgamento meu e a crítica é legítima.
