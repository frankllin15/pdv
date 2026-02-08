using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class ChangeIdTypeToBinary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OperatorId",
                table: "Sales",
                type: "BLOB",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Sales",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "SaleItems",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "SaleItems",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SaleItems",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Products",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "Payments",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Payments",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Operators",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "FiscalTransactions",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FiscalTransactions",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "OperatorId",
                table: "FiscalReprintLogs",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "FiscalTransactionId",
                table: "FiscalReprintLogs",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FiscalReprintLogs",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FiscalConfigurations",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "OperatorId",
                table: "Sales",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "BLOB",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Sales",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SaleItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Products",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Operators",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaleId",
                table: "FiscalTransactions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FiscalTransactions",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "OperatorId",
                table: "FiscalReprintLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "FiscalTransactionId",
                table: "FiscalReprintLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FiscalReprintLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "FiscalConfigurations",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "BLOB");
        }
    }
}
