using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class CreateUseraccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Useraccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Profilepicture = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FrontID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BackID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phonenumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    SecurityQuestions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Securityanswer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Useraccount", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Useraccount_Email",
                table: "Useraccount",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Useraccount_Phonenumber",
                table: "Useraccount",
                column: "Phonenumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Useraccount_Username",
                table: "Useraccount",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Useraccount");
        }
    }
}
