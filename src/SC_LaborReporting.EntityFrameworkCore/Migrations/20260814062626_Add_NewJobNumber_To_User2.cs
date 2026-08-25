using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SC_LaborReporting.Migrations
{
    /// <inheritdoc />
    public partial class Add_NewJobNumber_To_User2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "NewJobNumber",
                table: "AbpUsers",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbpUsers_NewJobNumber",
                table: "AbpUsers",
                column: "NewJobNumber",
                unique: true,
                filter: "[NewJobNumber] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AbpUsers_NewJobNumber",
                table: "AbpUsers");

            migrationBuilder.AlterColumn<string>(
                name: "NewJobNumber",
                table: "AbpUsers",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(32)",
                oldMaxLength: 32,
                oldNullable: true);
        }
    }
}
