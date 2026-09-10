using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAttendance.Api.Migrations;

/// <inheritdoc />
public partial class Init : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Employees",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                JobTitle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Department = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Employees", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AttendanceRecords",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CheckIn = table.Column<TimeOnly>(type: "time", nullable: true),
                CheckOut = table.Column<TimeOnly>(type: "time", nullable: true),
                LateMinutes = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_AttendanceRecords_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Missions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                Location = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Missions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Missions_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                EmployeeId = table.Column<int>(type: "int", nullable: false),
                Date = table.Column<DateOnly>(type: "date", nullable: false),
                From = table.Column<TimeOnly>(type: "time", nullable: false),
                To = table.Column<TimeOnly>(type: "time", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(max)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Permissions", x => x.Id);
                table.ForeignKey(
                    name: "FK_Permissions_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AttendanceRecords_EmployeeId_Date",
            table: "AttendanceRecords",
            columns: new[] { "EmployeeId", "Date" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Missions_EmployeeId",
            table: "Missions",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_EmployeeId",
            table: "Permissions",
            column: "EmployeeId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AttendanceRecords");

        migrationBuilder.DropTable(
            name: "Missions");

        migrationBuilder.DropTable(
            name: "Notifications");

        migrationBuilder.DropTable(
            name: "Permissions");

        migrationBuilder.DropTable(
            name: "Employees");
    }
}
