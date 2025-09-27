using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Register",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Register");
        }
    }
}
