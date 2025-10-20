using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiPdfCsv.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoValorToTermoEspecial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TipoValor",
                table: "TermoEspecial",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TipoValor",
                table: "TermoEspecial");
        }
    }
}
