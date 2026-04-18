using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebAppSandbox.Migrations
{
    /// <inheritdoc />
    public partial class InitAll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GameRoomId",
                table: "Players",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GameRooms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRooms", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameRoomId",
                table: "Players",
                column: "GameRoomId");

            migrationBuilder.AddForeignKey(
                name: "FK_Players_GameRooms_GameRoomId",
                table: "Players",
                column: "GameRoomId",
                principalTable: "GameRooms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Players_GameRooms_GameRoomId",
                table: "Players");

            migrationBuilder.DropTable(
                name: "GameRooms");

            migrationBuilder.DropIndex(
                name: "IX_Players_GameRoomId",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "GameRoomId",
                table: "Players");
        }
    }
}
