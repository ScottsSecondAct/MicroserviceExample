using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportingService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityRepProjections",
                columns: table => new
                {
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalCount = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityRepProjections", x => x.OwnerId);
                });

            migrationBuilder.CreateTable(
                name: "ContactFunnelProjections",
                columns: table => new
                {
                    Status = table.Column<string>(type: "text", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactFunnelProjections", x => x.Status);
                });

            migrationBuilder.CreateTable(
                name: "DealSnapshots",
                columns: table => new
                {
                    DealId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealSnapshots", x => x.DealId);
                });

            migrationBuilder.CreateTable(
                name: "PipelineProjections",
                columns: table => new
                {
                    Stage = table.Column<string>(type: "text", nullable: false),
                    DealCount = table.Column<int>(type: "integer", nullable: false),
                    TotalValue = table.Column<decimal>(type: "numeric", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PipelineProjections", x => x.Stage);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityRepProjections");

            migrationBuilder.DropTable(
                name: "ContactFunnelProjections");

            migrationBuilder.DropTable(
                name: "DealSnapshots");

            migrationBuilder.DropTable(
                name: "PipelineProjections");
        }
    }
}
