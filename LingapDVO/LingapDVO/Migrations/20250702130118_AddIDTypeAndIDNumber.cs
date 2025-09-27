using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddIDTypeAndIDNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IDnumber",
                table: "Useraccount",
                type: "int",
                maxLength: 100,
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "IDtype",
                table: "Useraccount",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IDnumber",
                table: "Useraccount");

            migrationBuilder.DropColumn(
                name: "IDtype",
                table: "Useraccount");
        }
    }
}
