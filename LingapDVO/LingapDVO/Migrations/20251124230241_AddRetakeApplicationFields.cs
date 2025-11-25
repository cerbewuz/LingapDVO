using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddRetakeApplicationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRetakeApplication",
                table: "OtherAssistance",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RetakeReason",
                table: "OtherAssistance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetakeRequestedAt",
                table: "OtherAssistance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetakeApplication",
                table: "HospitalAssistance",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RetakeReason",
                table: "HospitalAssistance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetakeRequestedAt",
                table: "HospitalAssistance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRetakeApplication",
                table: "FuneralAssistance",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RetakeReason",
                table: "FuneralAssistance",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetakeRequestedAt",
                table: "FuneralAssistance",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OtherAssistance_Status_Status2_CreatedAt",
                table: "OtherAssistance",
                columns: new[] { "Status", "Status2", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtherAssistance_Status3_CreatedAt",
                table: "OtherAssistance",
                columns: new[] { "Status3", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OtherAssistance_UserId_CreatedAt",
                table: "OtherAssistance",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalAssistance_Status_Status2_CreatedAt",
                table: "HospitalAssistance",
                columns: new[] { "Status", "Status2", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalAssistance_Status3_CreatedAt",
                table: "HospitalAssistance",
                columns: new[] { "Status3", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HospitalAssistance_UserId_CreatedAt",
                table: "HospitalAssistance",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralAssistance_Status_Status2_CreatedAt",
                table: "FuneralAssistance",
                columns: new[] { "Status", "Status2", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralAssistance_Status3_CreatedAt",
                table: "FuneralAssistance",
                columns: new[] { "Status3", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FuneralAssistance_UserId_CreatedAt",
                table: "FuneralAssistance",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionToken_UserId_FormType_CreatedAt",
                table: "FormSubmissionTokens",
                columns: new[] { "UserId", "FormType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FormSubmissionAuditLog_UserId_AttemptedAt",
                table: "FormSubmissionAuditLogs",
                columns: new[] { "UserId", "AttemptedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OtherAssistance_Status_Status2_CreatedAt",
                table: "OtherAssistance");

            migrationBuilder.DropIndex(
                name: "IX_OtherAssistance_Status3_CreatedAt",
                table: "OtherAssistance");

            migrationBuilder.DropIndex(
                name: "IX_OtherAssistance_UserId_CreatedAt",
                table: "OtherAssistance");

            migrationBuilder.DropIndex(
                name: "IX_HospitalAssistance_Status_Status2_CreatedAt",
                table: "HospitalAssistance");

            migrationBuilder.DropIndex(
                name: "IX_HospitalAssistance_Status3_CreatedAt",
                table: "HospitalAssistance");

            migrationBuilder.DropIndex(
                name: "IX_HospitalAssistance_UserId_CreatedAt",
                table: "HospitalAssistance");

            migrationBuilder.DropIndex(
                name: "IX_FuneralAssistance_Status_Status2_CreatedAt",
                table: "FuneralAssistance");

            migrationBuilder.DropIndex(
                name: "IX_FuneralAssistance_Status3_CreatedAt",
                table: "FuneralAssistance");

            migrationBuilder.DropIndex(
                name: "IX_FuneralAssistance_UserId_CreatedAt",
                table: "FuneralAssistance");

            migrationBuilder.DropIndex(
                name: "IX_FormSubmissionToken_UserId_FormType_CreatedAt",
                table: "FormSubmissionTokens");

            migrationBuilder.DropIndex(
                name: "IX_FormSubmissionAuditLog_UserId_AttemptedAt",
                table: "FormSubmissionAuditLogs");

            migrationBuilder.DropColumn(
                name: "IsRetakeApplication",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "RetakeReason",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "RetakeRequestedAt",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "IsRetakeApplication",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "RetakeReason",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "RetakeRequestedAt",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "IsRetakeApplication",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "RetakeReason",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "RetakeRequestedAt",
                table: "FuneralAssistance");
        }
    }
}
