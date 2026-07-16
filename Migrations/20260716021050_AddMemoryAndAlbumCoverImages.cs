using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoLens.Migrations
{
    /// <inheritdoc />
    public partial class AddMemoryAndAlbumCoverImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Memories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CoverImageId",
                table: "Albums",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Memories_CoverImageId",
                table: "Memories",
                column: "CoverImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Albums_CoverImageId",
                table: "Albums",
                column: "CoverImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Albums_MemoryImages_CoverImageId",
                table: "Albums",
                column: "CoverImageId",
                principalTable: "MemoryImages",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Memories_MemoryImages_CoverImageId",
                table: "Memories",
                column: "CoverImageId",
                principalTable: "MemoryImages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Albums_MemoryImages_CoverImageId",
                table: "Albums");

            migrationBuilder.DropForeignKey(
                name: "FK_Memories_MemoryImages_CoverImageId",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Memories_CoverImageId",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Albums_CoverImageId",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "CoverImageId",
                table: "Albums");
        }
    }
}
