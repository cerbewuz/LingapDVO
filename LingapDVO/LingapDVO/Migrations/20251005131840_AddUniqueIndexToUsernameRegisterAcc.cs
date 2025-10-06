using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToUsernameRegisterAcc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a unique index on the Username column
            migrationBuilder.CreateIndex(
                name: "IX_RegisterAcc_Username",
                table: "RegisterAcc",
                column: "Username",
                unique: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the unique index if migration is rolled back
            migrationBuilder.DropIndex(
                name: "IX_RegisterAcc_Username",
                table: "RegisterAcc");
        }
    }
}
