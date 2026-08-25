using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannPersonOpenLibraryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OpenLibraryId",
                table: "Person",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Person_OpenLibraryId",
                table: "Person",
                column: "OpenLibraryId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Person_OpenLibraryId",
                table: "Person");

            migrationBuilder.DropColumn(
                name: "OpenLibraryId",
                table: "Person");

        }
    }
}
