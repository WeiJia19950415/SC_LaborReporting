using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class updateLaborCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LaborCategories",
                table: "LaborCategories");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "LaborCategories");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "LaborCategories");

            migrationBuilder.RenameTable(
                name: "LaborCategories",
                newName: "AppLaborCategories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppLaborCategories",
                table: "AppLaborCategories",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "LaborCategoryDepartments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaborCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborCategoryDepartments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborCategoryDepartments_AppLaborCategories_LaborCategoryId",
                        column: x => x.LaborCategoryId,
                        principalTable: "AppLaborCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LaborCategoryRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LaborCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborCategoryRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborCategoryRoles_AppLaborCategories_LaborCategoryId",
                        column: x => x.LaborCategoryId,
                        principalTable: "AppLaborCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LaborCategoryDepartments_LaborCategoryId",
                table: "LaborCategoryDepartments",
                column: "LaborCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborCategoryRoles_LaborCategoryId",
                table: "LaborCategoryRoles",
                column: "LaborCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LaborCategoryDepartments");

            migrationBuilder.DropTable(
                name: "LaborCategoryRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppLaborCategories",
                table: "AppLaborCategories");

            migrationBuilder.RenameTable(
                name: "AppLaborCategories",
                newName: "LaborCategories");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "LaborCategories",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "LaborCategories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LaborCategories",
                table: "LaborCategories",
                column: "Id");
        }
    }
}
