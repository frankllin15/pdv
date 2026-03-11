using PDV.Shared.DTOs;

namespace PDV.Desktop.Services;

public static class CashSessionReceiptBuilder
{
    public static byte[] BuildClosingReceipt(
        CashBalanceResultDto balance,
        decimal countedBalance,
        string operatorName,
        string terminalId,
        DateTime openedAt,
        DateTime closedAt)
    {
        var difference = countedBalance - balance.ExpectedCashBalance;

        var builder = new EscPosBuilder()
            .Initialize()
            .Align(TextAlignment.Center)
            .Bold(true)
            .FontSize(2, 2)
            .TextLine("FECHAMENTO DE CAIXA")
            .FontSize(1, 1)
            .Bold(false)
            .LineFeed()
            .Separator('=')
            .Align(TextAlignment.Left)
            .TwoColumns("Operador:", operatorName)
            .TwoColumns("Terminal:", terminalId)
            .TwoColumns("Abertura:", openedAt.ToString("dd/MM/yyyy HH:mm"))
            .TwoColumns("Fechamento:", closedAt.ToString("dd/MM/yyyy HH:mm"))
            .Separator()
            .Align(TextAlignment.Center)
            .Bold(true)
            .TextLine("MOVIMENTACAO")
            .Bold(false)
            .Align(TextAlignment.Left)
            .Separator()
            .TwoColumns("Saldo Abertura:", FormatCurrency(balance.OpeningBalance))
            .TwoColumns("(+) Suprimentos:", FormatCurrency(balance.TotalSupply))
            .TwoColumns("(-) Sangrias:", FormatCurrency(balance.TotalBleed))
            .Separator()
            .Align(TextAlignment.Center)
            .Bold(true)
            .TextLine("VENDAS POR FORMA PGTO")
            .Bold(false)
            .Align(TextAlignment.Left)
            .Separator()
            .TwoColumns("Dinheiro:", FormatCurrency(balance.TotalSalesCash))
            .TwoColumns("Cartao:", FormatCurrency(balance.TotalSalesCard))
            .TwoColumns("PIX:", FormatCurrency(balance.TotalSalesPix))
            .TwoColumns("Outros:", FormatCurrency(balance.TotalSalesOther));

        var totalSales = balance.TotalSalesCash + balance.TotalSalesCard
                       + balance.TotalSalesPix + balance.TotalSalesOther;

        builder
            .Separator()
            .Bold(true)
            .TwoColumns("TOTAL VENDAS:", FormatCurrency(totalSales))
            .Bold(false)
            .Separator('=')
            .Bold(true)
            .TwoColumns("SALDO ESPERADO:", FormatCurrency(balance.ExpectedCashBalance))
            .TwoColumns("SALDO CONTADO:", FormatCurrency(countedBalance))
            .Separator()
            .TwoColumns("DIFERENCA:", FormatCurrency(difference))
            .Bold(false)
            .LineFeed(3)
            .Align(TextAlignment.Center)
            .TextLine("______________________________")
            .TextLine(operatorName)
            .LineFeed(2)
            .FeedAndCut(3);

        return builder.Build();
    }

    private static string FormatCurrency(decimal value) => $"R$ {value:N2}";
}
