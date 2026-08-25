---
type: requisito
id: RNF-006
title: Integridade da cadeia de dependências
classe: nao-funcional
caracteristica_qualidade: Segurança / Integridade
origem: derivado do objetivo da especificação
padrao_ears: unwanted-behaviour
verificacao: verificacao-de-construcao, inspecao
status: aprovado
vistas: [arquitetura/vistas/vista-de-implantacao.md]
decisoes: []
---

# RNF-006 — Integridade da cadeia de dependências

## Origem

Derivado do mesmo trecho do objetivo da especificação que origina [RNF-003](rnf-003-seguranca-de-acesso-e-transporte.md): *"proteja os dados e sistemas contra ameaças"*. Separado dele porque tem sujeito, momento de verificação e mecanismo distintos — o RNF-003 protege o acesso em execução, este protege o que entra no artefato antes de existir execução.

## Declaração

**Se** uma dependência incorporada ao sistema, direta ou transitiva, tiver vulnerabilidade conhecida publicada de severidade **moderada ou superior**, **então** a construção do artefato **deve** falhar.

## Racional do limiar

Um limiar que reprovasse qualquer severidade seria inaplicável na prática: alertas de severidade baixa são publicados contra dependências transitivas com frequência, e reprovariam a construção de quem clonasse o repositório sem que uma linha tivesse mudado. Um requisito que ninguém consegue manter verde deixa de ser verificado e passa a ser contornado.

Severidade baixa permanece **visível como aviso**, e não silenciada. O que muda é quem decide: severidade baixa é decisão de quem mantém; moderada ou acima é bloqueio.

## Critérios de aceitação

- **Se** uma dependência, direta ou transitiva, tiver vulnerabilidade conhecida de severidade moderada ou superior, **então** a construção falha.
- Vulnerabilidade de severidade baixa é emitida como aviso visível, sem bloquear.
- As versões das dependências são declaradas em um único lugar, de modo que dois projetos não possam divergir silenciosamente.
- Avisos dos analisadores estáticos são tratados como erro na construção.

Os quatro critérios são realizados por `back/Directory.Build.props` e `back/Directory.Packages.props`, e por nada além deles.

## O que este requisito não cobre

O requisito foi escrito mais largo do que o mecanismo que o realiza, e a diferença precisa estar nomeada:

- **Não há checagem de dependência descontinuada.** Nenhuma propriedade de construção a ativa.
- **Não há alerta automático de vulnerabilidade** no repositório, nem varredura de segredos, nem bloqueio de submissão que contenha um. São configurações de plataforma que não foram feitas.
- **A integração contínua cobre construção e suíte, não a cadeia.** O fluxo em `.github/workflows/ci.yml` executa restauração, construção e testes a cada envio, o que faz a auditoria de vulnerabilidade e os analisadores rodarem fora da máquina de quem escreve. Não há, porém, alerta automático de vulnerabilidade no repositório nem varredura de segredos.

A consequência é que a janela entre construções fica descoberta, e a verificação depende de alguém construir. É lacuna conhecida, e a mais barata de fechar da lista.

## Racional da integridade da cadeia

A especificação pede proteção contra ameaças, e dependência vulnerável é vetor de ameaça como qualquer outro — com o agravante de que entra pela porta da frente, autorizada, e não aparece em teste funcional.

O critério é verificado na construção, e não por revisão periódica, porque a revisão periódica descobre o problema em média meio ciclo depois de ele existir. Falhar a construção move a descoberta para o instante em que a dependência entra — para quem constrói, ao menos, que hoje é o único momento em que a verificação acontece.

## Verificação

A auditoria de dependências e os analisadores rodam na construção local. Inspeção de `back/Directory.Build.props` e `back/Directory.Packages.props` confirma os limiares e a centralização das versões.

```bash
cd back && dotnet list package --vulnerable --include-transitive
```

## Rastreabilidade

- Vista: [Implantação](../../arquitetura/vistas/vista-de-implantacao.md)
- Requisito relacionado: [RNF-003](rnf-003-seguranca-de-acesso-e-transporte.md)
