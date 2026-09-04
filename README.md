# NotificationsAPI (Serverless)

Microsserviço de notificações da FIAP Cloud Games (FCG) — Tech Challenge Fase 3, frente Serverless.

## Finalidade

O `NotificationsAPI` **simula o envio de e-mails** (via logging estruturado, sem integração real com SMTP) em resposta a eventos publicados por outros microsserviços:

- **Cadastro de usuário**: consome `UserRegisteredEvent` (publicado pelo `UsersAPI`) e "envia" um e-mail de boas-vindas.
- **Compra de jogo**: consome `PaymentProcessedEvent` (publicado pelo `PaymentsAPI`). Se o status do pagamento for `Approved`, "envia" um e-mail de confirmação de compra; caso contrário, nenhum e-mail é enviado.

O envio simulado está implementado em `NotificationsAPI/src/Notifications.Infrastructure/Email/EmailService.cs`, que apenas registra a notificação via logging estruturado.

## Arquitetura

Na Fase 2 este serviço era uma API HTTP + worker consumindo RabbitMQ, rodando 24/7 em container. Na Fase 3 ele foi **migrado para serverless**: não há mais processo/container rodando continuamente — o código só executa quando um evento chega.

```
UsersAPI  ──publica──▶  SNS fcg-user-events    ──▶  SQS fcg-notifications-user-registered   ──▶  Lambda UserRegisteredFunction
PaymentsAPI ─publica──▶  SNS fcg-payment-events ──▶  SQS fcg-notifications-payment-processed ──▶  Lambda PaymentProcessedFunction
```

Cada Lambda persiste o resultado no DynamoDB (tabela `fcg-notifications`) e "envia" o e-mail simulado via log estruturado.

Clean Architecture em 3 camadas + o novo host serverless:

```
NotificationsAPI/
├── src/Notifications.Domain/          Regras de negócio (aggregate Notification)
├── src/Notifications.Application/     Casos de uso e handlers de eventos
├── src/Notifications.Infrastructure/  Persistência (DynamoDB) e email simulado
└── src/Notifications.Functions/       Host Lambda: handlers, DI (Startup.cs), OpenTelemetry (Telemetry.cs)
```

Infraestrutura como código: [`template.yaml`](template.yaml) (AWS SAM — Lambdas, filas SQS, tópicos SNS, tabela DynamoDB) e [`newrelic-integration.yaml`](newrelic-integration.yaml) (CloudFormation — IAM Role para a integração nativa AWS↔New Relic). Ver decisões detalhadas em [`SDD.md`](SDD.md).

## Stack tecnológico

- .NET 10 (Lambda runtime `dotnet10`)
- AWS SAM (IaC)
- Amazon SNS + SQS (mensageria, substitui o RabbitMQ da Fase 2)
- Amazon DynamoDB (persistência)
- AWS Secrets Manager (license key do New Relic)
- New Relic (observabilidade): traces via OpenTelemetry/OTLP a partir da própria função, métricas nativas da Lambda/DynamoDB/SNS/SQS via integração AWS→New Relic (API polling), logs via CloudWatch Logs

## Observabilidade (New Relic)

Duas frentes complementares, sem precisar de nenhum agente/container rodando 24/7:

1. **Traces da função**: `Notifications.Functions/Telemetry.cs` configura o OpenTelemetry SDK para exportar spans via OTLP para o endpoint da New Relic, usando a license key resolvida do Secrets Manager (`NEW_RELIC_LICENSE_KEY`, ver `template.yaml`).
2. **Métricas de infraestrutura** (Lambda `Duration`/`Errors`/`Throttles`/`Invocations`, além de DynamoDB/SNS/SQS): coletadas nativamente pela New Relic via *API polling* do CloudWatch, sem qualquer código ou agente na função. Isso é feito através de uma IAM Role (`NewRelicInfrastructure-Integrations`, definida em [`newrelic-integration.yaml`](newrelic-integration.yaml)) que a conta da New Relic assume via `sts:AssumeRole`, e da integração configurada na conta New Relic do grupo (NerdGraph `cloudLinkAccount` + `cloudConfigureIntegration`, escopo: Lambda, DynamoDB, SNS, SQS).
3. **Logs**: cada Lambda escreve em stdout (Serilog/`AddSimpleConsole`), que o próprio runtime da Lambda já encaminha automaticamente para o CloudWatch Logs.

## Variáveis de ambiente (Lambda)

| Variável | Obrigatória | Origem | Descrição |
|---|---|---|---|
| `DynamoDb__TableName` | Não | `template.yaml` (Globals) | Nome da tabela de notificações (`fcg-notifications`) |
| `NEW_RELIC_LICENSE_KEY` | Sim (para traces) | Secrets Manager, resolvido no deploy | License key usada pelo exporter OTLP em `Telemetry.cs` |
| `DynamoDb__ServiceUrl` | Não | — | Endpoint do DynamoDB Local, só em desenvolvimento/testes locais |

## Como rodar/testar localmente

```bash
cd NotificationsAPI

# Build e testes da solução .NET
dotnet build
dotnet test
```

Para simular a execução da Lambda localmente (requer Docker e AWS SAM CLI), na raiz do repositório:

```bash
sam build
sam local invoke UserRegisteredFunction --event events/user-registered.json --env-vars env.local.json
```

## Deploy

```bash
sam build
sam deploy --no-confirm-changeset
```

O `samconfig.toml` já traz os parâmetros padrão de deploy (stack `fcg-notifications-serverless`, região, etc.). Após o deploy, os ARNs dos tópicos SNS e o nome da tabela DynamoDB são exibidos como *Outputs* do CloudFormation — são esses ARNs que `UsersAPI` e `PaymentsAPI` devem usar para publicar os eventos.

Para (re)aplicar a integração de métricas com a New Relic (role IAM):

```bash
aws cloudformation deploy --template-file newrelic-integration.yaml --stack-name fcg-newrelic-integration --capabilities CAPABILITY_NAMED_IAM
```
