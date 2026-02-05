using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalAccessKey",
                table: "Sales",
                type: "TEXT",
                maxLength: 44,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalNumber",
                table: "Sales",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalSeries",
                table: "Sales",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalStatus",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Cest",
                table: "Products",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "Products",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cst",
                table: "Products",
                type: "TEXT",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxOrigin",
                table: "Products",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"),
                columns: new[] { "Cest", "Cfop", "Cst" },
                values: new object[] { null, null, null });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalConfigurations");

            migrationBuilder.DropTable(
                name: "FiscalTransactions");

            migrationBuilder.DropColumn(
                name: "FiscalAccessKey",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "FiscalNumber",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "FiscalSeries",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "FiscalStatus",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "Cest",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Cfop",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Cst",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TaxOrigin",
                table: "Products");
        }
    }
}
