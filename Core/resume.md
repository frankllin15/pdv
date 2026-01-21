
# Especificação Técnica do Projeto: PDV Offline-First

## 1. Visão Geral

Desenvolvimento de um sistema de Ponto de Venda (PDV) de alta performance, com arquitetura **Offline-First**, focado em agilidade na frente de caixa e robustez na sincronização de dados. O sistema será multiplataforma (Windows/Linux) visando hardware diversificado (Desktops, Raspberry Pi, Totens).

## 2. Stack Tecnológica

### 2.1. Frente de Caixa (Client)

| Componente | Tecnologia | Versão | Justificativa |
| --- | --- | --- | --- |
| **Framework UI** | **Avalonia UI** | 11.x+ | Multiplataforma (Windows/Linux/macOS), XAML-based, alta performance de renderização. |
| **Linguagem** | C# | .NET 8/9 | Ecossistema robusto e tipagem forte. |
| **Banco Local** | **SQLite** | 3.x | Zero-config, arquivo único, transacional (ACID). |
| **ORM (Escrita)** | **EF Core** | Latest | Migrations, mapeamento de entidades complexas e persistência de vendas. |
| **ORM (Leitura)** | **Dapper** | Latest | Micro-ORM para consultas de alta performance (ex: busca de produtos no checkout). |
| **MVVM** | CommunityToolkit.Mvvm | Latest | Redução de boilerplate code (Source Generators). |

### 2.2. Retaguarda (Backend & Cloud)

| Componente | Tecnologia | Justificativa |
| --- | --- | --- |
| **API** | ASP.NET Core Web API | Endpoints RESTful para gestão e sincronização. |
| **Banco Nuvem** | SQL Server (Azure SQL) | Robustez relacional e compatibilidade nativa com .NET. |
| **Auth** | JWT (Bearer Token) | Autenticação segura e stateless. |

### 2.3. Bibliotecas e Ferramentas Críticas

* **Sincronização:** `Dotmim.Sync` (Sincronização bidirecional SQL Server ↔ SQLite).
* **Resiliência:** `Polly` (Retry policies, Circuit Breaker para chamadas HTTP e Fiscal).
* **Fiscal:** `Unimake.DFe` (ou `ACBrLib`) para emissão de NFC-e/SAT.
* **Logs:** `Serilog` (Sinks para Arquivo Local e Seq/Datadog opcional).
* **Mapeamento:** `AutoMapper` (Domain Entities ↔ DTOs).

---

## 3. Arquitetura da Solução

A solução seguirá o padrão **Onion Architecture** (ou Clean Architecture simplificada), garantindo que o núcleo do domínio não dependa de frameworks externos.

### 3.1. Estrutura de Projetos (Solution Explorer)

```text
PDV.Solution
│
├── 01-Core
│   ├── PDV.Core (Entities, Interfaces, Enums, Domain Exceptions)
│   └── PDV.Shared (DTOs compartilhados entre API e Desktop - essencial para Sync)
│
├── 02-Infra
│   ├── PDV.Data.Local (DbContext SQLite, Dapper Queries, Migrations Locais)
│   ├── PDV.Data.Cloud (DbContext SQL Server, Repositories Cloud)
│   ├── PDV.Fiscal (Implementação Unimake/ACBr, Gerenciamento de Contingência)
│   └── PDV.Integration (Services de Sync, Clients HTTP com Refit/HttpClient)
│
├── 03-Presentation
│   ├── PDV.Desktop (Avalonia Project, Views, ViewModels, Converters)
│   └── PDV.API (Controllers, Middlewares, Jobs de Background)
│
└── 04-Tests
    └── PDV.Tests (xUnit, Moq)

```

### 3.2. Padrão de Acesso a Dados (CQRS-Lite)

Para garantir a velocidade crítica do checkout, utilizaremos uma abordagem híbrida:

1. **Commands (Escrita/Updates):** Utilizam **EF Core**.
* *Ex:* `SalvarVenda`, `AtualizarEstoqueLocal`, `FecharCaixa`.
* *Motivo:* Integridade referencial e facilidade de manutenção.


2. **Queries (Leitura):** Utilizam **Dapper**.
* *Ex:* `BuscarProdutoPorEAN`, `ListarVendasDoDia`.
* *Motivo:* Performance bruta. O Dapper mapeia direto do DataReader para o Objeto, sem o overhead de change tracking do EF.



---

## 4. Estratégia de Sincronização (Sync Engine)

O sistema operará sob o princípio **Offline-First**. A conexão com a internet é tratada como um recurso intermitente.

### 4.1. Fluxo de Dados

* **Carga Inicial (Provisioning):** Ao instalar, o PDV baixa o snapshot do catálogo de produtos e configurações via `Dotmim.Sync`.
* **Venda Realizada:** A venda é salva no SQLite com flag `SyncState = Pending`.
* **Background Worker:** Um serviço roda a cada X minutos (ou via gatilho):
1. Verifica conexão.
2. Envia vendas `Pending` para a API (Upload).
3. Recebe atualizações de Produtos/Preços da API (Download).
4. Atualiza flags locais para `Synced`.



---

## 5. Módulos e Fases de Desenvolvimento

### Fase 1: Core & Venda Local (MVP)

* [ ] Setup da Solution e Avalonia UI.
* [ ] Banco SQLite com tabelas: `Produtos`, `Vendas`, `ItensVenda`, `Pagamentos`.
* [ ] Repositórios Dapper (Leitura) e EF (Escrita).
* [ ] Tela de Checkout funcional (Adicionar item, totais, remover item).
* [ ] Simulação de pagamento (Dinheiro/Cartão manual).

### Fase 2: Sincronização e Backend

* [ ] API .NET Core com SQL Server.
* [ ] Implementação do `Dotmim.Sync` (Client e Server providers).
* [ ] Worker Service no Desktop para sincronismo silencioso.

### Fase 3: Módulo Fiscal

* [ ] Integração com `Unimake.DFe` (ou similar).
* [ ] Lógica de Contingência (Se offline, emite em contingência e salva XML para envio posterior).
* [ ] Impressão de DANFE (ESC/POS).

### Fase 4: Pagamentos (TEF) e Periféricos

* [ ] Integração com DLLs de TEF (Sitef/PayGo).
* [ ] Integração Pix (API Banco).
* [ ] Leitura de Balança (Porta Serial).

---

## 6. Requisitos Não-Funcionais Críticos

1. **Latência de Input:** O tempo entre ler um código de barras e o item aparecer na grid deve ser **< 100ms**.
2. **Startup Time:** A aplicação deve estar pronta para vender em menos de 10 segundos após o boot do SO.
3. **Tratamento de Erros:** Nenhuma Exception não tratada deve fechar a aplicação (Crash). Uso de Global Exception Handler para logar e exibir mensagem amigável.
4. **Segurança:** Connection Strings e Tokens de API devem ser armazenados de forma segura (UserSecrets em dev, Cofre/Criptografia em prod).