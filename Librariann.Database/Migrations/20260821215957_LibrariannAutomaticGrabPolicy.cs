using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannAutomaticGrabPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutomaticGrabEnabled",
                table: "MonitoringTargets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DownloadClientId",
                table: "MonitoringTargets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAutomaticGrabAtUtc",
                table: "MonitoringTargets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinimumAutomaticGrabScore",
                table: "MonitoringTargets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 90);

            migrationBuilder.AddColumn<string>(
                name: "GrabSummary",
                table: "MonitoringSearchRuns",
                type: "TEXT",
                maxLength: 1024,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "WasGrabbed",
                table: "MonitoringSearchRuns",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MonitoringTargetId",
                table: "AcquisitionDownloads",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WantedItemId",
                table: "AcquisitionDownloads",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonitoringTargets_DownloadClientId",
                table: "MonitoringTargets",
                column: "DownloadClientId");

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionDownloads_MonitoringTargetId_WantedItemId_Status",
                table: "AcquisitionDownloads",
                columns: new[] { "MonitoringTargetId", "WantedItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionDownloads_WantedItemId",
                table: "AcquisitionDownloads",
                column: "WantedItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionDownloads_MonitoringTargets_MonitoringTargetId",
                table: "AcquisitionDownloads",
                column: "MonitoringTargetId",
                principalTable: "MonitoringTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionDownloads_WantedItems_WantedItemId",
                table: "AcquisitionDownloads",
                column: "WantedItemId",
                principalTable: "WantedItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MonitoringTargets_IntegrationProviderConfigurations_DownloadClientId",
                table: "MonitoringTargets",
                column: "DownloadClientId",
                principalTable: "IntegrationProviderConfigurations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcquisitionDownloads_MonitoringTargets_MonitoringTargetId",
                table: "AcquisitionDownloads");

            migrationBuilder.DropForeignKey(
                name: "FK_AcquisitionDownloads_WantedItems_WantedItemId",
                table: "AcquisitionDownloads");

            migrationBuilder.DropForeignKey(
                name: "FK_MonitoringTargets_IntegrationProviderConfigurations_DownloadClientId",
                table: "MonitoringTargets");

            migrationBuilder.DropIndex(
                name: "IX_MonitoringTargets_DownloadClientId",
                table: "MonitoringTargets");

            migrationBuilder.DropIndex(
                name: "IX_AcquisitionDownloads_MonitoringTargetId_WantedItemId_Status",
                table: "AcquisitionDownloads");

            migrationBuilder.DropIndex(
                name: "IX_AcquisitionDownloads_WantedItemId",
                table: "AcquisitionDownloads");

            migrationBuilder.DropColumn(
                name: "AutomaticGrabEnabled",
                table: "MonitoringTargets");

            migrationBuilder.DropColumn(
                name: "DownloadClientId",
                table: "MonitoringTargets");

            migrationBuilder.DropColumn(
                name: "LastAutomaticGrabAtUtc",
                table: "MonitoringTargets");

            migrationBuilder.DropColumn(
                name: "MinimumAutomaticGrabScore",
                table: "MonitoringTargets");

            migrationBuilder.DropColumn(
                name: "GrabSummary",
                table: "MonitoringSearchRuns");

            migrationBuilder.DropColumn(
                name: "WasGrabbed",
                table: "MonitoringSearchRuns");

            migrationBuilder.DropColumn(
                name: "MonitoringTargetId",
                table: "AcquisitionDownloads");

            migrationBuilder.DropColumn(
                name: "WantedItemId",
                table: "AcquisitionDownloads");
        }
    }
}
