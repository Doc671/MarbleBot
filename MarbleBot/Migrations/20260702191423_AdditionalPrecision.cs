using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarbleBot.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NetWorth",
                table: "Users",
                type: "numeric(20,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Users",
                type: "numeric(20,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "Items",
                type: "numeric(20,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "NetWorth",
                table: "Users",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Balance",
                table: "Users",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Value",
                table: "Items",
                type: "numeric(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,2)");
        }
    }
}
