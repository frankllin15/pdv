# PDV - Documentação do Schema do Banco de Dados

## Visão Geral

O projeto utiliza dois bancos de dados:
- **Local**: SQLite em `%APPDATA%\PDV\pdv_local.db` (desktop offline-first)
- **Nuvem**: SQL Server via API (sincronização bidirecional)

Todos os IDs são `Guid` (UUIDv7), armazenados como `BLOB` (16 bytes) no SQLite para performance.

---

## Entidade Base

Todas as tabelas herdam de `Entity`:

| Coluna | Tipo | Obrigatório | Descrição |
|--------|------|-------------|-----------|
| `Id` | Guid (BLOB) | ✓ | Chave primária, UUIDv7 gerado automaticamente |
| `CreatedAt` | DateTime (TEXT) | ✓ | Data/hora de criação em UTC |
| `UpdatedAt` | DateTime? (TEXT) | - | Data/hora da última atualização em UTC |

---

## Tabelas

### Operators
> Operadores/caixas do sistema.

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `Name` | varchar(100) | ✓ | - | Nome completo do operador |
| `Code` | varchar(20) | ✓ | - | Código único de identificação |
| `PinHash` | varchar(100) | ✓ | - | PIN criptografado (SHA256) |
| `IsActive` | bool | ✓ | `true` | Indica se o operador está ativo |
| `IsAdmin` | bool | ✓ | `false` | Indica se possui permissões de administrador |
| `LastLoginAt` | DateTime? | - | - | Último acesso ao sistema |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Índices**: `UNIQUE (Code)`

---

### Products
> Catálogo de produtos.

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `Barcode` | varchar(50) | ✓ | - | Código de barras único |
| `Description` | varchar(200) | ✓ | - | Descrição completa do produto |
| `ShortDescription` | varchar(50) | - | - | Descrição curta para exibição |
| `UnitPrice` | decimal(18,2) | ✓ | - | Preço de venda unitário |
| `UnitOfMeasure` | varchar(10) | ✓ | `"UN"` | Unidade de medida (UN, KG, etc.) |
| `StockQuantity` | decimal(18,3) | ✓ | - | Quantidade em estoque |
| `TaxCode` | varchar(20) | - | - | NCM (Nomenclatura Comum do Mercosul) |
| `TaxRate` | decimal(5,2) | - | - | Alíquota ICMS |
| `Cfop` | varchar(10) | - | - | Código Fiscal de Operações e Prestações |
| `Cest` | varchar(10) | - | - | Código Especificador da Substituição Tributária |
| `TaxOrigin` | int | ✓ | `0` | Enum `TaxOrigin`: origem tributária |
| `Cst` | varchar(10) | - | - | Código de Situação Tributária ICMS |
| `IsActive` | bool | ✓ | `true` | Indica se o produto está ativo |
| `SyncState` | int | ✓ | `0` | Enum `SyncState`: estado de sincronização |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Índices**: `UNIQUE (Barcode)`

---

### Sales
> Transações de venda.

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `SaleNumber` | int | ✓ | - | Número sequencial da venda |
| `SaleDate` | DateTime | ✓ | - | Data/hora da venda em UTC |
| `Subtotal` | decimal(18,2) | ✓ | - | Subtotal antes dos descontos |
| `Discount` | decimal(18,2) | ✓ | `0` | Desconto aplicado na venda |
| `Total` | decimal(18,2) | ✓ | - | Total final da venda |
| `Change` | decimal(18,2) | ✓ | `0` | Troco dado ao cliente |
| `Status` | int | ✓ | - | Enum `SaleStatus`: status da venda |
| `CustomerDocument` | varchar(20) | - | - | CPF do cliente (opcional) |
| `OperatorId` | Guid (FK) | ✓ | - | Operador que realizou a venda |
| `CashSessionId` | Guid (FK) | - | - | Sessão de caixa vinculada |
| `SyncState` | int | ✓ | `0` | Enum `SyncState`: estado de sincronização |
| `FiscalStatus` | int | ✓ | `0` | Enum `FiscalStatus`: status fiscal |
| `FiscalAccessKey` | varchar(44) | - | - | Chave de acesso da NFC-e (44 dígitos) |
| `FiscalNumber` | int? | - | - | Número sequencial da NFC-e |
| `FiscalSeries` | int? | - | - | Série da NFC-e |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Chaves Estrangeiras**:
- `OperatorId` → `Operators(Id)`
- `CashSessionId` → `CashSessions(Id)`

