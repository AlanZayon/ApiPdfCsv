using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApiPdfCsv.Migrations
{
    /// <inheritdoc />
    public partial class ModelosPadrões : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodigoConta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Codigo = table.Column<string>(type: "text", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodigoConta", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Imposto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    CodigoDebitoId = table.Column<int>(type: "integer", nullable: true),
                    CodigoCreditoId = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Imposto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Imposto_CodigoConta_CodigoCreditoId",
                        column: x => x.CodigoCreditoId,
                        principalTable: "CodigoConta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Imposto_CodigoConta_CodigoDebitoId",
                        column: x => x.CodigoDebitoId,
                        principalTable: "CodigoConta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Imposto_CodigoCreditoId",
                table: "Imposto",
                column: "CodigoCreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_Imposto_CodigoDebitoId",
                table: "Imposto",
                column: "CodigoDebitoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Imposto");

            migrationBuilder.DropTable(
                name: "CodigoConta");
        }
    }
}
