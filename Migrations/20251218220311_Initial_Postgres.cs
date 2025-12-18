using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DiscordBotNightOwl.Migrations
{
    /// <inheritdoc />
    public partial class Initial_Postgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GlobalSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HomeServerId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlobalSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildRoleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RoleId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildName = table.Column<string>(type: "text", nullable: true),
                    RoleName = table.Column<string>(type: "text", nullable: true),
                    ConfigType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRoleConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GuildSettings",
                columns: table => new
                {
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildName = table.Column<string>(type: "text", nullable: true),
                    AIChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    LogChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildSettings", x => x.GuildId);
                });

            migrationBuilder.CreateTable(
                name: "VoiceSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildName = table.Column<string>(type: "text", nullable: true),
                    ChannelName = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    JoinTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    LeaveTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    DurationMinutes = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceSessions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GlobalSettings");

            migrationBuilder.DropTable(
                name: "GuildRoleConfigs");

            migrationBuilder.DropTable(
                name: "GuildSettings");

            migrationBuilder.DropTable(
                name: "VoiceSessions");
        }
    }
}
