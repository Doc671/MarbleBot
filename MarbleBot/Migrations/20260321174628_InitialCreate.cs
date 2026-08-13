using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "text", nullable: true),
                    GlobalName = table.Column<string>(type: "text", nullable: true),
                    Discriminator = table.Column<string>(type: "text", nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    NetWorth = table.Column<decimal>(type: "numeric", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    DailyStreak = table.Column<int>(type: "integer", nullable: false),
                    SiegePing = table.Column<bool>(type: "boolean", nullable: false),
                    WarPing = table.Column<bool>(type: "boolean", nullable: false),
                    RaceWins = table.Column<int>(type: "integer", nullable: false),
                    SiegeWins = table.Column<int>(type: "integer", nullable: false),
                    WarWins = table.Column<int>(type: "integer", nullable: false),
                    WarEmoji = table.Column<string>(type: "text", nullable: true),
                    LastDaily = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastRaceWin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastScavenge = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSiegeWin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastWarWin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserInventoryItems",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInventoryItems", x => new { x.UserId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_UserInventoryItems_Items_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserInventoryItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserInventoryItems_ItemId",
                table: "UserInventoryItems",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserInventoryItems");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
