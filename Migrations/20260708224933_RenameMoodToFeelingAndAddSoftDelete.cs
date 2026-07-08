using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemoLens.Migrations
{
    /// <inheritdoc />
    public partial class RenameMoodToFeelingAndAddSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Mood",
                table: "Memories",
                newName: "Feeling");

            migrationBuilder.RenameIndex(
                name: "IX_Memories_Mood",
                table: "Memories",
                newName: "IX_Memories_Feeling");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Memories",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Story",
                table: "Memories",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Memories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Memories",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Memories",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Memories");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Memories");

            migrationBuilder.RenameColumn(
                name: "Feeling",
                table: "Memories",
                newName: "Mood");

            migrationBuilder.RenameIndex(
                name: "IX_Memories_Feeling",
                table: "Memories",
                newName: "IX_Memories_Mood");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Memories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.AlterColumn<string>(
                name: "Story",
                table: "Memories",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Location",
                table: "Memories",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);
        }
    }
}
