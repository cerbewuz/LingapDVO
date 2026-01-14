using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDistrictFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "RDistrict",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "District",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "RDistrict",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "District",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "RDistrict",
                table: "FuneralAssistance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "OtherAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RDistrict",
                table: "OtherAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "HospitalAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RDistrict",
                table: "HospitalAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "FuneralAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RDistrict",
                table: "FuneralAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
