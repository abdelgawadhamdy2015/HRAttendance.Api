using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAttendance.Api.Migrations
{
    /// <inheritdoc />
    public partial class Init8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Employees",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "GradeArabic",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GradeEnglish",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "JoiningDate",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameArabic",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameEnglish",
                table: "Employees",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Employees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Reports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    OfficeNameArabic = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OfficeNameEnglish = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reports_Users_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasError = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeReports_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeReports_Reports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "Reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurrentYearStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeReportId = table.Column<int>(type: "int", nullable: false),
                    TransferredFromBeginningOfYear = table.Column<int>(type: "int", nullable: false),
                    ReceivedDuringMonth = table.Column<int>(type: "int", nullable: false),
                    CompletedCases = table.Column<int>(type: "int", nullable: false),
                    UnderInvestigation = table.Column<int>(type: "int", nullable: false),
                    TechnicalOfficeSent = table.Column<int>(type: "int", nullable: false),
                    TechnicalOfficeUnderCopying = table.Column<int>(type: "int", nullable: false),
                    BranchSent = table.Column<int>(type: "int", nullable: false),
                    BranchUnderCopying = table.Column<int>(type: "int", nullable: false),
                    CentralAdministrations = table.Column<int>(type: "int", nullable: false),
                    FollowUpCases = table.Column<int>(type: "int", nullable: false),
                    SpatialVariableCases = table.Column<int>(type: "int", nullable: false),
                    ReceivedFromOtherProsecutions = table.Column<int>(type: "int", nullable: false),
                    PendingDecision = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentYearStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurrentYearStatistics_EmployeeReports_EmployeeReportId",
                        column: x => x.EmployeeReportId,
                        principalTable: "EmployeeReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreviousYearStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeReportId = table.Column<int>(type: "int", nullable: false),
                    PendingDecision = table.Column<int>(type: "int", nullable: false),
                    TotalPreviousYearCases = table.Column<int>(type: "int", nullable: false),
                    CompletedCases = table.Column<int>(type: "int", nullable: false),
                    UnderInvestigation = table.Column<int>(type: "int", nullable: false),
                    TechnicalOfficeSent = table.Column<int>(type: "int", nullable: false),
                    TechnicalOfficeUnderCopying = table.Column<int>(type: "int", nullable: false),
                    BranchSent = table.Column<int>(type: "int", nullable: false),
                    BranchUnderCopying = table.Column<int>(type: "int", nullable: false),
                    CentralAdministrations = table.Column<int>(type: "int", nullable: false),
                    FollowUpCases = table.Column<int>(type: "int", nullable: false),
                    SpatialVariableCases = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviousYearStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreviousYearStatistics_EmployeeReports_EmployeeReportId",
                        column: x => x.EmployeeReportId,
                        principalTable: "EmployeeReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NameArabic",
                table: "Employees",
                column: "NameArabic");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NameEnglish",
                table: "Employees",
                column: "NameEnglish");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity_EntityId",
                table: "AuditLogs",
                columns: new[] { "Entity", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_CurrentYearStatistics_EmployeeReportId",
                table: "CurrentYearStatistics",
                column: "EmployeeReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeReports_EmployeeId",
                table: "EmployeeReports",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeReports_ReportId_EmployeeId",
                table: "EmployeeReports",
                columns: new[] { "ReportId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreviousYearStatistics_EmployeeReportId",
                table: "PreviousYearStatistics",
                column: "EmployeeReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_FinalizedByUserId",
                table: "Reports",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Year_Month",
                table: "Reports",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Reports_Year_Month_OfficeNameArabic",
                table: "Reports",
                columns: new[] { "Year", "Month", "OfficeNameArabic" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CurrentYearStatistics");

            migrationBuilder.DropTable(
                name: "PreviousYearStatistics");

            migrationBuilder.DropTable(
                name: "EmployeeReports");

            migrationBuilder.DropTable(
                name: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Employees_NameArabic",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_NameEnglish",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "GradeArabic",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "GradeEnglish",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "JoiningDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NameArabic",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NameEnglish",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Employees");
        }
    }
}
