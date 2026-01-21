
# GUIA DE IMPLEMENTAÇÃO TÉCNICA: PDV OFFLINE-FIRST

**Projeto:** Sistema PDV (Frente de Caixa e Retaguarda)
**Versão da Especificação:** 1.0
**Stack Base:** .NET 8/9, Avalonia UI, SQLite, SQL Server.

---

## 1. Estrutura da Solução e Projetos

**Diretriz:** A solução deve seguir a Arquitetura em Camadas (Onion/Clean) para garantir desacoplamento entre UI, Lógica e Dados.

### 1.1. Organização da Solution (.sln)

A estrutura de pastas e projetos deve ser criada conforme abaixo:

* **`MyPDV.Core`** (Class Library)
* *Responsabilidade:* Entidades de domínio, Interfaces (Contratos), Enums e DTOs compartilhados.
* *Dependências:* Nenhuma (Zero dependency).


* **`MyPDV.Infrastructure`** (Class Library)
* *Responsabilidade:* Implementação de banco de dados, integrações fiscais e serviços de sistema.
* *Dependências:* EF Core, Dapper, Dotmim.Sync, Drivers Fiscais.


* **`MyPDV.Desktop`** (Avalonia App)
* *Responsabilidade:* Aplicação Cliente (Frente de Caixa).
* *Dependências:* MyPDV.Core, MyPDV.Infrastructure.


* **`MyPDV.API`** (ASP.NET Core Web API)
* *Responsabilidade:* Backend de gestão e hub de sincronização.
* *Dependências:* MyPDV.Core, MyPDV.Infrastructure.



---

## 2. Frente de Caixa (Client Desktop)

### 2.1. Interface de Usuário (UI)

* **Framework:** Utilizar **Avalonia UI** (v11+).
* **Requisito de Plataforma:** O código deve ser compatível com Windows (produção inicial) e Linux (previsão futura para Raspberry Pi/Totens).
* **Padrão de Design:** MVVM (Model-View-ViewModel) utilizando o pacote `CommunityToolkit.Mvvm` para Source Generators (`[ObservableProperty]`, `[RelayCommand]`).

### 2.2. Persistência Local (Dados Offline)

* **Motor de Banco:** **SQLite** (versão 3.x).
* **Configuração:** O banco deve ser criado como um arquivo local único (`pdv_local.db`) na pasta de dados de aplicativo do usuário (`AppData` ou `/var/lib`).

### 2.3. Estratégia de Acesso a Dados (ORM Híbrido)

**Regra Mandatória:** O acesso a dados deve seguir estritamente a divisão abaixo para garantir performance:

1. **Escrita e Gestão (Command):** Utilizar **Entity Framework Core**.
* *Uso:* Criação de vendas, migrações de schema, inserção de dados sincronizados.
* *Justificativa:* Garante integridade referencial e facilita a manutenção do schema.


2. **Leitura Crítica (Query):** Utilizar **Dapper**.
* *Uso:* Busca de produtos por código de barras no checkout, relatórios rápidos e listagens de grid.
* *Requisito de Performance:* A query de busca de produto deve retornar em **< 50ms**.
* *Snippet Obrigatório:* Mapear diretamente para DTOs leves, evitando o overhead de change tracking do EF.



---

## 3. Retaguarda (Backend & Cloud)

### 3.1. API

* **Framework:** **ASP.NET Core Web API** (.NET 8 ou superior).
* **Autenticação:** Implementar **JWT (JSON Web Tokens)**.
* *Fluxo:* O PDV realiza login uma única vez, obtém o token e o utiliza para todas as requisições de sincronização subsequentes.


* **Hospedagem:** Preparar para deploy em **Azure App Service** (Linux/Windows) ou Container (Docker).

### 3.2. Persistência Nuvem

* **Banco de Dados:** **SQL Server** (Azure SQL Database).
* **Schema:** Deve espelhar a estrutura crítica do SQLite, mas suportando multi-tenancy (coluna `TenantId` ou `LojaId` obrigatória em todas as tabelas).

---

## 4. Integrações e Bibliotecas (Componentes Core)

### 4.1. Módulo Fiscal

**Biblioteca:** Utilizar **Unimake.DFe** (Preferencial) ou **ACBrLib**.

* **Abstração:** Criar uma interface `IFiscalService` na camada `Core`. A implementação concreta (`UnimakeFiscalService`) deve ficar em `Infrastructure`.
* **Requisito:** Não manipular XML manualmente. Utilizar os objetos tipados da biblioteca escolhida.

### 4.2. Motor de Sincronização (Sync)

**Biblioteca:** **Dotmim.Sync**.

* **Arquitetura:**
* *Server Side (API):* Configurar `SqlSyncProvider` apontando para o SQL Server.
* *Client Side (Desktop):* Configurar `SqliteSyncProvider` apontando para o SQLite local.


* **Estratégia:** Sincronização delta (apenas o que mudou). Configurar para resolução automática de conflitos (Prioridade: Server Wins ou Client Wins dependendo da tabela).

### 4.3. Resiliência e Logs

* **Resiliência:** Utilizar **Polly**.
* *Política:* Implementar `WaitAndRetry` para falhas de rede transitórias (envio de nota, sync). Implementar `CircuitBreaker` para mudar o PDV para "Modo Offline" após falhas consecutivas.


* **Logging:** Utilizar **Serilog**.
* *Sinks:* Configurar `RollingFile` (arquivo local rotacionado diariamente) para auditoria local. O nível de log deve ser configurável via `appsettings.json`.


* **Mapeamento:** Utilizar **AutoMapper**.
* *Uso:* Conversão entre Entidades de Domínio (Core) e ViewModels (Desktop) ou DTOs (API). Proibido expor Entidades do EF Core diretamente na API ou na View.



---

## 5. Checklist de Inicialização (Setup)

Para iniciar o desenvolvimento, execute a instalação dos pacotes base nos respectivos projetos:

```powershell
# No projeto MyPDV.Infrastructure
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Dapper
dotnet add package Dotmim.Sync.Sqlite
dotnet add package Dotmim.Sync.SqlServer
dotnet add package Unimake.DFe
dotnet add package Serilog.Sinks.File

# No projeto MyPDV.API
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package Dotmim.Sync.Web.Server
dotnet add package AutoMapper

# No projeto MyPDV.Desktop
dotnet add package Avalonia
dotnet add package CommunityToolkit.Mvvm
dotnet add package Dotmim.Sync.Web.Client
dotnet add package Polly

```