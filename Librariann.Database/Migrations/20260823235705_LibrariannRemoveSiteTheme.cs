using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannRemoveSiteTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQLite has no ALTER TABLE DROP CONSTRAINT, so DropForeignKey alone emits no real DDL - dropping
            // AppUserPreferences.ThemeId only actually removes the FK once EF's SQLite provider rebuilds that
            // table (CREATE ef_temp_AppUserPreferences -> copy rows -> drop old -> rename). That rebuild gets
            // batched to run *after* this migration's other operations regardless of the order they're declared
            // in here, so a plain DropTable("SiteTheme") still sees a live FK pointing at it and SQLite rejects
            // it with "FOREIGN KEY constraint failed" - confirmed by actually running this migration against a
            // real copy of the database, not just an empty test DB. Toggling the pragma around the drop makes
            // it independent of exactly when EF gets around to rebuilding the referencing table.
            migrationBuilder.DropForeignKey(
                name: "FK_AppUserPreferences_SiteTheme_ThemeId",
                table: "AppUserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_AppUserPreferences_ThemeId",
                table: "AppUserPreferences");

            migrationBuilder.DropColumn(
                name: "ThemeId",
                table: "AppUserPreferences");

            migrationBuilder.AddColumn<int>(
                name: "ThemeMode",
                table: "AppUserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // SQLite only honors a PRAGMA foreign_keys change *outside* an active transaction - EF wraps the
            // whole migration in one by default, so without suppressTransaction: true here this pragma toggle
            // would silently no-op and the DROP TABLE below would still see FK enforcement active. Confirmed by
            // this exact failure still reproducing once with the plain (transaction-wrapped) Sql() calls.
            migrationBuilder.Sql("PRAGMA foreign_keys=OFF;", suppressTransaction: true);
            migrationBuilder.DropTable(
                name: "SiteTheme");
            migrationBuilder.Sql("PRAGMA foreign_keys=ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemeMode",
                table: "AppUserPreferences");

            migrationBuilder.AddColumn<int>(
                name: "ThemeId",
                table: "AppUserPreferences",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SiteTheme",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Author = table.Column<string>(type: "TEXT", nullable: true),
                    CompatibleVersion = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    GitHubPath = table.Column<string>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastModified = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    NormalizedName = table.Column<string>(type: "TEXT", nullable: true),
                    PreviewUrls = table.Column<string>(type: "TEXT", nullable: true),
                    Provider = table.Column<int>(type: "INTEGER", nullable: false),
                    ShaHash = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteTheme", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserPreferences_ThemeId",
                table: "AppUserPreferences",
                column: "ThemeId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUserPreferences_SiteTheme_ThemeId",
                table: "AppUserPreferences",
                column: "ThemeId",
                principalTable: "SiteTheme",
                principalColumn: "Id");
        }
    }
}
