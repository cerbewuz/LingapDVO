using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class RemovePhonenumberFromRegisterAcc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RegisterAcc_Phonenumber",
                table: "RegisterAcc");

            migrationBuilder.DropColumn(
                name: "Phonenumber",
                table: "RegisterAcc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Phonenumber",
                table: "RegisterAcc",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RegisterAcc_Phonenumber",
                table: "RegisterAcc",
                column: "Phonenumber",
                unique: true);
        }
    }
}
