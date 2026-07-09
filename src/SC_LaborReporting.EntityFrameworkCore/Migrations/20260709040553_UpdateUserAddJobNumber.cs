using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAddJobNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "JobNumber",
                table: "AbpUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_JobNumber",
                table: "AbpUsers",
                column: "JobNumber",
                unique: true,
                filter: "[JobNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_JobNumber",
                table: "AbpUsers");

            migrationBuilder.DropColumn(
                name: "JobNumber",
                table: "AbpUsers");
        }
    }
}
