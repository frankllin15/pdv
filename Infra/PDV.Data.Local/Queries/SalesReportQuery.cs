using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;
using PDV.Core.Interfaces.Queries;
using PDV.Shared.DTOs;
using PDV.Shared.Enums;

namespace PDV.Data.Local.Queries;

public class SalesReportQuery(string connectionString) : ISalesReportQuery
{
    private IDbConnection CreateConnection() => new SqliteConnection(connectionString);

    public async Task<IEnumerable<SalesByPeriodRow>> GetSalesByPeriodAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                DATE(s.SaleDate) as SaleDate,
                COUNT(*) as TotalSales,
                SUM(CASE WHEN s.Status = @Completed THEN 1 ELSE 0 END) as CompletedSales,
                SUM(CASE WHEN s.Status = @Cancelled THEN 1 ELSE 0 END) as CancelledSales,
                SUM(CASE WHEN s.Status = @Completed THEN s.Subtotal ELSE 0 END) as GrossRevenue,
                SUM(CASE WHEN s.Status = @Completed THEN s.Discount ELSE 0 END) as Discounts,
                SUM(CASE WHEN s.Status = @Completed THEN s.Total ELSE 0 END) as NetRevenue
            FROM Sales s
            WHERE s.SaleDate >= @StartDate AND s.SaleDate < @EndDate
            GROUP BY DATE(s.SaleDate)
            ORDER BY DATE(s.SaleDate)";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<SalesByPeriodQueryResult>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.ToString("o"),
            Completed = (int)SaleStatus.Completed,
            Cancelled = (int)SaleStatus.Cancelled
        });

        return results.Select(r =>
        {
            var avgTicket = r.CompletedSales > 0 ? r.NetRevenue / r.CompletedSales : 0m;
            return new SalesByPeriodRow(
                DateTime.Parse(r.SaleDate),
                r.TotalSales,
                r.CompletedSales,
                r.CancelledSales,
                r.GrossRevenue,
                r.Discounts,
                r.NetRevenue,
                avgTicket
            );
        });
    }

    public async Task<IEnumerable<RevenueByPaymentMethodRow>> GetRevenueByPaymentMethodAsync(
        DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                p.Method,
                SUM(p.Amount) as TotalAmount,
                COUNT(*) as TransactionCount
            FROM Payments p
            INNER JOIN Sales s ON p.SaleId = s.Id
            WHERE s.SaleDate >= @StartDate AND s.SaleDate < @EndDate
              AND s.Status = @Completed
            GROUP BY p.Method
            ORDER BY SUM(p.Amount) DESC";

        using var connection = CreateConnection();
        var results = await connection.QueryAsync<PaymentMethodQueryResult>(sql, new
        {
            StartDate = startDate.ToString("o"),
            EndDate = endDate.ToString("o"),
            Completed = (int)SaleStatus.Completed
        });

        var rows = results.ToList();
        var grandTotal = rows.Sum(r => r.TotalAmount);

        return rows.Select(r =>
        {
            var method = (PaymentMethod)r.Method;
            var percentage = grandTotal > 0 ? r.TotalAmount / grandTotal * 100 : 0m;
            return new RevenueByPaymentMethodRow(
                method,
                GetPaymentMethodName(method),
                r.TransactionCount,
                r.TotalAmount,
                Math.Round(percentage, 2)
            );
        });
    }

    private static string GetPaymentMethodName(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Dinheiro",
        PaymentMethod.CreditCard => "Cartão Crédito",
        PaymentMethod.DebitCard => "Cartão Débito",
        PaymentMethod.Pix => "PIX",
        PaymentMethod.FoodVoucher => "Vale Alimentação",
        PaymentMethod.MealVoucher => "Vale Refeição",
        PaymentMethod.Other => "Outros",
        _ => method.ToString()
    };

    private class SalesByPeriodQueryResult
    {
        public string SaleDate { get; set; } = string.Empty;
        public int TotalSales { get; set; }
        public int CompletedSales { get; set; }
        public int CancelledSales { get; set; }
        public decimal GrossRevenue { get; set; }
        public decimal Discounts { get; set; }
        public decimal NetRevenue { get; set; }
    }

    private class PaymentMethodQueryResult
    {
        public int Method { get; set; }
        public decimal TotalAmount { get; set; }
        public int TransactionCount { get; set; }
    }
}
