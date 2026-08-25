using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonitoringTargets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    MediaType = table.Column<int>(type: "INTEGER", nullable: false),
                    LibrarySeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    QualityProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Author = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Isbn = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ExternalProviderKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExternalItemId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    MonitorMissing = table.Column<bool>(type: "INTEGER", nullable: false),
                    MonitorFuture = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    SearchIntervalHours = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSearchAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    NextSearchAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastSearchSummary = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitoringTargets_QualityProfiles_QualityProfileId",
                        column: x => x.QualityProfileId,
                        principalTable: "QualityProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonitoringTargets_Series_LibrarySeriesId",
                        column: x => x.LibrarySeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "MonitoringSearchRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MonitoringTargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Query = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ResultCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovedCount = table.Column<int>(type: "INTEGER", nullable: false),
                    BestReleaseTitle = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    BestReleaseScore = table.Column<int>(type: "INTEGER", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    DecisionSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonitoringSearchRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonitoringSearchRuns_MonitoringTargets_MonitoringTargetId",
                        column: x => x.MonitoringTargetId,
                        principalTable: "MonitoringTargets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringSearchRuns_MonitoringTargetId_StartedAtUtc",
                table: "MonitoringSearchRuns",
                columns: new[] { "MonitoringTargetId", "StartedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringTargets_ExternalProviderKey_ExternalItemId_Kind",
                table: "MonitoringTargets",
                columns: new[] { "ExternalProviderKey", "ExternalItemId", "Kind" },
                unique: true,
                filter: "\"ExternalProviderKey\" <> '' AND \"ExternalItemId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringTargets_LibrarySeriesId_Kind",
                table: "MonitoringTargets",
                columns: new[] { "LibrarySeriesId", "Kind" },
                unique: true,
                filter: "\"LibrarySeriesId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringTargets_QualityProfileId",
                table: "MonitoringTargets",
                column: "QualityProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonitoringSearchRuns");

            migrationBuilder.DropTable(
                name: "MonitoringTargets");
        }
    }
}
