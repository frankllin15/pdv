using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalReprintLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FiscalReprintLogs");
        }
    }
}
