using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannWantedCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogSummary",
                table: "MonitoringTargets",
                type: "TEXT",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastCatalogSyncAtUtc",
                table: "MonitoringTargets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WantedItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MonitoringTargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExternalItemId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Series = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Sequence = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    PublicationYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    LibrarySeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    FirstSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSeenAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WantedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WantedItems_MonitoringTargets_MonitoringTargetId",
                        column: x => x.MonitoringTargetId,
                        principalTable: "MonitoringTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WantedItems_Series_LibrarySeriesId",
                        column: x => x.LibrarySeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WantedItems_LibrarySeriesId",
                table: "WantedItems",
                column: "LibrarySeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_WantedItems_MonitoringTargetId_ProviderKey_ExternalItemId",
                table: "WantedItems",
                columns: new[] { "MonitoringTargetId", "ProviderKey", "ExternalItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WantedItems");

            migrationBuilder.DropColumn(
                name: "CatalogSummary",
                table: "MonitoringTargets");

            migrationBuilder.DropColumn(
                name: "LastCatalogSyncAtUtc",
                table: "MonitoringTargets");
        }
    }
}
