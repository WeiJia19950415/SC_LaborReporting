using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddNewJobNumber2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NewJobNumber",
                table: "AbpUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewJobNumber",
                table: "AbpUsers");
        }
    }
}
