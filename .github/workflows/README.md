# GitHub Actions Workflows

## CI/CD Pipeline (ci.yml)

Executa automaticamente em push e pull requests.

### Etapas:
1. **Checkout** - Faz checkout do código
2. **Setup .NET** - Configura .NET 10.0
3. **Restore** - Restaura dependências do projeto
4. **Build** - Compila a solução em modo Release
5. **Tests** - Executa todos os testes unitários
6. **Code Style** - Valida regras de código (Roslyn)
7. **Check Changes** - Verifica se há mudanças não commitadas

### Triggers:
- Push em `main` ou branches `feature/**`
- Pull requests para `main` ou branches `feature/**`

## CodeQL Analysis (codeql-analysis.yml)

Análise de segurança e qualidade de código automatizada.

### Etapas:
1. **Checkout** - Faz checkout do código
2. **CodeQL Init** - Inicializa análise CodeQL
3. **Setup .NET** - Configura .NET 10.0
4. **Build** - Compila a solução
5. **Analyze** - Executa análise CodeQL

### Triggers:
- Push em `main` ou branches `feature/**`
- Pull requests para `main` ou branches `feature/**`
- Agendado: toda segunda-feira às 2h UTC

## Artifacts

Os workflows armazenam automaticamente:
- `test-results` - Resultados dos testes em formato TRX

## Status Badge

Adicione ao README principal:

```markdown
![CI/CD Pipeline](https://github.com/joao-malvetoni-alta-horizon/FIAPCloudGames-fase2-NotificationsAPI/workflows/CI%2FCD%20Pipeline/badge.svg)
```
