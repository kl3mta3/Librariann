using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librariann.Database.Migrations
{
    /// <inheritdoc />
    public partial class LibrariannProvidersAndAcquisitionQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcquisitionDownloads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RequestedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    IntegrationProviderConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    DownloadClientName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    ReleaseTitle = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false),
                    Format = table.Column<int>(type: "INTEGER", nullable: false),
                    Protocol = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    Progress = table.Column<double>(type: "REAL", nullable: false),
                    OutputPath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    ImportedPath = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    ConsecutivePollFailures = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastPolledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcquisitionDownloads", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CredentialKey = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    AllowPrivateNetwork = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownloadCategory = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "librariann"),
                    RemotePath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: true, defaultValue: "[]"),
                    ProtectedUsername = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    ProtectedPassword = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: false),
                    ProtectedApiKey = table.Column<string>(type: "TEXT", maxLength: 8192, nullable: false),
                    DownloadClientKind = table.Column<int>(type: "INTEGER", nullable: true),
                    IndexerProtocol = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionDownloads_IntegrationProviderConfigurationId_ExternalId",
                table: "AcquisitionDownloads",
                columns: new[] { "IntegrationProviderConfigurationId", "ExternalId" });

            migrationBuilder.CreateIndex(
                name: "IX_AcquisitionDownloads_Status",
                table: "AcquisitionDownloads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationProviderConfigurations_CredentialKey",
                table: "IntegrationProviderConfigurations",
                column: "CredentialKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationProviderConfigurations_Name",
                table: "IntegrationProviderConfigurations",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcquisitionDownloads");

            migrationBuilder.DropTable(
                name: "IntegrationProviderConfigurations");
        }
    }
}
