---
type: vista
title: Vista de contexto (C4 nível 1)
description: O sistema como caixa preta, seus atores e os sistemas externos com que interage.
status: ativo
ponto_de_vista: Contexto
preocupacoes: [P2, P5]
stakeholders: [Comerciante]
---

# Vista de contexto

## Preocupações enquadradas

- **P2** — o registro de lançamentos continua funcionando quando a apuração falha.
- **P5** — o acesso é autenticado e os dados protegidos em trânsito.

## Diagrama

```mermaid
flowchart TB
    comerciante["Comerciante<br/>[Pessoa]<br/>Registra as entradas e saídas do dia<br/>e acompanha o saldo consolidado"]
    sistema["Sistema de fluxo de caixa diário<br/>[Sistema de software]<br/>Registra lançamentos de crédito e débito<br/>e apura o saldo diário consolidado"]
    idp["Provedor de identidade<br/>[Sistema externo]<br/>Autentica o comerciante e emite<br/>as credenciais de acesso"]

    comerciante -->|"Autentica-se<br/>[HTTPS]"| idp
    comerciante -->|"Registra lançamentos e consulta<br/>o saldo consolidado<br/>[HTTPS]"| sistema
    sistema -->|"Valida a credencial apresentada<br/>[HTTPS]"| idp

    classDef pessoa fill:#08427b,stroke:#052e56,color:#ffffff
    classDef interno fill:#1168bd,stroke:#0b4884,color:#ffffff
    classDef externo fill:#8b8b8b,stroke:#5f5f5f,color:#ffffff
    class comerciante pessoa
    class sistema interno
    class idp externo
```

## Descrição

O sistema tem **um** ator humano. Essa constatação é a primeira decisão de escopo e sustenta várias omissões documentadas em [escopo e limites](../../escopo-e-limites.md): não há hierarquia de permissões porque não há segundo papel, e não há particionamento por cliente porque não há segundo cliente.

A autenticação é delegada a um provedor de identidade externo. O sistema não armazena senha, não implementa recuperação de acesso e não gerencia ciclo de vida de credencial. Ele verifica a credencial apresentada e extrai dela a identidade do portador.

## Fronteira do sistema

| Dentro | Fora |
|---|---|
| Registro e consulta de lançamentos | Autenticação e gestão de identidade |
| Apuração e consulta do saldo diário | Conciliação bancária e meios de pagamento |
| Interface de uso | Emissão de documentos fiscais |

Os itens da coluna direita não são lacunas. São fronteiras, e as que poderiam ser confundidas com omissão estão tratadas em [evolucao/roadmap.md](../../evolucao/roadmap.md).

## Decomposição

A vista seguinte, [C2 — Contêineres](c2-conteineres.md), abre a caixa preta e mostra que ela contém dois serviços independentes, e por quê.
