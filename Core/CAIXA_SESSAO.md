
O conceito principal aqui é que **toda Venda deve pertencer a uma Sessão de Caixa**. Sem isso, você não consegue saber se o dinheiro na gaveta bate com o que foi vendido naquele turno específico.

Aqui está o plano de implementação:

### 1. Novos Enums

Primeiro, vamos definir os estados e tipos de movimentação.

```csharp
public enum SessionStatus
{
    Open = 1,       // Caixa Aberto
    Closed = 2,     // Caixa Fechado
    Locked = 3      // Bloqueado (Ex: Operador saiu para almoço, mas não fechou)
}

public enum CashTransactionType
{
    Supply = 1,     // Suprimento (Entrada de dinheiro/troco)
    Bleed = 2       // Sangria (Retirada de dinheiro para cofre/pagamento)
}

```

### 2. Novas Entidades

Precisamos de duas novas tabelas: uma para controlar o turno (`CashSession`) e outra para registrar entradas/saídas manuais (`CashTransaction`).

#### A. CashSession (O Turno/Caixa)

Representa o período de trabalho.

```csharp
public class CashSession : Entity
{
    // Quem abriu o caixa
    public Guid OperatorId { get; private set; }
    
    // Identificador do Terminal (Lido do config local, importante para sync)
    public string TerminalId { get; private set; } = string.Empty;

    public DateTime OpenedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }

    // Valores Financeiros
    public decimal OpeningBalance { get; private set; } // Fundo de Troco
    public decimal ClosingBalance { get; private set; } // Valor informado na contagem cega
    public decimal CalculatedBalance { get; private set; } // Valor que o sistema calculou
    public decimal Difference => ClosingBalance - CalculatedBalance; // Sobra ou Quebra

    public SessionStatus Status { get; private set; } = SessionStatus.Open;
    public SyncState SyncState { get; private set; } = SyncState.Pending;

    // Construtor para Abertura
    public CashSession(Guid operatorId, string terminalId, decimal openingBalance)
    {
        Id = Guid.NewGuid();
        OperatorId = operatorId;
        TerminalId = terminalId;
        OpeningBalance = openingBalance;
        OpenedAt = DateTime.Now;
        Status = SessionStatus.Open;
    }

    // Método para Fechamento
    public void Close(decimal countedBalance, decimal systemBalance)
    {
        ClosingBalance = countedBalance;
        CalculatedBalance = systemBalance;
        ClosedAt = DateTime.Now;
        Status = SessionStatus.Closed;
        // Atualizar UpdatedAt se sua classe base Entity tiver
    }
}

```

#### B. CashTransaction (Sangrias e Suprimentos)

Registra quando o gerente coloca troco ou retira excesso de dinheiro.

```csharp
public class CashTransaction : Entity
{
    public Guid CashSessionId { get; private set; }
    public CashTransactionType Type { get; private set; }
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty; // Motivo
    public Guid OperatorId { get; private set; } // Quem realizou a ação
    public DateTime TransactionDate { get; private set; }
    public SyncState SyncState { get; private set; } = SyncState.Pending;

    public CashTransaction(Guid cashSessionId, CashTransactionType type, decimal amount, string description, Guid operatorId)
    {
        Id = Guid.NewGuid();
        CashSessionId = cashSessionId;
        Type = type;
        Amount = amount;
        Description = description;
        OperatorId = operatorId;
        TransactionDate = DateTime.Now;
    }
}

```

### 3. Alteração nas Entidades Existentes

Precisamos vincular a `Sale` à `CashSession`. Sem isso, se você tiver dois turnos no mesmo dia, não saberá de quem é a venda.

**Na classe `Sale`:**

```csharp
public class Sale : Entity
{
    // ... propriedades existentes ...

    // NOVO CAMPO: Vínculo obrigatório com o caixa aberto
    public Guid CashSessionId { get; private set; } 

    // Atualize o construtor ou método Factory para exigir o sessionId
    public static Sale Create(Guid cashSessionId, Guid operatorId /*...outros args...*/)
    {
        return new Sale
        {
            Id = Guid.NewGuid(),
            CashSessionId = cashSessionId,
            OperatorId = operatorId,
            SaleDate = DateTime.Now,
            Status = SaleStatus.Pending
            // ...
        };
    }
    
    // ... resto da classe
}

```

---

### 4. Lógica de Negócio (O Cálculo do Saldo)

O coração do módulo de caixa é saber calcular quanto deve ter na gaveta. Você deve criar um `CashManagementService` ou `SessionService` na sua camada Core ou Application.

