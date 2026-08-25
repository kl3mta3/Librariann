using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannPlusAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "ExternalSeriesMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedUtc",
                table: "ExternalSeriesMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModified",
                table: "ExternalSeriesMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastModifiedUtc",
                table: "ExternalSeriesMetadata",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "LibrariannPlusAuditLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    EventType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SeriesId = table.Column<int>(type: "INTEGER", nullable: true),
                    SubjectType = table.Column<int>(type: "INTEGER", nullable: false),
                    SubjectId = table.Column<int>(type: "INTEGER", nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true),
                    HasRetried = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibrariannPlusAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibrariannPlusAuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibrariannPlusAuditLog_Category_CreatedUtc",
                table: "LibrariannPlusAuditLogs",
                columns: new[] { "Category", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LibrariannPlusAuditLog_CreatedUtc",
                table: "LibrariannPlusAuditLogs",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_LibrariannPlusAuditLog_SeriesId_CreatedUtc",
                table: "LibrariannPlusAuditLogs",
                columns: new[] { "SeriesId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LibrariannPlusAuditLog_SubjectType_SubjectId",
                table: "LibrariannPlusAuditLogs",
                columns: new[] { "SubjectType", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_LibrariannPlusAuditLog_UserId",
                table: "LibrariannPlusAuditLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibrariannPlusAuditLogs");

            migrationBuilder.DropColumn(
                name: "Created",
                table: "ExternalSeriesMetadata");

            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "ExternalSeriesMetadata");

            migrationBuilder.DropColumn(
                name: "LastModified",
                table: "ExternalSeriesMetadata");

            migrationBuilder.DropColumn(
                name: "LastModifiedUtc",
                table: "ExternalSeriesMetadata");
        }
    }
}
