using Librariann.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations;

[DbContext(typeof(DataContext))]
[Migration("20260821211500_LibrariannMetadataFileWriteSetting")]
public sealed class LibrariannMetadataFileWriteSetting : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT OR IGNORE INTO "ServerSetting" ("Key", "RowVersion", "Value")
            VALUES (44, 0, 'false');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM \"ServerSetting\" WHERE \"Key\" = 44;");
    }
}
