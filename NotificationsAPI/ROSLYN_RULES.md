# Regras Roslyn e EditorConfig

## Regras Obrigatórias do Projeto

Este projeto utiliza Roslyn Analyzers e EditorConfig para garantir qualidade de código e consistência.

### 📋 Regras Configuradas

#### 1. **Primary Constructors (C# 12+)** - ❌ OBRIGATÓRIO
**Severity:** ERROR

Todas as classes, records e structs com injeção de dependência **DEVEM** usar primary constructors.

**✅ Correto:**
```csharp
public class UserCreatedEventHandler(
    IUnitOfWork unitOfWork,
    ILogger<UserCreatedEventHandler> logger) : IEventHandler<UserCreatedEvent>
{
    public async Task HandleAsync(UserCreatedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        // código aqui
    }
}
```

**❌ Incorreto:**
```csharp
public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(IUnitOfWork unitOfWork, ILogger<UserCreatedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }
}
```

---

#### 2. **Unnecessary Imports (IDE0005)** - ❌ OBRIGATÓRIO
**Severity:** ERROR

Todos os `using` statements desnecessários **DEVEM** ser removidos.

**✅ Correto:**
```csharp
namespace NotificationsAPI.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using Domain.Notifications;
using Domain.Shared;

// Todos os usings acima são utilizados no arquivo
```

**❌ Incorreto:**
```csharp
namespace NotificationsAPI.Application.UseCases.Handlers;

using Microsoft.Extensions.Logging;
using FiapCloudGames.Contracts.Users;
using Domain.Notifications;
using Domain.Shared;
using System.Collections.Generic;  // ❌ Não utilizado
using System.Linq;                  // ❌ Não utilizado

// Usar algum dos usings acima
```

---

#### 3. **Code Formatting (IDE0055)** - ❌ OBRIGATÓRIO
**Severity:** ERROR

Todo código **DEVE** seguir as regras de formatação definidas no `.editorconfig`.

Inclui:
- Espaçamento
- Indentação
- Quebras de linha
- Ordem de declarações

**Ao salvar um arquivo, seu IDE deve auto-formatar conforme as regras.**

---

### 🔧 Como as Regras Funcionam

#### No Visual Studio / Rider
1. Arquivo `.editorconfig` é automaticamente lido
2. Regras são aplicadas ao salvar
3. Erros aparecem em tempo real (ondulado vermelho)
4. Use `Ctrl+K, Ctrl+D` para formatar documento

#### No Build
```bash
dotnet build
```

Se houver violações:
- **IDE0005**: `error CS8019: Unnecessary using directive`
- **IDE0055**: `error IDE0055: Fix formatting`
- **Primary Constructors**: `error CS1729: Class does not contain a constructor`

O build **FALHARÁ** até que todas as regras sejam satisfeitas.

---

### 📝 Configuração em Visual Studio Code

Adicione ao `.vscode/settings.json`:
```json
{
  "editor.formatOnSave": true,
  "omnisharp.enableRoslynAnalyzers": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.style": true,
    "source.organizeImports": true
  }
}
```

### 📝 Configuração em JetBrains Rider

1. Vá para **Settings → Tools → Actions on Save**
2. Ative **Reformat code**
3. Ative **Optimize imports**
4. Ative **Run Code Inspections**

---

### ⚠️ Erros Comuns

#### Erro: "CS8019: Unnecessary using directive"
**Solução:** Remova o using não utilizado

```bash
# EditorConfig automático ao salvar remove esses
# Ou execute:
dotnet format
```

#### Erro: "CS1729: Class does not contain a constructor matching arguments"
**Solução:** Use primary constructor

```csharp
// Antes
public class MyClass
{
    private readonly IService _service;
    public MyClass(IService service) => _service = service;
}

// Depois
public class MyClass(IService service)
{
    // service está disponível diretamente
}
```

#### Erro: "IDE0055: Fix formatting"
**Solução:** Formate o arquivo

```bash
dotnet format --include <arquivo>
```

---

### 🚀 Workflow Recomendado

1. **Ao escrever código:**
   - Configure seu IDE para formatar ao salvar
   - Remova usings não utilizados automaticamente

2. **Antes de commitar:**
   ```bash
   dotnet build
   ```
   Se houver erros, corrija antes de fazer commit

3. **Em CI/CD:**
   ```bash
   dotnet build --configuration Release
   ```
   O build falhará se houver violações

---

### 📚 Referências

- [EditorConfig Documentation](https://editorconfig.org/)
- [Roslyn Analyzers](https://github.com/dotnet/roslyn-analyzers)
- [Code Style Rules (IDE)](https://docs.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/)
- [Primary Constructors (C# 12)](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#primary-constructors)

---

### ✅ Checklist

Antes de fazer push:

- [ ] Nenhum `using` desnecessário (`IDE0005` resolvido)
- [ ] Código formatado conforme `.editorconfig` (`IDE0055` resolvido)
- [ ] Todas as classes/records com DI usam primary constructors
- [ ] Build passa sem erros: `dotnet build`
- [ ] Código compila: `dotnet build --configuration Release`

---

*Última atualização: 2026-07-01*
