using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannMetadataProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetadataFieldProvenances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityType = table.Column<int>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Field = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ProviderItemId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ValueHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    IsUserOverride = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetadataFieldProvenances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MetadataFieldProvenances_EntityType_EntityId_Field",
                table: "MetadataFieldProvenances",
                columns: new[] { "EntityType", "EntityId", "Field" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetadataFieldProvenances");
        }
    }
}
