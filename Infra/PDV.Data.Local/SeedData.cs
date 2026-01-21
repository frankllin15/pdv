using PDV.Core.Entities;
using PDV.Data.Local.Context;

namespace PDV.Data.Local;

public static class SeedData
{
    public static async Task InitializeAsync(PdvDbContext context)
    {
        if (context.Products.Any())
            return;

        var products = new List<Product>
        {
            new Product("7891000100103", "Leite Integral 1L", 5.99m, "Leite 1L", "UN", 100, "04011010", 12),
            new Product("7891000200109", "Arroz Tipo 1 5kg", 24.90m, "Arroz 5kg", "UN", 50, "10063011", 7),
            new Product("7891000300106", "Feijão Carioca 1kg", 8.49m, "Feijão 1kg", "UN", 80, "07133319", 7),
            new Product("7891000400103", "Açúcar Refinado 1kg", 4.99m, "Açúcar 1kg", "UN", 120, "17019900", 7),
            new Product("7891000500100", "Café Torrado 500g", 15.90m, "Café 500g", "UN", 60, "09012100", 7),
            new Product("7891000600107", "Óleo de Soja 900ml", 7.99m, "Óleo 900ml", "UN", 70, "15079019", 7),
            new Product("7891000700104", "Macarrão Espaguete 500g", 4.49m, "Macarrão 500g", "UN", 90, "19021100", 7),
            new Product("7891000800101", "Biscoito Cream Cracker 400g", 5.49m, "Biscoito 400g", "UN", 100, "19053100", 7),
            new Product("7891000900108", "Refrigerante Cola 2L", 8.99m, "Refri 2L", "UN", 60, "22021000", 7),
            new Product("7891001000100", "Sabonete 90g", 2.99m, "Sabonete", "UN", 150, "34011190", 18)
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
