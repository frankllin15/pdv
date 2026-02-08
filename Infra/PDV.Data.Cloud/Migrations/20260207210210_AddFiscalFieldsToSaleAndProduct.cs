using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Cloud.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalFieldsToSaleAndProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FiscalAccessKey",
                table: "Sales",
                type: "nvarchar(44)",
                maxLength: 44,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalNumber",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalSeries",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FiscalStatus",
                table: "Sales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Cest",
                table: "Products",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "Products",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cst",
                table: "Products",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxOrigin",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
