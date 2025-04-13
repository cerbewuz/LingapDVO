using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class FirstMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Register",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fullname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phonenumber = table.Column<int>(type: "int", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ImageFilename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecurityQuestions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Securityanswer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Register", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Register_Email",
                table: "Register",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Register_Fullname",
                table: "Register",
                column: "Fullname",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Register_Phonenumber",
                table: "Register",
                column: "Phonenumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Register_Username",
                table: "Register",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Register");
        }
    }
}
