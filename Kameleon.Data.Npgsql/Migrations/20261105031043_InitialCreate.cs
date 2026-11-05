using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kameleon.Data.Npgsql.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KameleonCatalogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ThemeId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Availability = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KameleonCatalogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KameleonTenantSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    DefaultThemeId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: true),
                    DefaultLayoutId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: true),
                    AllowUserOverride = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KameleonTenantSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KameleonTenantThemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    ThemeId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    IsGranted = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Configuration = table.Column<string>(type: "text", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KameleonTenantThemes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KameleonUserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TenantId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ThemeId = table.Column<string>(type: "character varying(64)", unicode: false, maxLength: 64, nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KameleonUserPreferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KameleonCatalogEntries_ThemeId",
                table: "KameleonCatalogEntries",
                column: "ThemeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KameleonTenantSettings_TenantId",
                table: "KameleonTenantSettings",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KameleonTenantThemes_TenantId_ThemeId",
                table: "KameleonTenantThemes",
                columns: new[] { "TenantId", "ThemeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KameleonTenantThemes_ThemeId",
                table: "KameleonTenantThemes",
                column: "ThemeId");

            migrationBuilder.CreateIndex(
                name: "IX_KameleonUserPreferences_TenantId_UserId",
                table: "KameleonUserPreferences",
                columns: new[] { "TenantId", "UserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KameleonCatalogEntries");

            migrationBuilder.DropTable(
                name: "KameleonTenantSettings");

            migrationBuilder.DropTable(
                name: "KameleonTenantThemes");

            migrationBuilder.DropTable(
                name: "KameleonUserPreferences");
        }
    }
}
