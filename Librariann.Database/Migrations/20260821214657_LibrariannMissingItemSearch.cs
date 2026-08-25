using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannMissingItemSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSearchAtUtc",
                table: "WantedItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSearchSummary",
                table: "WantedItems",
                type: "TEXT",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "NextSearchAtUtc",
                table: "WantedItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "WantedItemId",
                table: "MonitoringSearchRuns",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WantedItems_MonitoringTargetId_Status_NextSearchAtUtc",
                table: "WantedItems",
                columns: new[] { "MonitoringTargetId", "Status", "NextSearchAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringSearchRuns_WantedItemId",
                table: "MonitoringSearchRuns",
                column: "WantedItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonitoringSearchRuns_WantedItems_WantedItemId",
                table: "MonitoringSearchRuns",
                column: "WantedItemId",
                principalTable: "WantedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonitoringSearchRuns_WantedItems_WantedItemId",
                table: "MonitoringSearchRuns");

            migrationBuilder.DropIndex(
                name: "IX_WantedItems_MonitoringTargetId_Status_NextSearchAtUtc",
                table: "WantedItems");

            migrationBuilder.DropIndex(
                name: "IX_MonitoringSearchRuns_WantedItemId",
                table: "MonitoringSearchRuns");

            migrationBuilder.DropColumn(
                name: "LastSearchAtUtc",
                table: "WantedItems");

            migrationBuilder.DropColumn(
                name: "LastSearchSummary",
                table: "WantedItems");

            migrationBuilder.DropColumn(
                name: "NextSearchAtUtc",
                table: "WantedItems");

            migrationBuilder.DropColumn(
                name: "WantedItemId",
                table: "MonitoringSearchRuns");
        }
    }
}
