using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class Guild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Color = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildUsableChannels",
                columns: table => new
                {
                    GuildId = table.Column<long>(type: "bigint", nullable: false),
                    ChannelId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildUsableChannels", x => new { x.GuildId, x.ChannelId });
                    table.ForeignKey(
                        name: "FK_GuildUsableChannels_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GuildUsableChannels");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
