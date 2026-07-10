using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoLens.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserRefreshTokens_RevokedAt",
                table: "UserRefreshTokens",
                column: "RevokedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Memories_UserId_IsDeleted_CreatedAt",
                table: "Memories",
                columns: new[] { "UserId", "IsDeleted", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Memories_UserId_IsDeleted_MemoryDate",
                table: "Memories",
                columns: new[] { "UserId", "IsDeleted", "MemoryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Albums_UserId_IsDeleted_CreatedAt",
                table: "Albums",
                columns: new[] { "UserId", "IsDeleted", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserRefreshTokens_RevokedAt",
                table: "UserRefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Memories_UserId_IsDeleted_CreatedAt",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Memories_UserId_IsDeleted_MemoryDate",
                table: "Memories");

            migrationBuilder.DropIndex(
                name: "IX_Albums_UserId_IsDeleted_CreatedAt",
                table: "Albums");
        }
    }
}
