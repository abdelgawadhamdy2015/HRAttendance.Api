using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HRAttendance.Api.Migrations;

/// <inheritdoc />
public partial class Init2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Permissions_Employees_EmployeeId",
            table: "Permissions");

        migrationBuilder.DropIndex(
            name: "IX_Permissions_EmployeeId",
            table: "Permissions");

        migrationBuilder.DropColumn(
            name: "Date",
            table: "Permissions");

        migrationBuilder.DropColumn(
            name: "EmployeeId",
            table: "Permissions");

        migrationBuilder.DropColumn(
            name: "From",
            table: "Permissions");

        migrationBuilder.DropColumn(
            name: "To",
            table: "Permissions");

        migrationBuilder.RenameColumn(
            name: "Reason",
            table: "Permissions",
            newName: "Name");

        migrationBuilder.AddColumn<string>(
            name: "Description",
            table: "Permissions",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "PermissionRequests",
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
                table.PrimaryKey("PK_PermissionRequests", x => x.Id);
                table.ForeignKey(
                    name: "FK_PermissionRequests_Employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "Employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Username = table.Column<string>(type: "nvarchar(450)", nullable: false),
                Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserPermissions",
            columns: table => new
            {
                UserId = table.Column<int>(type: "int", nullable: false),
                PermissionId = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPermissions", x => new { x.UserId, x.PermissionId });
                table.ForeignKey(
                    name: "FK_UserPermissions_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserPermissions_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PermissionRequests_EmployeeId",
            table: "PermissionRequests",
            column: "EmployeeId");

        migrationBuilder.CreateIndex(
            name: "IX_UserPermissions_PermissionId",
            table: "UserPermissions",
            column: "PermissionId");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Users_Username",
            table: "Users",
            column: "Username",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PermissionRequests");

        migrationBuilder.DropTable(
            name: "UserPermissions");

        migrationBuilder.DropTable(
            name: "Users");

        migrationBuilder.DropColumn(
            name: "Description",
            table: "Permissions");

        migrationBuilder.RenameColumn(
            name: "Name",
            table: "Permissions",
            newName: "Reason");

        migrationBuilder.AddColumn<DateOnly>(
            name: "Date",
            table: "Permissions",
            type: "date",
            nullable: false,
            defaultValue: new DateOnly(1, 1, 1));

        migrationBuilder.AddColumn<int>(
            name: "EmployeeId",
            table: "Permissions",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<TimeOnly>(
            name: "From",
            table: "Permissions",
            type: "time",
            nullable: false,
            defaultValue: new TimeOnly(0, 0, 0));

        migrationBuilder.AddColumn<TimeOnly>(
            name: "To",
            table: "Permissions",
            type: "time",
            nullable: false,
            defaultValue: new TimeOnly(0, 0, 0));

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_EmployeeId",
            table: "Permissions",
            column: "EmployeeId");

        migrationBuilder.AddForeignKey(
            name: "FK_Permissions_Employees_EmployeeId",
            table: "Permissions",
            column: "EmployeeId",
            principalTable: "Employees",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
