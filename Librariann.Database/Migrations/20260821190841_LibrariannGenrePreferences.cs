using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannGenrePreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FavoriteGenreIds",
                table: "AppUserPreferences",
                type: "TEXT",
                nullable: true,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "IgnoredGenreIds",
                table: "AppUserPreferences",
                type: "TEXT",
                nullable: true,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteGenreIds",
                table: "AppUserPreferences");

            migrationBuilder.DropColumn(
                name: "IgnoredGenreIds",
                table: "AppUserPreferences");
        }
    }
}
