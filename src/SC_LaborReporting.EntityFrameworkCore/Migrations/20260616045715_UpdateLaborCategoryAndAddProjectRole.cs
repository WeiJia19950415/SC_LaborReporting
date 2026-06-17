using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLaborCategoryAndAddProjectRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaborCategoryDepartments_AppLaborCategories_LaborCategoryId",
                table: "LaborCategoryDepartments");

            migrationBuilder.DropTable(
                name: "LaborCategoryRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AppLaborCategories",
                table: "AppLaborCategories");

            migrationBuilder.RenameTable(
                name: "AppLaborCategories",
                newName: "LaborCategories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LaborCategories",
                table: "LaborCategories",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AppLaborCategoryProjectRoles",
                columns: table => new
                {
                    LaborCategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectRoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLaborCategoryProjectRoles", x => new { x.LaborCategoryId, x.ProjectRoleId });
                    table.ForeignKey(
                        name: "FK_AppLaborCategoryProjectRoles_LaborCategories_LaborCategoryId",
                        column: x => x.LaborCategoryId,
                        principalTable: "LaborCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppProjectRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExtraProperties = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DeleterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppProjectRoles", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_LaborCategoryDepartments_LaborCategories_LaborCategoryId",
                table: "LaborCategoryDepartments",
                column: "LaborCategoryId",
                principalTable: "LaborCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LaborCategoryDepartments_LaborCategories_LaborCategoryId",
                table: "LaborCategoryDepartments");

            migrationBuilder.DropTable(
                name: "AppLaborCategoryProjectRoles");

            migrationBuilder.DropTable(
                name: "AppProjectRoles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LaborCategories",
                table: "LaborCategories");

            migrationBuilder.RenameTable(
                name: "LaborCategories",
                newName: "AppLaborCategories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AppLaborCategories",
                table: "AppLaborCategories",
                column: "Id");

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
                name: "IX_LaborCategoryRoles_LaborCategoryId",
                table: "LaborCategoryRoles",
                column: "LaborCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_LaborCategoryDepartments_AppLaborCategories_LaborCategoryId",
                table: "LaborCategoryDepartments",
                column: "LaborCategoryId",
                principalTable: "AppLaborCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
