# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

PDV (Ponto de Venda) is an **Offline-First Point-of-Sale system** built with .NET 9. It consists of a desktop client (frente de caixa) using Avalonia UI and a backend API for cloud synchronization.

## Build and Run Commands

```bash
# Build entire solution
dotnet build PDV.sln

# Run desktop application
dotnet run --project Presentation/PDV.Desktop/PDV.Desktop.csproj

# Run API server
dotnet run --project Presentation/PDV.API/PDV.API.csproj

# Create EF migration (local SQLite)
dotnet ef migrations add <MigrationName> -p Infra/PDV.Data.Local -s Presentation/PDV.Desktop

# Create EF migration (cloud SQL Server)
dotnet ef migrations add <MigrationName> -p Infra/PDV.Data.Cloud -s Presentation/PDV.API
```

Database migrations are automatically applied on startup via `context.Database.Migrate()`.

## Architecture

**Onion/Clean Architecture with CQRS-Lite pattern:**

```
Core/           - Domain layer (entities, interfaces, DTOs)
  PDV.Core      - Entities (Sale, Product, Operator), repository interfaces
  PDV.Shared    - DTOs and enums shared between API and Desktop

Infra/          - Infrastructure layer
  PDV.Data.Local  - SQLite context, EF repositories, Dapper queries
  PDV.Data.Cloud  - SQL Server context for backend
  PDV.Integration - Sync services, HTTP clients, background workers
  PDV.Fiscal      - Reserved for fiscal integration (NFC-e/SAT)

Presentation/   - Application layer
  PDV.Desktop   - Avalonia UI client (MVVM with CommunityToolkit.Mvvm)
  PDV.API       - ASP.NET Core Web API with JWT auth
```

## Key Patterns

### CQRS-Lite Data Access
- **Writes:** Entity Framework Core (change tracking, migrations)
- **Reads:** Dapper (high-performance queries for checkout)

Example: `IProductRepository` (EF) for writes, `IProductQuery` (Dapper) for reads.

### Offline-First Synchronization
- Local: SQLite at `%APPDATA%\PDV\pdv_local.db`
- Cloud: SQL Server via Dotmim.Sync
- Sales marked with `SyncState.Pending` until synchronized
- Background worker handles bidirectional sync

### MVVM Pattern (Desktop)
- ViewModels in `PDV.Desktop/ViewModels/`
- Use `[ObservableProperty]` and `[RelayCommand]` from CommunityToolkit.Mvvm
- DI configured in `App.axaml.cs`

## Core Entities

- **Sale** - Main transaction with `SaleItems` and `Payments`, tracks `SyncState`
- **Product** - Catalog items with barcode, price, stock
- **Operator** - Cashier/user for session tracking

## Performance Requirements

- Barcode to item display: <100ms
- Product search (Dapper): <50ms
- Application startup: <10 seconds

## Technology Stack

- .NET 9.0, Avalonia UI 11.x, SQLite/SQL Server
- EF Core 9 (writes), Dapper (reads)
- Dotmim.Sync (synchronization)
- Polly (resilience), Serilog (logging)
- JWT authentication (API)
