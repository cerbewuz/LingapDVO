using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddFullDetailsToHospitalBill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pdffilname",
                table: "FillupformHospitalBill",
                newName: "Validfrontimage");

            migrationBuilder.AddColumn<string>(
                name: "Age",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlkLotStreet",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Brgy",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactNo",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Dateofbirth",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DoctorPrescription",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Firstname",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lastname",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Middlename",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhilHealth",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhilHealthNo",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RBlkLotStreet",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RBrgy",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RDistrict",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RFirstname",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RLastname",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RMiddlename",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RSubVill",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RSuffix",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RelationshipPatient",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SubVill",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Typeassistance",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ValidBackimage",
                table: "FillupformHospitalBill",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "BlkLotStreet",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Brgy",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "ContactNo",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Dateofbirth",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "District",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "DoctorPrescription",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Firstname",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Lastname",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Middlename",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "PhilHealth",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "PhilHealthNo",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RBlkLotStreet",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RBrgy",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RDistrict",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RFirstname",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RLastname",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RMiddlename",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RSubVill",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RSuffix",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "RelationshipPatient",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Sex",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "SubVill",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "Typeassistance",
                table: "FillupformHospitalBill");

            migrationBuilder.DropColumn(
                name: "ValidBackimage",
                table: "FillupformHospitalBill");

            migrationBuilder.RenameColumn(
                name: "Validfrontimage",
                table: "FillupformHospitalBill",
                newName: "Pdffilname");
        }
    }
}
