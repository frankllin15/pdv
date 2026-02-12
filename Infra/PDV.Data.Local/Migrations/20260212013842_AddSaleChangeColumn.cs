using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PDV.Data.Local.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleChangeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Change",
                table: "Sales",
                type: "REAL",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Change",
                table: "Sales");
        }
    }
}
