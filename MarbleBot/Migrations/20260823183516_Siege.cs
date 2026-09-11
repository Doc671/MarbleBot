using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class Siege : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bosses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Health = table.Column<int>(type: "integer", nullable: false),
                    MaxHealth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bosses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Attacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BossId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    Accuracy = table.Column<int>(type: "integer", nullable: false),
                    StatusEffect = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Attacks_Bosses_BossId",
                        column: x => x.BossId,
                        principalTable: "Bosses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BossItemDrops",
                columns: table => new
                {
                    BossId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    MinCount = table.Column<int>(type: "integer", nullable: false),
                    MaxCount = table.Column<int>(type: "integer", nullable: false),
                    Chance = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BossItemDrops", x => new { x.BossId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_BossItemDrops_Bosses_BossId",
                        column: x => x.BossId,
                        principalTable: "Bosses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BossItemDrops_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attacks_BossId",
                table: "Attacks",
                column: "BossId");

            migrationBuilder.CreateIndex(
                name: "IX_Attacks_Name",
                table: "Attacks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Bosses_Name",
                table: "Bosses",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BossItemDrops_ItemId",
                table: "BossItemDrops",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attacks");

            migrationBuilder.DropTable(
                name: "BossItemDrops");

            migrationBuilder.DropTable(
                name: "Bosses");
        }
    }
}
