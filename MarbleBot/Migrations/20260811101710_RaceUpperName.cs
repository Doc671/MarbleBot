using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class RaceUpperName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UpperMarbleName",
                table: "SignupGameResults",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpperMarbleName",
                table: "SignupGameResults");
        }
    }
}