A fórmula é:


Aqui está um esboço da lógica de cálculo (usando EF Core ou Dapper):

```csharp
public class CashBalanceResult
{
    public decimal OpeningBalance { get; set; }
    public decimal TotalSupply { get; set; }
    public decimal TotalBleed { get; set; }
    public decimal TotalSalesCash { get; set; }
    public decimal TotalSalesCard { get; set; } // Cartão não soma na gaveta, mas aparece no relatório
    
    // Quanto deve ter de dinheiro físico
    public decimal ExpectedCashBalance => (OpeningBalance + TotalSupply + TotalSalesCash) - TotalBleed;
}

public async Task<CashBalanceResult> CalculateSessionBalanceAsync(Guid sessionId)
{
    // 1. Buscar a Sessão
    var session = await _sessionRepo.GetByIdAsync(sessionId);
    
    // 2. Buscar Movimentações (Sangrias/Suprimentos)
    var transactions = await _transactionRepo.GetBySessionIdAsync(sessionId);
    
    // 3. Buscar Vendas daquela sessão (Somente FINALIZADAS)
    var sales = await _saleRepo.GetCompletedSalesBySessionIdAsync(sessionId);

    var result = new CashBalanceResult
    {
        OpeningBalance = session.OpeningBalance,
        TotalSupply = transactions.Where(t => t.Type == CashTransactionType.Supply).Sum(t => t.Amount),
        TotalBleed = transactions.Where(t => t.Type == CashTransactionType.Bleed).Sum(t => t.Amount),
        
        // CUIDADO AQUI: Só somar pagamentos em DINHEIRO
        TotalSalesCash = sales.SelectMany(s => s.Payments)
                              .Where(p => p.Method == PaymentMethod.Cash)
                              .Sum(p => p.Amount),

        // Opcional: Somar cartões para conferência de filipetas
        TotalSalesCard = sales.SelectMany(s => s.Payments)
                              .Where(p => p.Method == PaymentMethod.CreditCard || p.Method == PaymentMethod.DebitCard)
                              .Sum(p => p.Amount)
    };

    return result;
}

```

### 5. Fluxo de Uso no Avalonia (Passo a Passo)

#### Passo A: Bloqueio Inicial

No startup ou na `MainWindow`, verifique:

1. Existe uma `CashSession` com `Status = Open` e `TerminalId = Local`?
* **Sim:** Carregue o ID dela na memória (Singleton/State) e libere a tela de vendas.
* **Não:** Exiba um modal ou redirecione para a tela de **Abertura de Caixa**.



#### Passo B: Tela de Abertura

* Input: "Valor do Fundo de Troco" (Ex: R$ 100,00).
* Ação: Cria nova `CashSession` no SQLite.

#### Passo C: Sangria (Botão no Menu)

* O operador clica em "Sangria".
* Input: Valor (Ex: R$ 500,00) e Motivo (Ex: "Depósito Cofre").
* Ação:
1. Verifica se tem saldo suficiente (opcional, mas recomendado).
2. Salva `CashTransaction` (Type = Bleed).
3. **Importante:** Imprime comprovante na impressora térmica (via ESC/POS) para assinatura.



#### Passo D: Fechamento (Contagem Cega)

* O operador clica em "Fechar Caixa".
* Sistema exibe inputs:
* "Dinheiro em Gaveta:" [____]
* "Comprovantes Cartão:" [____]


* **O sistema NÃO mostra quanto deveria ter.**
* Ao confirmar, o sistema roda o método `CalculateSessionBalanceAsync`, compara com o input, salva na `CashSession` e muda status para `Closed`.
* Imprime o relatório de fechamento (Redução Z gerencial).

### Dicas de Implementação

1. **Dapper vs EF:** Para o cálculo do saldo (`CalculateSessionBalanceAsync`), use **Dapper**. Você precisará fazer JOINs entre `Sales` e `Payments` filtrando por `SessionId`. O EF Core traria todos os objetos para memória, o que ficaria lento se o caixa tiver 500 vendas no dia.
2. **Sincronização:** Configure o `Dotmim.Sync` para sincronizar `CashSession` e `CashTransaction` como **UploadOnly** (do Caixa para Nuvem). Raramente você precisa baixar sessões de outros caixas para o caixa atual.
3. **Segurança:** Operações de Sangria geralmente exigem permissão de Gerente (ou usuário Admin). Verifique a flag `IsAdmin` do `Operator` antes de salvar.