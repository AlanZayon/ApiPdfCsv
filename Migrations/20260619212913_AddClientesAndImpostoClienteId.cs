using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApiPdfCsv.Migrations
{
    /// <inheritdoc />
    public partial class AddClientesAndImpostoClienteId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "clienteid",
                table: "Imposto",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    razaosocial = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    codigobancopadrao = table.Column<int>(type: "integer", nullable: true),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    createdatutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Imposto_clienteid",
                table: "Imposto",
                column: "clienteid");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_User_Cnpj",
                table: "Clientes",
                columns: new[] { "userid", "cnpj" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Imposto_Clientes_clienteid",
                table: "Imposto",
                column: "clienteid",
                principalTable: "Clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Imposto_Clientes_clienteid",
                table: "Imposto");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Imposto_clienteid",
                table: "Imposto");

            migrationBuilder.DropColumn(
                name: "clienteid",
                table: "Imposto");
        }
    }
}
