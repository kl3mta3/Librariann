using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class ScrobbleErrorRework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChapterId",
                table: "ScrobbleError",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScrobbleErrorId",
                table: "LibrariannPlusAuditLogs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScrobbleError_ChapterId",
                table: "ScrobbleError",
                column: "ChapterId");

            migrationBuilder.CreateIndex(
                name: "IX_LibrariannPlusAuditLogs_ScrobbleErrorId",
                table: "LibrariannPlusAuditLogs",
                column: "ScrobbleErrorId");

            migrationBuilder.AddForeignKey(
                name: "FK_LibrariannPlusAuditLogs_ScrobbleError_ScrobbleErrorId",
                table: "LibrariannPlusAuditLogs",
                column: "ScrobbleErrorId",
                principalTable: "ScrobbleError",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrobbleError_Chapter_ChapterId",
                table: "ScrobbleError",
                column: "ChapterId",
                principalTable: "Chapter",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibrariannPlusAuditLogs_ScrobbleError_ScrobbleErrorId",
                table: "LibrariannPlusAuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ScrobbleError_Chapter_ChapterId",
                table: "ScrobbleError");

            migrationBuilder.DropIndex(
                name: "IX_ScrobbleError_ChapterId",
                table: "ScrobbleError");

            migrationBuilder.DropIndex(
                name: "IX_LibrariannPlusAuditLogs_ScrobbleErrorId",
                table: "LibrariannPlusAuditLogs");

            migrationBuilder.DropColumn(
                name: "ChapterId",
                table: "ScrobbleError");

            migrationBuilder.DropColumn(
                name: "ScrobbleErrorId",
                table: "LibrariannPlusAuditLogs");
        }
    }
}
