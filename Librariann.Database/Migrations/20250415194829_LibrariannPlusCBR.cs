using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannPlusCBR : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableChapterCoverImage",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableChapterPublisher",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableChapterReleaseDate",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableChapterSummary",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableChapterTitle",
                table: "MetadataSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CbrId",
                table: "ExternalSeriesMetadata",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "LibrariannPlusConnection",
                table: "ChapterPeople",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrderWeight",
                table: "ChapterPeople",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableChapterCoverImage",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "EnableChapterPublisher",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "EnableChapterReleaseDate",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "EnableChapterSummary",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "EnableChapterTitle",
                table: "MetadataSettings");

            migrationBuilder.DropColumn(
                name: "CbrId",
                table: "ExternalSeriesMetadata");

            migrationBuilder.DropColumn(
                name: "LibrariannPlusConnection",
                table: "ChapterPeople");

            migrationBuilder.DropColumn(
                name: "OrderWeight",
                table: "ChapterPeople");
        }
    }
}
