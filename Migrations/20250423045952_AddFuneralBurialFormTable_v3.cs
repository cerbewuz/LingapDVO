using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddFuneralBurialFormTable_v3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Funeralburialform",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Middlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Suffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Brgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sex = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhilHealthNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Dateofbirth = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Age = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RLastname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RFirstname = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RMiddlename = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RSuffix = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RBlkLotStreet = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RSubVill = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RBrgy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RDistrict = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RelationshipPatient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ContactNo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Typeassistance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ForCMOPERSONNEL = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Validfrontimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ValidBackimage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorPrescription = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeathCertificate = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Processby = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funeralburialform", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Funeralburialform");
        }
    }
}
