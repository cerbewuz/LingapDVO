using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LingapDVO.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Office = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ServiceAvailed = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TypeOfClient = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AssistanceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssistanceId = table.Column<int>(type: "int", nullable: true),
                    Q1_CCKnowledge = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Q2_CCVisibility = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Q3_CCHelpfulness = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    R1_ServiceSatisfaction = table.Column<int>(type: "int", nullable: true),
                    R2_TimeSpent = table.Column<int>(type: "int", nullable: true),
                    R3_ProcessFollowed = table.Column<int>(type: "int", nullable: true),
                    R4_ProcessSimplicity = table.Column<int>(type: "int", nullable: true),
                    R5_InformationAccess = table.Column<int>(type: "int", nullable: true),
                    R6_FairPayment = table.Column<int>(type: "int", nullable: true),
                    R7_Fairness = table.Column<int>(type: "int", nullable: true),
                    R8_EmployeeCourtesy = table.Column<int>(type: "int", nullable: true),
                    Commendation = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Suggestion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Request = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Complaint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Signature = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Feedbacks");
        }
    }
}
