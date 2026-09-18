using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAttendanceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CheckInAddress",
                table: "AttendanceData",
                newName: "OffdutytimeResults");

            migrationBuilder.AddColumn<string>(
                name: "Offdutytime",
                table: "AttendanceData",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Offdutytime",
                table: "AttendanceData");

            migrationBuilder.RenameColumn(
                name: "OffdutytimeResults",
                table: "AttendanceData",
                newName: "CheckInAddress");
        }
    }
}
