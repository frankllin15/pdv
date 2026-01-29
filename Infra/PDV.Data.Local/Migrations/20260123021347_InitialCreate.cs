using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PDV.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "REAL", nullable: false),
                    UnitOfMeasure = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false, defaultValue: "UN"),
                    StockQuantity = table.Column<decimal>(type: "REAL", nullable: false),
                    TaxCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    TaxRate = table.Column<decimal>(type: "REAL", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    SyncState = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Subtotal = table.Column<decimal>(type: "REAL", nullable: false),
                    Discount = table.Column<decimal>(type: "REAL", nullable: false),
                    Total = table.Column<decimal>(type: "REAL", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerDocument = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    OperatorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SyncState = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Method = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "REAL", nullable: false),
                    AuthorizationCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CardBrand = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SaleItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ProductDescription = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "REAL", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "REAL", nullable: false),
                    Discount = table.Column<decimal>(type: "REAL", nullable: false),
                    Total = table.Column<decimal>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleItems_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "CreatedAt", "Description", "IsActive", "ShortDescription", "StockQuantity", "TaxCode", "TaxRate", "UnitOfMeasure", "UnitPrice", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), "7891000100103", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Leite Integral 1L", true, "Leite 1L", 100m, "04011010", 12m, "UN", 5.99m, null },
                    { new Guid("11111111-1111-1111-1111-111111111102"), "7891000200109", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Arroz Tipo 1 5kg", true, "Arroz 5kg", 50m, "10063011", 7m, "UN", 24.90m, null },
                    { new Guid("11111111-1111-1111-1111-111111111103"), "7891000300106", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Feijão Carioca 1kg", true, "Feijão 1kg", 80m, "07133319", 7m, "UN", 8.49m, null },
                    { new Guid("11111111-1111-1111-1111-111111111104"), "7891000400103", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Açúcar Refinado 1kg", true, "Açúcar 1kg", 120m, "17019900", 7m, "UN", 4.99m, null },
                    { new Guid("11111111-1111-1111-1111-111111111105"), "7891000500100", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Café Torrado 500g", true, "Café 500g", 60m, "09012100", 7m, "UN", 15.90m, null },
                    { new Guid("11111111-1111-1111-1111-111111111106"), "7891000600107", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Óleo de Soja 900ml", true, "Óleo 900ml", 70m, "15079019", 7m, "UN", 7.99m, null },
                    { new Guid("11111111-1111-1111-1111-111111111107"), "7891000700104", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Macarrão Espaguete 500g", true, "Macarrão 500g", 90m, "19021100", 7m, "UN", 4.49m, null },
                    { new Guid("11111111-1111-1111-1111-111111111108"), "7891000800101", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Biscoito Cream Cracker 400g", true, "Biscoito 400g", 100m, "19053100", 7m, "UN", 5.49m, null },
                    { new Guid("11111111-1111-1111-1111-111111111109"), "7891000900108", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Refrigerante Cola 2L", true, "Refri 2L", 60m, "22021000", 7m, "UN", 8.99m, null },
                    { new Guid("11111111-1111-1111-1111-111111111110"), "7891001000100", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sabonete 90g", true, "Sabonete", 150m, "34011190", 18m, "UN", 2.99m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SaleId",
                table: "Payments",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropTable(
                name: "Sales");
        }
    }
}
