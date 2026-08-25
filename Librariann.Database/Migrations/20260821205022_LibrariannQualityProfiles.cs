using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannQualityProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QualityProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MediaType = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UpgradeAllowed = table.Column<bool>(type: "INTEGER", nullable: false),
                    PreferRetail = table.Column<bool>(type: "INTEGER", nullable: false),
                    CutoffFormat = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    MaximumSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    FormatScores = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualityProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QualityProfiles_Name",
                table: "QualityProfiles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QualityProfiles");
        }
    }
}
