using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannAudiobookSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Bitrate",
                table: "MangaFile",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChapterMarkersJson",
                table: "MangaFile",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DurationSeconds",
                table: "MangaFile",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PlaybackPositionSeconds",
                table: "AppUserProgresses",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bitrate",
                table: "MangaFile");

            migrationBuilder.DropColumn(
                name: "ChapterMarkersJson",
                table: "MangaFile");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "MangaFile");

            migrationBuilder.DropColumn(
                name: "PlaybackPositionSeconds",
                table: "AppUserProgresses");
        }
    }
}
