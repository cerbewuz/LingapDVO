using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class RenameFormTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename tables instead of dropping and recreating to preserve data
            migrationBuilder.RenameTable(
                name: "FillupformHospitalBill",
                newName: "HospitalAssistance");

            migrationBuilder.RenameTable(
                name: "Funeralburialform",
                newName: "FuneralAssistance");

            migrationBuilder.RenameTable(
                name: "Medicalandlabform",
                newName: "OtherAssistance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the table renames
            migrationBuilder.RenameTable(
                name: "HospitalAssistance",
                newName: "FillupformHospitalBill");

            migrationBuilder.RenameTable(
                name: "FuneralAssistance",
                newName: "Funeralburialform");

            migrationBuilder.RenameTable(
                name: "OtherAssistance",
                newName: "Medicalandlabform");
        }
    }
}
