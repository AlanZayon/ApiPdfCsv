using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPdfCsv.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    JobKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    FileType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    InputFileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    OutputFile = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ResultJson = table.Column<string>(type: "jsonb", nullable: true),
                    MetadataJson = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadJobs_User_Job",
                table: "UploadJobs",
                columns: new[] { "UserId", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadJobs");
        }
    }
}
