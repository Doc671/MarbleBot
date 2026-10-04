using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class MarbleAppearanceAngleOffsetRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "GradientAngleDegrees",
                table: "MarbleAppearances",
                newName: "AngleOffset");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AngleOffset",
                table: "MarbleAppearances",
                newName: "GradientAngleDegrees");
        }
    }
}
