using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Cloud.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FiscalConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaxId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TradeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StateRegistration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CityCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddressNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Neighborhood = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TaxRegime = table.Column<int>(type: "int", nullable: false),
                    Series = table.Column<int>(type: "int", nullable: false),
                    NextNumber = table.Column<int>(type: "int", nullable: false),
                    CertificatePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CertificatePassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CscToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CscId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsProduction = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FiscalTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccessKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Series = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Protocol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: true),
                    StatusMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XmlRequest = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    XmlResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsContingency = table.Column<bool>(type: "bit", nullable: false),
                    AuthorizationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationProtocol = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CancellationJustification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalTransactions_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FiscalReprintLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReprintedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReprintNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FiscalReprintLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FiscalReprintLogs_FiscalTransactions_FiscalTransactionId",
                        column: x => x.FiscalTransactionId,
                        principalTable: "FiscalTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FiscalReprintLogs_Operators_OperatorId",
                        column: x => x.OperatorId,
                        principalTable: "Operators",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReprintLogs_FiscalTransactionId",
                table: "FiscalReprintLogs",
                column: "FiscalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalReprintLogs_OperatorId",
                table: "FiscalReprintLogs",
                column: "OperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_FiscalTransactions_SaleId",
                table: "FiscalTransactions",
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
                name: "FiscalTransactions");
        }
    }
}
