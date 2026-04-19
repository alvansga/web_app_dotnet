using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSandbox.Migrations
{
    /// <inheritdoc />
    public partial class AddGameStateWinner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "State_Winner",
                table: "GameRooms",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "State_Winner",
                table: "GameRooms");
        }
    }
}
