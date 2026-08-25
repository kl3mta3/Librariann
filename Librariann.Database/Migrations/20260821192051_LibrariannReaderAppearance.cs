using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannReaderAppearance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookReaderBackgroundColor",
                table: "AppUserReadingProfiles",
                type: "TEXT",
                nullable: true,
                defaultValue: "#F1E4D5");

            migrationBuilder.AddColumn<int>(
                name: "BookReaderBackgroundOpacity",
                table: "AppUserReadingProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookReaderBackgroundColor",
                table: "AppUserReadingProfiles");

            migrationBuilder.DropColumn(
                name: "BookReaderBackgroundOpacity",
                table: "AppUserReadingProfiles");
        }
    }
}
