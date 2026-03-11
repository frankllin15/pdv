Para acomodar múltiplos relatórios e diferentes formatos de saída (CSV, PDF, ESC/POS) sem ferir os princípios do DDD (onde o domínio não deve saber o que é um CSV ou uma impressora térmica), a melhor abordagem é combinar o **Padrão CQRS** (lado de leitura) com o **Padrão Strategy** para a geração de formatos na camada de Infraestrutura.

Aqui está a estruturação ideal em 4 passos:

### 1. Camada de Aplicação (Application Layer)

Esta camada define os "contratos" de leitura. Você cria DTOs (Data Transfer Objects) específicos para o resultado de cada relatório, totalmente desconectados das Entidades de Domínio.

```csharp
// 1. O DTO de Saída (Focado apenas no que o relatório precisa)
public record ProductAbcCurveDto(
    string Barcode, 
    string Description, 
    decimal TotalQuantity, 
    decimal TotalRevenue
);

// 2. O Parâmetro de Busca (Query)
public record GetAbcCurveQuery(DateTime StartDate, DateTime EndDate);

// 3. A Interface do Repositório de Leitura
public interface ISalesReportQueries
{
    Task<IEnumerable<ProductAbcCurveDto>> GetProductAbcCurveAsync(GetAbcCurveQuery query);
}

```

### 2. Camada de Infraestrutura de Dados (Data Infrastructure)

Aqui você implementa a interface usando Dapper. Como você está utilizando SQLite no seu PDV, as consultas devem ser otimizadas para o dialeto dele.

```csharp
public class SalesReportQueries : ISalesReportQueries
{
    private readonly IDbConnection _connection;

    public SalesReportQueries(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<IEnumerable<ProductAbcCurveDto>> GetProductAbcCurveAsync(GetAbcCurveQuery query)
    {
        // SQL otimizado focado apenas em leitura
        const string sql = @"
            SELECT 
                p.Barcode, 
                p.Description, 
                SUM(si.Quantity) as TotalQuantity, 
                SUM(si.Total) as TotalRevenue
            FROM SaleItems si
            INNER JOIN Products p ON p.Id = si.ProductId
            INNER JOIN Sales s ON s.Id = si.SaleId
            WHERE s.SaleDate >= @StartDate 
              AND s.SaleDate <= @EndDate 
              AND s.Status = 2 /* Apenas vendas Completed */
            GROUP BY p.Id, p.Barcode, p.Description
            ORDER BY TotalRevenue DESC";

        return await _connection.QueryAsync<ProductAbcCurveDto>(sql, query);
    }
}

```

### 3. Camada de Infraestrutura de Exportação (Cross-Cutting ou Export Infrastructure)

O segredo para lidar com múltiplos formatos é extrair a lógica de formatação para serviços dedicados usando o padrão Strategy. A sua aplicação solicita os dados via Dapper e os entrega para o "Formatador" correto.

```csharp
// 1. Interface Genérica de Exportação
public interface IReportExporter
{
    // Retorna um array de bytes (útil para download de CSV/PDF)
    byte[] ExportToCsv<T>(IEnumerable<T> data);
    byte[] ExportToPdf<T>(IEnumerable<T> data, string reportTitle);
}

// Interface Específica para Impressão Térmica (pois não gera um arquivo, gera um comando)
public interface IThermalPrinterService
{
    void PrintCashSessionSummary(CashSessionSummaryDto summary, string printerName);
}

```

Implementação de exemplo do CSV (usando uma biblioteca como `CsvHelper` ou montando manualmente):

```csharp
public class ReportExporter : IReportExporter
{
    public byte[] ExportToCsv<T>(IEnumerable<T> data)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        
        csv.WriteRecords(data);
        writer.Flush();
        
        return memoryStream.ToArray();
    }

    public byte[] ExportToPdf<T>(IEnumerable<T> data, string reportTitle)
    {
        // Implementação usando QuestPDF ou iText7
        throw new NotImplementedException();
    }
}

```

### 4. Orquestração (API ou ViewModel do Avalonia)

Na ponta (seu endpoint da API ou o ViewModel da sua interface gráfica no Avalonia), você junta a consulta de dados com o gerador de formato.

```csharp
public class ReportAppService
{
    private readonly ISalesReportQueries _queries;
    private readonly IReportExporter _exporter;

    public ReportAppService(ISalesReportQueries queries, IReportExporter exporter)
    {
        _queries = queries;
        _exporter = exporter;
    }

    public async Task<byte[]> GenerateAbcCurveCsvAsync(DateTime start, DateTime end)
    {
        // 1. Busca os dados limpos com Dapper
        var query = new GetAbcCurveQuery(start, end);
        var data = await _queries.GetProductAbcCurveAsync(query);

        // 2. Transforma no formato desejado
        return _exporter.ExportToCsv(data);
    }
}

```

### Resumo das Vantagens desta Estrutura:

1. **Performance:** O Dapper vai direto ao ponto. Você pode usar views, índices ou queries complexas sem se preocupar com o mapeamento do EF Core.
2. **Escalabilidade de Formatos:** Se amanhã pedirem para exportar em Excel (`.xlsx`), você cria apenas um novo método no `IReportExporter`, sem tocar na regra de negócios ou no SQL.
3. **Isolamento de Responsabilidade:** O Domínio foca em regras de negócio (calcular troco, validar impostos). Os relatórios vivem em paralelo, apenas consumindo o que já foi consolidado.

Quer focar em como estruturar a classe que envia os comandos ESC/POS direto para a impressora não fiscal, ou prefere ver a implementação detalhada do `CsvHelper` na Infraestrutura?