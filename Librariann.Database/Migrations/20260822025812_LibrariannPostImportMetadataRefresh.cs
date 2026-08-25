using Librariann.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260822025812_LibrariannPostImportMetadataRefresh")]
public partial class LibrariannPostImportMetadataRefresh : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MetadataRefreshQueuedAtUtc",
            table: "AcquisitionDownloads",
            type: "TEXT",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "MetadataRefreshQueuedAtUtc",
            table: "AcquisitionDownloads");
    }
}

