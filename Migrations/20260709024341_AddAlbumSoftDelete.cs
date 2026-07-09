using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoLens.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbumSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Albums",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Albums",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Albums_UpdatedAt",
                table: "Albums",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Albums_UpdatedAt",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Albums");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Albums");
        }
    }
}
