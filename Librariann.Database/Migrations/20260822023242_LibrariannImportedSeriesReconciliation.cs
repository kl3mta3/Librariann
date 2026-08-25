using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannImportedSeriesReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ImportedAtUtc",
                table: "AcquisitionDownloads",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImportedSeriesId",
                table: "AcquisitionDownloads",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionDownloads_ImportedSeriesId_Status",
                table: "AcquisitionDownloads",
                columns: new[] { "ImportedSeriesId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_AcquisitionDownloads_Series_ImportedSeriesId",
                table: "AcquisitionDownloads",
                column: "ImportedSeriesId",
                principalTable: "Series",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AcquisitionDownloads_Series_ImportedSeriesId",
                table: "AcquisitionDownloads");

            migrationBuilder.DropIndex(
                name: "IX_AcquisitionDownloads_ImportedSeriesId_Status",
                table: "AcquisitionDownloads");

            migrationBuilder.DropColumn(
                name: "ImportedAtUtc",
                table: "AcquisitionDownloads");

            migrationBuilder.DropColumn(
                name: "ImportedSeriesId",
                table: "AcquisitionDownloads");
        }
    }
}
