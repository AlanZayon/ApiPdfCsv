using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPdfCsv.Migrations
{
    /// <inheritdoc />
    public partial class AddTermoEspecialIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TermoEspecial_Lookup",
                table: "TermoEspecial",
                columns: new[] { "userId", "CNPJ", "codigoBanco", "termo", "tipovalor" });

            migrationBuilder.CreateIndex(
                name: "IX_TermoEspecial_User_Cnpj_Banco",
                table: "TermoEspecial",
                columns: new[] { "userId", "CNPJ", "codigoBanco" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TermoEspecial_Lookup",
                table: "TermoEspecial");

            migrationBuilder.DropIndex(
                name: "IX_TermoEspecial_User_Cnpj_Banco",
                table: "TermoEspecial");
        }
    }
}
