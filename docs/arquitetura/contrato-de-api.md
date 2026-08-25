---
type: contrato
title: Contrato da API
description: Superfície HTTP dos dois serviços, formato de erro e mapeamento entre falha de domínio e resposta.
status: ativo
estado: implementado
---

# Contrato da API

Os dois serviços expõem caminhos distintos sob o mesmo prefixo de versão, `/api/v1`. No desenho eles são alcançados pelo mesmo domínio, atrás do gateway da [vista de implantação](vistas/vista-de-implantacao.md); hoje o gateway não está provisionado, e cada serviço responde no seu próprio endereço — no ambiente local, atrás do nginx que serve a interface e repassa os dois.

Toda operação de negócio exige credencial válida, conforme [RNF-003](../requisitos/transversais/rnf-003-seguranca-de-acesso-e-transporte.md). As verificações de saúde são as únicas rotas sem credencial.

## Lançamentos

### Registrar lançamento

```
POST /api/v1/entries
```

```json
{
  "type": "credit",
  "amount": 150.75,
  "competenceDate": "2026-08-19",
  "description": "venda do dia"
}
```

| Campo | Regra |
|---|---|
| `type` | `credit` ou `debit`. Obrigatório |
| `amount` | Maior que zero, no máximo duas casas decimais. O sinal vem do tipo, nunca do valor |
| `competenceDate` | Data sem horário, não posterior à data corrente no fuso do comerciante |
| `description` | Até 200 caracteres. Opcional |

**201 Created**, com `Location` apontando o recurso criado e o lançamento no corpo, incluindo `id` e `recordedAt`.

Realiza [RF-LAN-001](../requisitos/lancamentos/rf-lancamentos-001-registrar-lancamento.md). Não há operação de alteração nem de exclusão: o lançamento é imutável, e a correção se faz por lançamento compensatório.

### Consultar lançamento

```
GET /api/v1/entries/{id}
```

**200 OK** com o lançamento, ou **404 Not Found**. Realiza [RF-LAN-002](../requisitos/lancamentos/rf-lancamentos-002-consultar-lancamento.md).

### Listar lançamentos por período

```
GET /api/v1/entries?from=2026-08-01&to=2026-08-19&offset=0&limit=50
```

Intervalo fechado nos dois extremos. `limit` entre 1 e 200, padrão 50. Ordenação por data de competência e, dentro do mesmo dia, por data de registro.

**200 OK** com a página e o total do intervalo. Intervalo sem lançamentos devolve coleção vazia, não erro. Realiza [RF-LAN-003](../requisitos/lancamentos/rf-lancamentos-003-listar-lancamentos-por-periodo.md).

## Consolidado diário

### Consultar o saldo de uma data

```
GET /api/v1/daily-balances/{date}
```

```json
{
  "competenceDate": "2026-08-19",
  "totalCredits": 1200.00,
  "totalDebits": 340.50,
  "balance": 859.50,
  "entryCount": 14,
  "updatedAt": "2026-08-19T18:04:11Z"
}
```

**200 OK sempre** para data válida. Data sem movimento devolve totais zerados e `updatedAt` nulo — a ausência de movimento é resposta válida sobre o dia, não falha de localização. Realiza [RF-CON-003](../requisitos/consolidado/rf-consolidado-003-consultar-saldo-de-uma-data.md).

Esta é a operação submetida ao pico de [RNF-002](../requisitos/transversais/rnf-002-capacidade-de-consulta-do-consolidado.md). É atendida por leitura de uma linha já apurada.

### Consultar a série por período

```
GET /api/v1/daily-balances?from=2026-08-01&to=2026-08-19
```

Intervalo fechado, limitado a 366 dias. A série é **contínua**: dias sem movimento aparecem com totais zerados, para que nenhum consumidor precise preencher lacunas por conta própria. Realiza [RF-CON-004](../requisitos/consolidado/rf-consolidado-004-consultar-saldo-por-periodo.md).

## Verificações de saúde

| Rota | Verifica | Uso |
|---|---|---|
| `GET /health/live` | Apenas que o processo responde | Substituição de tarefa doente |
| `GET /health/ready` | Persistência, por consulta trivial ao banco | Entrada e saída do encaminhamento |

A separação é deliberada: uma verificação única que consultasse a persistência faria o orquestrador reiniciar tarefas saudáveis durante uma indisponibilidade do banco, transformando falha de dependência em falha de disponibilidade.

A prontidão **não** verifica o transporte de mensagens. Verifica só a persistência, que é a dependência do caminho de leitura; a indisponibilidade da fila não torna a consulta do saldo incapaz de responder, e retirar a tarefa do encaminhamento por causa dela trocaria uma degradação de apuração por uma indisponibilidade de leitura.

Nenhuma das duas expõe dado de negócio.

## Formato de erro

O corpo de toda falha segue a estrutura de *problem details* da RFC 9457. O tipo de mídia da resposta, porém, é `application/json`, e não `application/problem+json` — o corpo é o da norma, o cabeçalho ainda não.

```json
{
  "type": "https://cashflow/errors/invalid-amount",
  "title": "O valor do lançamento é inválido",
  "status": 400,
  "detail": "Entry amount must be greater than zero.",
  "instance": "/api/v1/entries",
  "correlationId": "01J8XG..."
}
```

O `correlationId` é o mesmo que atravessa a fila até a apuração, e é o que liga a resposta ao rastro nos registros de execução, conforme [RNF-005](../requisitos/transversais/rnf-005-analisabilidade-da-execucao.md).

Nenhuma resposta de erro expõe rastro de pilha, consulta ou detalhe de infraestrutura.

## Mapeamento entre falha e resposta

| Falha | Status |
|---|---|
| Regra de negócio violada — valor, tipo, competência, descrição | 400 |
| Parâmetro de consulta inválido — intervalo invertido, paginação fora do limite | 400 |
| Corpo da requisição que não é JSON válido | 400 |
| Credencial ausente, expirada ou com assinatura inválida | 401 |
| Lançamento inexistente | 404 |
| Falha não prevista | 500, com mensagem genérica |

O mapeamento vive na camada de API. **A camada de domínio não conhece código de status**: ela lança falhas de negócio, e traduzi-las é responsabilidade da borda — é o que permite que a mesma regra sirva a um consumidor que não fale HTTP.

Não há resposta 429 nesta tabela porque **não há controle de vazão**. Ele é previsto no gateway da [vista de implantação](vistas/vista-de-implantacao.md), que não está provisionado; enquanto não estiver, nenhuma requisição é rejeitada por excedente, nem pelo serviço nem por qualquer coisa à frente dele.

## Versionamento

O prefixo `/api/v1` é a única forma de versionamento. Não há negociação por cabeçalho nem versionamento por recurso, porque não há consumidor externo estabelecido a preservar — decisão registrada em [escopo e limites](../escopo-e-limites.md).

O contrato **entre os serviços** é outro e tem versionamento próprio, descrito na [vista de contêineres](vistas/c2-conteineres.md).
