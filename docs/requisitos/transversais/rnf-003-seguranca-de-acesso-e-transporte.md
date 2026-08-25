---
type: requisito
id: RNF-003
title: Segurança de acesso e de transporte
classe: nao-funcional
caracteristica_qualidade: Segurança / Confidencialidade e Autenticidade
origem: derivado do objetivo da especificação
padrao_ears: ubiquitous
verificacao: teste-integracao, inspecao
status: aprovado
vistas: [arquitetura/vistas/c2-conteineres.md, arquitetura/vistas/vista-de-implantacao.md]
decisoes: [decisoes/0006-persistencia-em-aurora-dsql-com-ef-core.md, decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md, decisoes/0008-terraform-com-stack-unico-e-toggles.md]
---

# RNF-003 — Segurança de acesso e de transporte

## Origem

Derivado do objetivo da especificação, que inclui *"Segurança: proteja os dados e sistemas contra ameaças. Implemente autenticação, autorização, criptografia e mecanismos de proteção contra ataques."*

## Declaração

O sistema **deve** exigir identidade autenticada para toda operação de negócio e proteger os dados em trânsito e em repouso.

## Critérios de aceitação

- Toda requisição às operações de negócio apresenta credencial válida emitida pelo provedor de identidade. A verificação ocorre na borda, antes de alcançar os serviços.
- **Se** a credencial estiver ausente, expirada ou com assinatura inválida, **então** a requisição é rejeitada antes de qualquer efeito.
- As verificações de saúde são as únicas rotas sem exigência de credencial, e não expõem dado de negócio.
- Todo tráfego externo trafega sobre canal cifrado. Requisições em canal não cifrado são redirecionadas.
- Os serviços não são endereçáveis diretamente pela internet: a única entrada é a borda.
- Os dados em repouso são cifrados pelo mecanismo do serviço gerenciado de persistência.
- Nenhum segredo é gravado em código-fonte, em imagem de contêiner ou em variável de ambiente em texto claro.
- O acesso à persistência usa credencial de curta duração derivada da identidade da carga de trabalho, não senha estática.
- Registros de execução não contêm credenciais, tokens ou dados que identifiquem o portador da credencial.

## Sobre autorização

A especificação pede autenticação **e** autorização. Este sistema tem **um ator** — o comerciante, sobre os próprios lançamentos. Autorização é a decisão sobre qual sujeito pode qual operação sobre qual recurso, e um sistema com um sujeito e nenhum recurso alheio não tem essa decisão a tomar. Modelar papéis aqui produziria uma matriz de permissões de uma linha por uma coluna, e um leitor razoável a interpretaria como cerimônia.

O que existe é a fronteira que a autorização usaria: as rotas de negócio exigem princípio autenticado por política nomeada, e acrescentar uma exigência de papel ou de escopo é alterar essa política num lugar só. A condição que traria autorização para dentro do escopo é a primeira em que aparecer um segundo sujeito — mais de um comerciante, ou um perfil de leitura para contabilidade — e ela está em [roadmap](../../evolucao/roadmap.md).

## Sobre proteção contra ataques

A especificação nomeia "mecanismos de proteção contra ataques". Vale separar o que protege hoje do que não existe, porque a diferença é grande.

**Protege hoje:** toda consulta à persistência é parametrizada ou construída pelo mapeador, sem concatenação de entrada em SQL — o único SQL literal é o das migrações e o dos roteiros de verificação de saúde, ambos sem entrada externa. A interface renderiza exclusivamente por `textContent`, nunca por injeção de marcação, e a descrição do lançamento é texto livre do usuário, portanto é o vetor real. A política de conteúdo servida com a interface restringe origem de script, estilo e conexão à própria origem. Nenhuma política de compartilhamento entre origens é registrada, então o navegador bloqueia leitura cruzada por padrão. O corpo das respostas de erro é genérico para falha inesperada, sem vazar rastro de pilha, nome de tabela ou cadeia de conexão.

**Não existe:** controle de vazão, firewall de aplicação, limite de tamanho de requisição, redirecionamento para canal cifrado e cabeçalho de transporte estrito. Os quatro primeiros pertencem à borda, que não está provisionada; os dois últimos exigem terminação de canal cifrado, que também é da borda. Nenhum deles é substituível por código de serviço sem duplicar na aplicação o que a borda existe para concentrar — o argumento está em [ADR 0007](../../decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md).

A ausência mais relevante não é nenhuma dessas: é que **não há limite de tentativas de autenticação**, porque não há autenticação de usuário neste sistema — a credencial é emitida por provedor externo, e a proteção contra força bruta é responsabilidade dele.

## Verificação

Testes de integração cobrindo requisição sem credencial, com credencial expirada e com assinatura inválida. Inspeção da configuração de rede confirmando que os serviços só recebem tráfego da borda, e do código confirmando ausência de segredo versionado.

## Rastreabilidade

- Vistas: [C2 — Contêineres](../../arquitetura/vistas/c2-conteineres.md), [Implantação](../../arquitetura/vistas/vista-de-implantacao.md)
- Requisito relacionado: [RNF-006](rnf-006-integridade-da-cadeia-de-dependencias.md)
- Decisão: [0007](../../decisoes/0007-borda-com-api-gateway-cognito-e-fargate.md)
