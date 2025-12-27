using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddAdditionalInformationFieldsToAssistanceTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DoctorContactDetail",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentBrand",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentCategory",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentCost",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentName",
                table: "OtherAssistance",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EquipmentQuantity",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LaboratoryCenterAddress",
                table: "OtherAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LaboratoryCenterName",
                table: "OtherAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicineCost",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicineName",
                table: "OtherAssistance",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicineQuantity",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrescribingDoctor",
                table: "OtherAssistance",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestCost",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestName",
                table: "OtherAssistance",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TestOtherInfo",
                table: "OtherAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TherapyFacilityAddress",
                table: "OtherAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TherapyFacilityContact",
                table: "OtherAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TherapyFacilityName",
                table: "OtherAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TherapyType",
                table: "OtherAssistance",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdmissionDate",
                table: "HospitalAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiagnosisMedicalCondition",
                table: "HospitalAssistance",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DischargeDate",
                table: "HospitalAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalBillCost",
                table: "HospitalAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalFacilityAddress",
                table: "HospitalAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalFacilityName",
                table: "HospitalAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WardRoomType",
                table: "HospitalAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BurialCremationDate",
                table: "FuneralAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BurialCremationTime",
                table: "FuneralAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BurialCremationType",
                table: "FuneralAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CauseOfDeath",
                table: "FuneralAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DateOfDeath",
                table: "FuneralAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeceasedPersonName",
                table: "FuneralAssistance",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuneralHomeAddress",
                table: "FuneralAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FuneralHomeName",
                table: "FuneralAssistance",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationshipToDeceased",
                table: "FuneralAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TimeOfDeath",
                table: "FuneralAssistance",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoctorContactDetail",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "EquipmentBrand",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "EquipmentCategory",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "EquipmentCost",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "EquipmentName",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "EquipmentQuantity",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "LaboratoryCenterAddress",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "LaboratoryCenterName",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "MedicineCost",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "MedicineName",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "MedicineQuantity",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "PrescribingDoctor",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TestCost",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TestName",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TestOtherInfo",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TherapyFacilityAddress",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TherapyFacilityContact",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TherapyFacilityName",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "TherapyType",
                table: "OtherAssistance");

            migrationBuilder.DropColumn(
                name: "AdmissionDate",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "DiagnosisMedicalCondition",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "DischargeDate",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "HospitalBillCost",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "HospitalFacilityAddress",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "HospitalFacilityName",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "WardRoomType",
                table: "HospitalAssistance");

            migrationBuilder.DropColumn(
                name: "BurialCremationDate",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "BurialCremationTime",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "BurialCremationType",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "CauseOfDeath",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "DateOfDeath",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "DeceasedPersonName",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "FuneralHomeAddress",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "FuneralHomeName",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "RelationshipToDeceased",
                table: "FuneralAssistance");

            migrationBuilder.DropColumn(
                name: "TimeOfDeath",
                table: "FuneralAssistance");
        }
    }
}
