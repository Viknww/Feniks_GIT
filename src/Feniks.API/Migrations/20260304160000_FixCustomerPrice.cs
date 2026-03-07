using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feniks.API.Migrations
{
    /// <inheritdoc />
    public partial class FixCustomerPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "ReferenceItems",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "ReferenceItems");
        }
    }
}