**Índices**: `INDEX (CashSessionId)`

---

### SaleItems
> Itens de cada venda (snapshot do produto no momento da venda).

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `SaleId` | Guid (FK) | ✓ | - | Venda à qual o item pertence |
| `ProductId` | Guid | ✓ | - | Referência ao produto |
| `Barcode` | varchar(50) | ✓ | - | Código de barras no momento da venda |
| `ProductDescription` | varchar(200) | ✓ | - | Descrição do produto no momento da venda |
| `Quantity` | decimal(18,3) | ✓ | - | Quantidade vendida |
| `UnitPrice` | decimal(18,2) | ✓ | - | Preço unitário no momento da venda |
| `Discount` | decimal(18,2) | ✓ | `0` | Desconto por item |
| `Total` | decimal(18,2) | ✓ | - | Total calculado do item |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Chaves Estrangeiras**:
- `SaleId` → `Sales(Id)` (cascade delete)

**Índices**: `INDEX (SaleId)`

---

### Payments
> Pagamentos realizados em uma venda (pode haver múltiplos por venda).

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `SaleId` | Guid (FK) | ✓ | - | Venda vinculada |
| `Method` | int | ✓ | - | Enum `PaymentMethod`: forma de pagamento |
| `Amount` | decimal(18,2) | ✓ | - | Valor pago |
| `AuthorizationCode` | varchar(50) | - | - | NSU/código de autorização do cartão |
| `CardBrand` | varchar(30) | - | - | Bandeira do cartão (Visa, Master, etc.) |
| `PaymentDate` | DateTime | ✓ | - | Data/hora do pagamento |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Chaves Estrangeiras**:
- `SaleId` → `Sales(Id)` (cascade delete)

**Índices**: `INDEX (SaleId)`

---

### CashSessions
> Sessões de abertura e fechamento de caixa.

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `OperatorId` | Guid | ✓ | - | Operador responsável pela sessão |
| `TerminalId` | varchar(100) | ✓ | - | Identificador do terminal/caixa |
| `OpenedAt` | DateTime | ✓ | - | Data/hora de abertura |
| `ClosedAt` | DateTime? | - | - | Data/hora de fechamento |
| `OpeningBalance` | decimal(18,2) | ✓ | `0` | Valor em caixa na abertura |
| `ClosingBalance` | decimal(18,2) | ✓ | `0` | Valor contado no fechamento |
| `CalculatedBalance` | decimal(18,2) | ✓ | `0` | Saldo calculado pelo sistema |
| `Status` | int | ✓ | - | Enum `SessionStatus`: status da sessão |
| `SyncState` | int | ✓ | `0` | Enum `SyncState`: estado de sincronização |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

> **Nota**: A propriedade `Difference` (ClosingBalance - CalculatedBalance) é calculada em memória e não persiste no banco.

**Índices**: `INDEX (TerminalId, Status)`

---

### CashTransactions
> Sangrias e suprimentos de caixa.

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `CashSessionId` | Guid (FK) | ✓ | - | Sessão de caixa vinculada |
| `Type` | int | ✓ | - | Enum `CashTransactionType`: tipo (Suprimento/Sangria) |
| `Amount` | decimal(18,2) | ✓ | - | Valor da movimentação |
| `Description` | varchar(500) | - | - | Observação/justificativa |
| `OperatorId` | Guid | ✓ | - | Operador que realizou a movimentação |
| `TransactionDate` | DateTime | ✓ | - | Data/hora da movimentação |
| `SyncState` | int | ✓ | `0` | Enum `SyncState`: estado de sincronização |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Chaves Estrangeiras**:
- `CashSessionId` → `CashSessions(Id)` (cascade delete)

**Índices**: `INDEX (CashSessionId)`

---

