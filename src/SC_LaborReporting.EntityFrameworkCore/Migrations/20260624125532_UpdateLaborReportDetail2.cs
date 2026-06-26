using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLaborReportDetail2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Hoursfinance",
                table: "AppLaborReportDetails",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hoursfinance",
                table: "AppLaborReportDetails");
        }
    }
}
