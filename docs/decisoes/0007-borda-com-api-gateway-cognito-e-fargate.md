---
type: decisao
id: ADR-0007
title: Expor os serviços por gateway de API com autorizador de credencial, executando-os em contêineres gerenciados
status: aceita
data: 2026-08-18
tags: [borda, autenticacao, contêineres, escala]
requisitos_afetados: [RNF-002, RNF-003]
---

# ADR 0007 — Expor os serviços por gateway de API com autorizador de credencial, executando-os em contêineres gerenciados

> **Estado.** Esta decisão está **parcialmente implementada**. A validação de credencial existe e roda nos dois serviços; o gateway, o provedor de identidade, o balanceador interno e as tarefas em sub-rede privada **não estão provisionados** — o Terraform do repositório provisiona apenas a camada de persistência. As consequências abaixo descrevem o desenho decidido, não o estado atual da infraestrutura.

## Contexto

Os dois serviços precisam ser alcançáveis pela interface web, com acesso autenticado, sem que fiquem diretamente expostos à internet. O serviço de Consolidado precisa absorver um pico de leitura sem degradar além do teto de perda estabelecido.

Ambos são interfaces de longa duração, e o de Consolidado hospeda no mesmo processo o consumidor contínuo da fila.

## Alternativas consideradas

**Serviços expostos diretamente por balanceador público.** Menos peças no caminho. Cada serviço passaria a implementar por conta própria a validação da credencial e o controle de vazão, duplicando lógica de segurança em dois lugares que precisariam ser mantidos em acordo — e cujo desacordo seria silencioso.

**Funções sob demanda.** Escala elástica sem gestão de capacidade. Não acomodam bem um consumidor contínuo residente no processo, e exigiriam separá-lo em unidade própria, revertendo a decisão de [ADR 0001](0001-decomposicao-em-dois-servicos.md) de não criar terceira unidade de implantação. O modelo de execução também diferiria do ambiente local, enfraquecendo a equivalência entre o que se testa e o que se implanta.

**Contêineres em orquestrador autogerido.** Controle total sobre o agendamento. Introduz um plano de controle a operar e a manter, cuja complexidade não é exigida por dois serviços.

**Gateway de API com autorizador, à frente de contêineres em serviço gerenciado.** O gateway centraliza a validação da credencial e o controle de vazão; os contêineres executam interfaces de longa duração de forma idêntica localmente e em nuvem.

## Decisão

Expor os dois serviços por um gateway de API, com autorizador que valida a assinatura e a validade da credencial emitida pelo provedor de identidade antes de qualquer encaminhamento.

Executar os serviços em contêineres gerenciados, em sub-redes privadas, alcançáveis apenas pelo gateway por integração privada com um balanceador interno.

Aplicar o controle de vazão no gateway, rejeitando o excedente de forma explícita sob sobrecarga.

Escalar o serviço de Consolidado por número de tarefas, com a política reagindo à quantidade de requisições por tarefa.

As verificações de saúde são as únicas rotas sem exigência de credencial, e não expõem dado de negócio.

## Consequências

A validação da credencial acontece uma vez, na borda, e os serviços confiam na identidade recebida. Isso elimina a duplicação da lógica de segurança e concentra em um ponto a decisão sobre quem entra.

Não existe caminho de rede que alcance uma tarefa sem atravessar o gateway, e portanto não existe caminho que contorne a validação. É o que realiza o critério de não endereçabilidade direta do requisito de segurança.

Rejeitar o excedente de forma explícita, em vez de deixar a carga chegar aos serviços, troca latência crescente e falhas por esgotamento de tempo por rejeições imediatas e legíveis. As duas consomem orçamento de erro; só a primeira o consome sem indicar a causa.

A escala horizontal por número de tarefas só funciona porque o serviço não mantém estado local entre requisições. Essa propriedade passa a ser uma restrição de projeto: introduzir estado em memória compartilhado entre requisições invalidaria o mecanismo de escala.

Manter o consumidor no mesmo processo faz o consumo escalar junto com a leitura. Várias tarefas consumindo a mesma fila podem receber a mesma mensagem, e isso é seguro apenas porque a incorporação é idempotente — a decisão depende de [ADR 0003](0003-outbox-transacional-e-consumo-idempotente.md).

O gateway acrescenta um salto no caminho da requisição, e sua indisponibilidade alcança os dois serviços. É a contrapartida aceita por ter um único ponto de aplicação das políticas de acesso.

## Emenda de 2026-08-24 — verificação também no serviço

A decisão original concentrava toda a verificação de credencial no gateway, e os serviços confiavam na identidade recebida. Isso deixava dois problemas práticos.

O primeiro é de demonstração: o gateway é infraestrutura, e o ambiente local não o tem. Um serviço que não verifica nada sobe, localmente, como uma API inteiramente aberta — e é assim que ele é lido por quem executa o repositório.

O segundo é de verificação: os critérios de aceitação do [RNF-003](../requisitos/transversais/rnf-003-seguranca-de-acesso-e-transporte.md) exigem ensaio de credencial ausente, expirada e com assinatura inválida. Sem verificação no serviço, esses ensaios só existiriam contra infraestrutura provisionada, fora do alcance da suíte.

Os serviços passam a validar a credencial também na sua própria borda. O gateway continua sendo o ponto único de **política** — controle de vazão, terminação de canal cifrado, quais rotas são públicas — e deixa de ser o ponto único de **verificação**.

Isso não reabilita a alternativa rejeitada acima. O que foi rejeitado era expor os serviços diretamente e fazer de cada um o único responsável pela sua segurança, com dois pontos de política a manter em acordo. Aqui a política segue num lugar só; o que se duplica é a checagem, deliberadamente, e uma checagem redundante que discorda da outra rejeita a requisição em vez de admiti-la.

A verificação falha fechada: sem autoridade de identidade configurada, o serviço **recusa-se a iniciar**. Um serviço que subisse aberto por configuração ausente seria pior que a ausência de verificação, porque pareceria protegido. O único modo de operar sem credencial é explícito e nomeado, existe para a execução local, e está documentado no README.

O custo é uma segunda validação por requisição, com a chave pública em cache pelo processo, e a necessidade de manter as duas configurações de audiência em acordo.

## Requisitos afetados

- [RNF-002 — Capacidade de consulta em dias de pico](../requisitos/transversais/rnf-002-capacidade-de-consulta-do-consolidado.md)
- [RNF-003 — Segurança de acesso e de transporte](../requisitos/transversais/rnf-003-seguranca-de-acesso-e-transporte.md)