### FiscalTransactions
> Documentos fiscais NFC-e emitidos (apenas banco local).

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `SaleId` | Guid (FK) | ✓ | - | Venda vinculada |
| `AccessKey` | varchar(44) | ✓ | - | Chave de acesso única da NFC-e (44 dígitos) |
| `Number` | int | ✓ | - | Número sequencial da NFC-e |
| `Series` | int | ✓ | - | Série da NFC-e |
| `Status` | int | ✓ | `1` | Enum `FiscalStatus`: status do documento |
| `Protocol` | varchar(50) | - | - | Protocolo de autorização da SEFAZ |
| `StatusCode` | int? | - | - | Código de retorno cStat da SEFAZ |
| `StatusMessage` | varchar(500) | - | - | Mensagem de retorno xMotivo da SEFAZ |
| `XmlRequest` | TEXT | - | - | XML enviado para a SEFAZ |
| `XmlResponse` | TEXT | - | - | XML de resposta da SEFAZ |
| `IsContingency` | bool | ✓ | `false` | Emitido em modo de contingência (offline) |
| `AuthorizationDate` | DateTime? | - | - | Data/hora de autorização pela SEFAZ |
| `CancellationDate` | DateTime? | - | - | Data/hora do cancelamento |
| `CancellationProtocol` | varchar(50) | - | - | Protocolo de cancelamento |
| `CancellationJustification` | varchar(255) | - | - | Justificativa do cancelamento |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Chaves Estrangeiras**:
- `SaleId` → `Sales(Id)` (restrict delete)

**Índices**: `UNIQUE (AccessKey)`, `INDEX (SaleId)`, `INDEX (Status)`, `INDEX (IsContingency)`

---

### FiscalConfigurations
> Configurações do contribuinte para emissão de NFC-e (apenas banco local).

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `TaxId` | varchar(14) | ✓ | - | CNPJ único do estabelecimento |
| `LegalName` | varchar(200) | ✓ | - | Razão Social |
| `TradeName` | varchar(200) | ✓ | - | Nome Fantasia |
| `StateRegistration` | varchar(20) | ✓ | - | Inscrição Estadual |
| `State` | varchar(2) | ✓ | - | UF (ex: SP, RJ) |
| `CityCode` | varchar(7) | ✓ | - | Código IBGE do município (7 dígitos) |
| `Address` | varchar(200) | ✓ | - | Logradouro |
| `AddressNumber` | varchar(20) | ✓ | - | Número do endereço |
| `Neighborhood` | varchar(100) | - | - | Bairro |
| `ZipCode` | varchar(8) | ✓ | - | CEP (8 dígitos, sem máscara) |
| `TaxRegime` | int | ✓ | - | Regime tributário (1=Simples, 2=Simples Excesso, 3=Normal) |
| `Series` | int | ✓ | `1` | Série da NFC-e |
| `NextNumber` | int | ✓ | `1` | Próximo número sequencial da NFC-e |
| `CertificatePath` | varchar(500) | - | - | Caminho para o certificado digital A1 |
| `CertificatePassword` | varchar(200) | - | - | Senha do certificado (deve ser criptografada) |
| `CscToken` | varchar(100) | - | - | Código de Segurança do Contribuinte |
| `CscId` | varchar(10) | - | - | Identificador do CSC |
| `IsProduction` | bool | ✓ | `false` | `true` = Produção, `false` = Homologação |
| `IsActive` | bool | ✓ | `true` | Configuração ativa |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Índices**: `UNIQUE (TaxId)`, `INDEX (IsActive)`

---

### FiscalReprintLogs
> Auditoria de reimpressões de DANFE (apenas banco local).

| Coluna | Tipo | Obrigatório | Padrão | Descrição |
|--------|------|-------------|--------|-----------|
| `Id` | Guid | ✓ | UUIDv7 | Chave primária |
| `FiscalTransactionId` | Guid (FK) | ✓ | - | Documento fiscal reimpresso |
| `OperatorId` | Guid (FK) | ✓ | - | Operador que solicitou a reimpressão |
| `ReprintedAt` | DateTime | ✓ | - | Data/hora da reimpressão |
| `Reason` | varchar(255) | - | - | Motivo da reimpressão |
| `ReprintNumber` | int | ✓ | - | Contador sequencial de reimpressões |
| `CreatedAt` | DateTime | ✓ | - | *herdado* |
| `UpdatedAt` | DateTime? | - | - | *herdado* |

