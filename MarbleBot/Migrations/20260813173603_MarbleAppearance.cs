using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class MarbleAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarbleAppearances",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    AppearanceType = table.Column<int>(type: "integer", nullable: false),
                    GradientAngleDegrees = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarbleAppearances", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_MarbleAppearances_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarbleAppearanceColors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RawColor = table.Column<long>(type: "bigint", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarbleAppearanceColors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarbleAppearanceColors_MarbleAppearances_UserId",
                        column: x => x.UserId,
                        principalTable: "MarbleAppearances",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarbleAppearanceColors_UserId_Order",
                table: "MarbleAppearanceColors",
                columns: new[] { "UserId", "Order" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarbleAppearanceColors");

            migrationBuilder.DropTable(
                name: "MarbleAppearances");
        }
    }
}
