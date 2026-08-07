using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ApiPdfCsv.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilPlanoContasReplaceImpostoClienteId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Imposto_Clientes_clienteid",
                table: "Imposto");

            migrationBuilder.CreateTable(
                name: "PerfisPlanoContas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    userid = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    nome = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    isdefault = table.Column<bool>(type: "boolean", nullable: false),
                    codigogenericoentrada = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    codigogenericosaida = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    createdatutc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfisPlanoContas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfisPlanoContas_User_Nome",
                table: "PerfisPlanoContas",
                columns: new[] { "userid", "nome" });

            migrationBuilder.AddColumn<int>(
                name: "perfilid",
                table: "Imposto",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO "PerfisPlanoContas" (userid, nome, isdefault, createdatutc)
                SELECT DISTINCT i.userid, 'Padrão', TRUE, NOW() AT TIME ZONE 'UTC'
                FROM "Imposto" i
                WHERE i.userid IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM "PerfisPlanoContas" p WHERE p.userid = i.userid
                  );

                UPDATE "Imposto" i
                SET perfilid = p.id
                FROM "PerfisPlanoContas" p
                WHERE i.userid = p.userid
                  AND p.isdefault = TRUE
                  AND i.clienteid IS NULL
                  AND i.userid IS NOT NULL;

                INSERT INTO "PerfisPlanoContas" (userid, nome, isdefault, createdatutc)
                SELECT DISTINCT c.userid, LEFT(COALESCE(NULLIF(c.razaosocial, ''), 'Cliente ' || c.id::text), 128), FALSE, NOW() AT TIME ZONE 'UTC'
                FROM "Imposto" i
                INNER JOIN "Clientes" c ON c.id = i.clienteid
                WHERE i.clienteid IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM "PerfisPlanoContas" p
                      WHERE p.userid = c.userid AND p.nome = LEFT(COALESCE(NULLIF(c.razaosocial, ''), 'Cliente ' || c.id::text), 128)
                  );

                UPDATE "Imposto" i
                SET perfilid = p.id
                FROM "Clientes" c
                INNER JOIN "PerfisPlanoContas" p
                    ON p.userid = c.userid
                   AND p.nome = LEFT(COALESCE(NULLIF(c.razaosocial, ''), 'Cliente ' || c.id::text), 128)
                WHERE i.clienteid = c.id
                  AND i.clienteid IS NOT NULL;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Imposto_clienteid",
                table: "Imposto");

            migrationBuilder.DropColumn(
                name: "clienteid",
                table: "Imposto");

            migrationBuilder.CreateIndex(
                name: "IX_Imposto_perfilid",
                table: "Imposto",
                column: "perfilid");

            migrationBuilder.AddForeignKey(
                name: "FK_Imposto_PerfisPlanoContas_perfilid",
                table: "Imposto",
                column: "perfilid",
                principalTable: "PerfisPlanoContas",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Imposto_PerfisPlanoContas_perfilid",
                table: "Imposto");

            migrationBuilder.DropIndex(
                name: "IX_Imposto_perfilid",
                table: "Imposto");

            migrationBuilder.AddColumn<int>(
                name: "clienteid",
                table: "Imposto",
                type: "integer",
                nullable: true);

            migrationBuilder.DropColumn(
                name: "perfilid",
                table: "Imposto");

            migrationBuilder.DropTable(
                name: "PerfisPlanoContas");

            migrationBuilder.CreateIndex(
                name: "IX_Imposto_clienteid",
                table: "Imposto",
                column: "clienteid");

            migrationBuilder.AddForeignKey(
                name: "FK_Imposto_Clientes_clienteid",
                table: "Imposto",
                column: "clienteid",
                principalTable: "Clientes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
