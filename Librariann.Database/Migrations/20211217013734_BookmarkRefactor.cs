using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    public partial class BookmarkRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "AppUserBookmark",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "AppUserBookmark");
        }
    }
}