**Chaves Estrangeiras**:
- `FiscalTransactionId` → `FiscalTransactions(Id)` (restrict delete)
- `OperatorId` → `Operators(Id)` (restrict delete)

**Índices**: `INDEX (FiscalTransactionId)`, `INDEX (OperatorId)`, `INDEX (ReprintedAt)`

---

## Enumerações

### SyncState
| Valor | Nome | Descrição |
|-------|------|-----------|
| `0` | `Pending` | Aguardando sincronização (padrão) |
| `1` | `Synced` | Sincronizado com a nuvem |
| `2` | `Error` | Erro na sincronização |

### SaleStatus
| Valor | Nome | Descrição |
|-------|------|-----------|
| `1` | `InProgress` | Venda em andamento |
| `2` | `Completed` | Venda finalizada |
| `3` | `Cancelled` | Venda cancelada |

### PaymentMethod
| Valor | Nome | Descrição |
|-------|------|-----------|
| `1` | `Cash` | Dinheiro |
| `2` | `CreditCard` | Cartão de crédito |
| `3` | `DebitCard` | Cartão de débito |
| `4` | `Pix` | Pix |
| `5` | `FoodVoucher` | Vale alimentação |
| `6` | `MealVoucher` | Vale refeição |
| `99` | `Other` | Outro |

### FiscalStatus
| Valor | Nome | Descrição |
|-------|------|-----------|
| `0` | `None` | Venda sem documento fiscal |
| `1` | `Pending` | Aguardando emissão |
| `2` | `Authorized` | Autorizado pela SEFAZ |
| `3` | `Contingency` | Emitido em contingência (offline) |
| `4` | `Cancelled` | Cancelado |
| `5` | `Rejected` | Rejeitado pela SEFAZ |

### SessionStatus
| Valor | Nome | Descrição |
|-------|------|-----------|
| `1` | `Open` | Caixa aberto |
| `2` | `Closed` | Caixa fechado |

### CashTransactionType
| Valor | Nome | Descrição |
|-------|------|-----------|
| `1` | `Supply` | Suprimento (entrada de dinheiro) |
| `2` | `Bleed` | Sangria (retirada de dinheiro) |

### TaxOrigin
| Valor | Nome | Descrição |
|-------|------|-----------|
| `0` | `National` | Nacional |
| `1` | `ImportedDirect` | Importação direta |
| `2` | `ImportedInternal` | Adquirido no mercado interno |

---

## Relacionamentos

```
Operators (1) ────< Sales (M)                FK: Sales.OperatorId
Operators (1) ────< CashSessions (M)         (referência lógica)
Operators (1) ────< CashTransactions (M)     (referência lógica)
Operators (1) ────< FiscalReprintLogs (M)    FK: FiscalReprintLogs.OperatorId (RESTRICT)

Products (1) ────< SaleItems (M)             (referência lógica, snapshot)

Sales (1) ────< SaleItems (M)                FK: SaleItems.SaleId (CASCADE DELETE)
Sales (1) ────< Payments (M)                 FK: Payments.SaleId (CASCADE DELETE)
Sales (1) ────── FiscalTransactions (1)      FK: FiscalTransactions.SaleId (RESTRICT)
Sales (M) ────> CashSessions (1)             FK: Sales.CashSessionId

CashSessions (1) ────< CashTransactions (M)  FK: CashTransactions.CashSessionId (CASCADE DELETE)

FiscalTransactions (1) ────< FiscalReprintLogs (M)  FK: FiscalReprintLogs.FiscalTransactionId (RESTRICT)
```

---

## Migrações (SQLite Local)

| Migração | Data | Descrição |
|----------|------|-----------|
| `InitMigrations` | 2026-02-07 | Schema inicial |
| `ChangeIdTypeToBinary` | 2026-02-08 | Otimização de GUIDs para BLOB |
| `AddCashSession` | 2026-02-12 | Tabelas `CashSessions` e `CashTransactions` |
| `AddSaleChangeColumn` | 2026-02-12 | Coluna `Change` em `Sales` |