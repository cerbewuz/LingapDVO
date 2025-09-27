using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToAdminaccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Adminaccount",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Adminaccount");
        }
    }
}
