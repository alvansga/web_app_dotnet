using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSandbox.Migrations
{
    /// <inheritdoc />
    public partial class AddClueSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Word = table.Column<string>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false),
                    GameRoomId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clues_GameRooms_GameRoomId",
                        column: x => x.GameRoomId,
                        principalTable: "GameRooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clues_GameRoomId",
                table: "Clues",
                column: "GameRoomId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clues");
        }
    }
}
