using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRelationshipPatientFromFuneral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationshipPatient",
                table: "FuneralAssistance");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RelationshipPatient",
                table: "FuneralAssistance",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
