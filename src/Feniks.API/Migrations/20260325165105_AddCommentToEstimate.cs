using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Feniks.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCommentToEstimate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "Estimates",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Comment",
                table: "Estimates");
        }
    }
}
