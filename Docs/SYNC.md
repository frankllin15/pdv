# Sincronização de Dados - PDV

Este documento explica como funciona a sincronização bidirecional entre o banco local (SQLite) e o banco na nuvem (SQL Server) usando **Dotmim.Sync**.

## Visão Geral

```
┌─────────────────────┐                    ┌─────────────────────┐
│   PDV.Desktop       │                    │      PDV.API        │
│   (Cliente)         │                    │     (Servidor)      │
│                     │                    │                     │
│  ┌───────────────┐  │    HTTP/HTTPS      │  ┌───────────────┐  │
│  │    SQLite     │◄─┼────────────────────┼─►│  SQL Server   │  │
│  │  (Local DB)   │  │   Dotmim.Sync      │  │  (Cloud DB)   │  │
│  └───────────────┘  │                    │  └───────────────┘  │
└─────────────────────┘                    └─────────────────────┘
```

## Arquitetura

### Componentes

| Componente | Projeto | Responsabilidade |
|------------|---------|------------------|
| `SyncService` | PDV.Integration | Cliente de sincronização (SQLite → API) |
| `BackgroundSyncService` | PDV.Desktop | Executa sync periodicamente em background |
| `SyncController` | PDV.API | Endpoint que recebe requisições de sync |
| `WebServerAgent` | PDV.API | Orquestrador server-side do Dotmim.Sync |

### Fluxo de Sincronização

1. **BackgroundSyncService** inicia a cada 5 minutos (configurável)
2. Verifica disponibilidade do servidor (`/` health check)
3. Autentica via JWT se necessário
4. **SyncService** cria conexão com `WebRemoteOrchestrator` (HTTP) e `SqliteSyncProvider` (local)
5. `SyncAgent` executa sincronização bidirecional
6. Mudanças locais são enviadas (upload) e mudanças remotas são baixadas (download)

## Tabelas Sincronizadas

Configuradas em `PDV.API/Program.cs`:

```csharp
var syncSetup = new SyncSetup("Products", "Sales", "SaleItems", "Payments", "Operators");
```

**Tabelas atuais:**
- `Products` - Catálogo de produtos
- `Sales` - Vendas realizadas
- `SaleItems` - Itens de cada venda
- `Payments` - Pagamentos
- `Operators` - Operadores/Caixas

## Como Adicionar Nova Tabela à Sincronização

### Passo 1: Criar a Entidade

```csharp
// Core/PDV.Core/Entities/MinhaEntidade.cs
public class MinhaEntidade : Entity
{
    public string Nome { get; private set; }
    // ... propriedades
}
```

### Passo 2: Configurar EF Core (ambos os contextos)

**Local (SQLite):**
```csharp
// Infra/PDV.Data.Local/Configurations/MinhaEntidadeConfiguration.cs
public class MinhaEntidadeConfiguration : IEntityTypeConfiguration<MinhaEntidade>
{
    public void Configure(EntityTypeBuilder<MinhaEntidade> builder)
    {
        builder.ToTable("MinhaTabela");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        // ... outras configurações
    }
}
```

**Cloud (SQL Server):**
```csharp
// Infra/PDV.Data.Cloud/Configurations/MinhaEntidadeConfiguration.cs
// (mesma estrutura)
```

### Passo 3: Adicionar DbSet nos Contextos

```csharp
// PdvDbContext.cs e CloudDbContext.cs
public DbSet<MinhaEntidade> MinhasEntidades => Set<MinhaEntidade>();
```

### Passo 4: Criar Migrations

```bash
# Local (SQLite)
dotnet ef migrations add AddMinhaTabela -p Infra/PDV.Data.Local -s Presentation/PDV.Desktop

# Cloud (SQL Server)
dotnet ef migrations add AddMinhaTabela -p Infra/PDV.Data.Cloud -s Presentation/PDV.API
```

### Passo 5: Adicionar à Configuração de Sync

```csharp
// PDV.API/Program.cs
var syncSetup = new SyncSetup(
    "Products",
    "Sales",
    "SaleItems",
    "Payments",
    "Operators",
    "MinhaTabela"  // ← Adicionar aqui
);
```

### Passo 6: Atualizar SyncController (opcional)

```csharp
// Para documentação no GET
Tables = new[] { "Products", "Sales", "SaleItems", "Payments", "Operators", "MinhaTabela" }
```

## Configurações Importantes

### Intervalo de Sincronização

```csharp
// PDV.Desktop/Services/BackgroundSyncService.cs
_syncInterval = syncInterval ?? TimeSpan.FromMinutes(5);
```

### Política de Retry (Polly)

```csharp
// 3 tentativas com backoff exponencial (2s, 4s, 8s)
_retryPolicy = Policy
    .Handle<Exception>()
    .WaitAndRetryAsync(3, retryAttempt =>
        TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
```

### Resolução de Conflitos

Por padrão, Dotmim.Sync usa **Server Wins** - em caso de conflito, a versão do servidor prevalece.

Para customizar:
```csharp
syncAgent.LocalOrchestrator.OnApplyChangesConflictOccured(args =>
{
    args.Resolution = ConflictResolution.ClientWins; // ou ServerWins
});
```

## Endpoints da API

| Método | Rota | Descrição |
|--------|------|-----------|
| `POST` | `/api/sync` | Executa sincronização (requer JWT) |
| `GET` | `/api/sync` | Retorna status e tabelas configuradas |
| `GET` | `/` | Health check |

## Monitoramento

### Eventos de Status

```csharp
backgroundSync.StatusChanged += (sender, e) =>
{
    Console.WriteLine($"[{e.Status}] {e.Message}");
    // Status: Idle, Checking, Syncing, Completed, Offline, Error, Stopped
};
```

### Logs (Serilog)

```
[INF] Starting sync with server http://localhost:5233/api/sync
[INF] Sync completed. Downloaded: 10, Uploaded: 5, Duration: 1234ms
[WRN] Sync retry 1 after 2s
[ERR] Sync failed after 5000ms
```

## Troubleshooting

### Sync não inicia
- Verificar se API está rodando (`/` retorna healthy)
- Verificar autenticação JWT
- Conferir connection strings

### Tabela não sincroniza
- Confirmar que está no `SyncSetup`
- Verificar se migration foi aplicada em ambos os bancos
- Nome da tabela deve ser idêntico nos dois contextos

### Conflitos frequentes
- Considerar estratégia de resolução customizada
- Verificar se há edição simultânea da mesma entidade

## Referências

- [Dotmim.Sync Documentation](https://dotmimsync.readthedocs.io/)
- [Dotmim.Sync GitHub](https://github.com/Mimetis/Dotmim.Sync)
