using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class InitMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaxId = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    LegalName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    TradeName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    StateRegistration = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    CityCode = table.Column<string>(type: "TEXT", maxLength: 7, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AddressNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Neighborhood = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    TaxRegime = table.Column<int>(type: "INTEGER", nullable: false),
                    Series = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    NextNumber = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CertificatePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CertificatePassword = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CscToken = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CscId = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    IsProduction = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operators",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PinHash = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LastLoginAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operators", x => x.Id);
                });

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
                    Cfop = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Cest = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    TaxOrigin = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    Cst = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
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
                    FiscalStatus = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    FiscalAccessKey = table.Column<string>(type: "TEXT", maxLength: 44, nullable: true),
                    FiscalNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    FiscalSeries = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SaleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccessKey = table.Column<string>(type: "TEXT", maxLength: 44, nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Series = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Protocol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusMessage = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    XmlRequest = table.Column<string>(type: "TEXT", nullable: true),
                    XmlResponse = table.Column<string>(type: "TEXT", nullable: true),
                    IsContingency = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AuthorizationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancellationProtocol = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CancellationJustification = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalTransactions_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateTable(
                name: "FiscalReprintLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FiscalTransactionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OperatorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReprintedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ReprintNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalReprintLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalReprintLogs_FiscalTransactions_FiscalTransactionId",
                        column: x => x.FiscalTransactionId,
                        principalTable: "FiscalTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FiscalReprintLogs_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalConfigurations_IsActive",
                table: "FiscalConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalConfigurations_TaxId",
                table: "FiscalConfigurations",
                column: "TaxId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReprintLogs_FiscalTransactionId",
                table: "FiscalReprintLogs",
                column: "FiscalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReprintLogs_OperatorId",
                table: "FiscalReprintLogs",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReprintLogs_ReprintedAt",
                table: "FiscalReprintLogs",
                column: "ReprintedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalTransactions_AccessKey",
                table: "FiscalTransactions",
                column: "AccessKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FiscalTransactions_IsContingency",
                table: "FiscalTransactions",
                column: "IsContingency");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalTransactions_SaleId",
                table: "FiscalTransactions",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalTransactions_Status",
                table: "FiscalTransactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Operators_Code",
                table: "Operators",
                column: "Code",
                unique: true);

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
                name: "FiscalConfigurations");

            migrationBuilder.DropTable(
                name: "FiscalReprintLogs");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "SaleItems");

            migrationBuilder.DropTable(
                name: "FiscalTransactions");

            migrationBuilder.DropTable(
                name: "Operators");

            migrationBuilder.DropTable(
                name: "Sales");
        }
    }
}
