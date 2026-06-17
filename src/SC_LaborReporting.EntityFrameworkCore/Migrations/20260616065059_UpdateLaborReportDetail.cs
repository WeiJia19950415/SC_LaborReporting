using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLaborReportDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreationTime",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "LastModificationTime",
                table: "AppLaborReportDetails");

            migrationBuilder.RenameColumn(
                name: "LastModifierId",
                table: "AppLaborReportDetails",
                newName: "ProjectRoleId");

            migrationBuilder.AddColumn<int>(
                name: "LaborClass",
                table: "AppLaborReportDetails",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProjectCode",
                table: "AppLaborReportDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectName",
                table: "AppLaborReportDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProjectRoleName",
                table: "AppLaborReportDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LaborClass",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "ProjectCode",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "ProjectName",
                table: "AppLaborReportDetails");

            migrationBuilder.DropColumn(
                name: "ProjectRoleName",
                table: "AppLaborReportDetails");

            migrationBuilder.RenameColumn(
                name: "ProjectRoleId",
                table: "AppLaborReportDetails",
                newName: "LastModifierId");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreationTime",
                table: "AppLaborReportDetails",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "AppLaborReportDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                table: "AppLaborReportDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "AppLaborReportDetails",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AppLaborReportDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModificationTime",
                table: "AppLaborReportDetails",
                type: "datetime2",
                nullable: true);
        }
    }
}
