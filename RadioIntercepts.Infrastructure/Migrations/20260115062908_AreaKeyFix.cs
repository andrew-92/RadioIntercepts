using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RadioIntercepts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AreaKeyFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Areas_Name",
                table: "Areas");

            migrationBuilder.AddColumn<string>(
                name: "Key",
                table: "Areas",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Key",
                table: "Areas",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Name",
                table: "Areas",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Areas_Key",
                table: "Areas");

            migrationBuilder.DropIndex(
                name: "IX_Areas_Name",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Key",
                table: "Areas");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Name",
                table: "Areas",
                column: "Name",
                unique: true);
        }
    }
}
