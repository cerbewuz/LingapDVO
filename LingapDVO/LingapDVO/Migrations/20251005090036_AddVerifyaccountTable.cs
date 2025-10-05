using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifyaccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Verifyaccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IDtype = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IDnumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
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
                    Barangay = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SecurityQuestions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Securityanswer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verifyaccount", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Verifyaccount");
        }
    }
}
