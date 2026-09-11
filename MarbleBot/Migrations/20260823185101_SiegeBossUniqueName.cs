using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class SiegeBossUniqueName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bosses_Name",
                table: "Bosses");

            migrationBuilder.CreateIndex(
                name: "IX_Bosses_Name",
                table: "Bosses",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bosses_Name",
                table: "Bosses");

            migrationBuilder.CreateIndex(
                name: "IX_Bosses_Name",
                table: "Bosses",
                column: "Name");
        }
    }
}
