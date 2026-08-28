# Claude Code Project Configuration

## Guia de Desenvolvimento para Notifications API

### 📋 Padrões Obrigatórios

#### 1. Commit Messages em Português
Todas as commit messages devem ser em **português brasileiro** seguindo o padrão Conventional Commits:

**Formato:**
```
<tipo>(<escopo>): <descrição>

<corpo>
```

**Tipos Válidos:**
- `feat`: Nova funcionalidade
- `fix`: Correção de bug
- `docs`: Mudanças em documentação
- `style`: Formatação, sem mudança de lógica
- `refactor`: Refatoração de código
- `perf`: Melhoria de performance
- `test`: Adicionar ou atualizar testes
- `chore`: Tarefas de build, dependências, etc.
- `ci`: Mudanças em CI/CD

**Exemplos Corretos:**
```
feat: implementar autenticação JWT
fix: corrigir validação de email no agregado Notification
docs: adicionar documentação de API
refactor: converter logs para partial log methods
test: adicionar testes unitários para NotificationRepository
```

**❌ Exemplos Incorretos:**
```
feat: Implement JWT authentication (em inglês)
Add new feature (sem tipo Conventional Commits)
```

---

#### 2. Logging com Partial Log Methods
Sempre use **partial log methods** com o atributo `[LoggerMessage]` em vez de chamar `logger.LogInformation()` diretamente.

**✅ Correto:**
```csharp
public partial class UserCreatedEventHandler
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Processando evento UserCreatedEvent para usuário {UserId}")]
    private partial void LogProcessingUserCreatedEvent(Guid userId);

    public async Task HandleAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        LogProcessingUserCreatedEvent(integrationEvent.UserId);
        // resto do código
    }
}
```

**❌ Incorreto:**
```csharp
logger.LogInformation("Processando evento para usuário {UserId}", userId);
```

**Benefícios:**
- 🚀 Source generation em compile-time
- 🔒 Type-safe logging
- 📝 Structured logging
- ⚡ Melhor performance

**Referências no Projeto:**
- `NotificationsAPI.Infrastructure/Email/EmailService.cs`
- `NotificationsAPI.Application/UseCases/Handlers/UserCreatedEventHandler.cs`
- `NotificationsAPI.Application/UseCases/Handlers/UserRegisteredEventHandler.cs`

---

#### 3. Nomeação de Variáveis
Evite usar `@` como escape para variáveis:

**✅ Correto:**
```csharp
public async Task HandleAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
{
    var userId = integrationEvent.UserId;
}
```

**❌ Incorreto:**
```csharp
public async Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken = default)
{
    var userId = @event.UserId;
}
```

---

### 🏗️ Arquitetura do Projeto

**Clean Architecture com 3 Camadas:**

```
NotificationsAPI/
├── NotificationsAPI.Domain/          ← Regras de negócio
│   ├── Notifications/                ← Aggregate root
│   └── Shared/                       ← Interfaces e base classes
├── NotificationsAPI.Application/     ← Casos de uso
│   ├── UseCases/
│   └── DependencyInjection/
└── NotificationsAPI.Infrastructure/  ← Persistência, logging, mensageria
    ├── Persistence/DynamoDb/         ← Repositório DynamoDB e mapeamento
    ├── Configuration/                ← Serilog setup
    ├── Email/                        ← Serviços de email
    └── Messaging/                    ← RabbitMQ setup
```

> A camada `NotificationsAPI/` (API HTTP) foi removida na migração para serverless —
> ver `SDD.md`, DD-02.

---

### 📚 Stack Tecnológico

- **.NET 10.0** - Framework
- **DynamoDB** - Database (AWS SDK v4)
- **RabbitMQ** - Message Broker
- **Serilog 4.2.0** - Structured Logging
- **NSubstitute** - Mocking (testes)
- **Shouldly** - Assertions (testes)

---

### 🔧 Configuração de Desenvolvimento

**Variáveis de Ambiente Necessárias:**
```bash
ASPNETCORE_ENVIRONMENT=Development
DynamoDb__TableName=fcg-notifications
DynamoDb__ServiceUrl=http://localhost:8000
RabbitMq__Host=localhost
RabbitMq__Port=5672
RabbitMq__Username=guest
RabbitMq__Password=guest
```

**Executar Localmente:**
```bash
# Build
dotnet build

# Run
dotnet run --project NotificationsAPI

# Tests
dotnet test
```

---

### ✅ Checklist para PRs

Antes de criar um PR, verifique:

- [ ] Commit messages em português com Conventional Commits
- [ ] Todos os logs usam partial log methods
- [ ] Nenhuma variável escapeada com `@`
- [ ] Código compila sem erros
- [ ] Testes passam: `dotnet test`
- [ ] Documentação XML em português
- [ ] Nenhum `Console.WriteLine()` ou `print()` direto
- [ ] Structured logging com propriedades estruturadas

---

### 🚀 Fluxo de Trabalho

1. **Criar branch feature:**
   ```bash
   git checkout -b feature/descricao-feature
   ```

2. **Desenvolver com commits em português:**
   ```bash
   git commit -m "feat: implementar nova funcionalidade"
   ```

3. **Criar PR quando pronto:**
   ```bash
   gh pr create --title "PR X: Descrição" --body "Detalhes da PR"
   ```

4. **Após aprovação, merge:**
   ```bash
   git merge --squash feature/descricao-feature
   ```

---

### 📞 Contato e Dúvidas

Para dúvidas sobre padrões ou configuração, consulte:
- Este arquivo (CLAUDE.md)
- Exemplos de implementação nos arquivos do projeto
- Issues abertas no GitHub do projeto

---

*Última atualização: 2026-07-01*
