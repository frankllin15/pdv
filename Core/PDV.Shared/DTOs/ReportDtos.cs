using PDV.Shared.Enums;

namespace PDV.Shared.DTOs;

public record SalesByPeriodRow(
    DateTime Date,
    int TotalSales,
    int CompletedSales,
    int CancelledSales,
    decimal GrossRevenue,
    decimal Discounts,
    decimal NetRevenue,
    decimal AverageTicket
);

public record RevenueByPaymentMethodRow(
    PaymentMethod PaymentMethod,
    string PaymentMethodName,
    int TransactionCount,
    decimal TotalAmount,
    decimal Percentage
);

public record StockPositionRow(
    string Barcode,
    string Description,
    string UnitOfMeasure,
    decimal UnitPrice,
    decimal StockQuantity,
    decimal StockValue
);
