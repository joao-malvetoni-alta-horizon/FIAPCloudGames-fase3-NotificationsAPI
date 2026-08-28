# NotificationsAPI

Microsserviço de notificações da FIAP Cloud Games (FCG) — Tech Challenge Fase 2.

## Finalidade

O `NotificationsAPI` é responsável por **simular o envio de e-mails** (via logging estruturado, sem integração real com SMTP) em resposta a eventos publicados por outros microsserviços através do RabbitMQ:

- **Cadastro de usuário**: consome o evento `UserRegisteredEvent` (publicado pelo `UsersAPI`, equivalente ao `UserCreatedEvent` do desafio) e "envia" um e-mail de boas-vindas.
- **Compra de jogo**: consome o evento `PaymentProcessedEvent` (publicado pelo `PaymentsAPI`). Se o status do pagamento for `Approved`, "envia" um e-mail de confirmação de compra; caso contrário, nenhum e-mail é enviado.

O envio simulado está implementado em `src/Notifications.Infrastructure/Email/EmailService.cs`, que apenas registra a notificação via logging estruturado (partial log methods com `[LoggerMessage]`).

## Arquitetura

Clean Architecture em 3 camadas:

```
NotificationsAPI/
├── src/Notifications.Domain/          Regras de negócio (aggregate Notification)
├── src/Notifications.Application/     Casos de uso e handlers de eventos
└── src/Notifications.Infrastructure/  Persistência (EF Core + PostgreSQL), email simulado e mensageria (RabbitMQ)
```

> **Migração em andamento.** A API HTTP foi removida (SDD, DD-02). O host novo — a função
> Lambda acionada por SQS — chega no PR de `Notifications.Functions`. Até lá o código compila
> e os testes passam, mas não há processo executável. Ver [`SDD.md`](SDD.md).

Mais detalhes em [`NotificationsAPI/CLAUDE.md`](NotificationsAPI/CLAUDE.md).

## Stack tecnológico

- .NET 10
- Entity Framework Core 9 + PostgreSQL
- RabbitMQ (via pacote `FiapCloudGames.RabbitMq`)
- Serilog (structured logging)

## Variáveis de ambiente

| Variável | Obrigatória | Padrão | Descrição |
|---|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Não | — | Ambiente de execução (`Development`, `Production`, etc.) |
| `ConnectionStrings__DefaultConnection` | Sim | — | Connection string do PostgreSQL (ex.: `Host=localhost;Port=5432;Database=fcgdb;Username=fcg;Password=fcg123`) |
| `RabbitMq__Host` | Não | `localhost` | Host do RabbitMQ |
| `RabbitMq__Port` | Não | `5672` | Porta do RabbitMQ |
| `RabbitMq__Username` | Não | `guest` | Usuário do RabbitMQ |
| `RabbitMq__Password` | Não | `guest` | Senha do RabbitMQ |
| `RabbitMq__VirtualHost` | Não | `/` | Virtual host do RabbitMQ |
| `RabbitMq__MaxConnectionRetries` | Não | `3` | Número máximo de tentativas de conexão ao RabbitMQ |
| `RabbitMq__ConnectionRetryDelayMs` | Não | `1000` | Atraso (ms) entre tentativas de conexão ao RabbitMQ |

## Como rodar localmente

```bash
cd NotificationsAPI

# Build
dotnet build

# Rodar os testes
dotnet test
```

A solução (`NotificationsAPI.slnx`) está na raiz da pasta `NotificationsAPI/`.
