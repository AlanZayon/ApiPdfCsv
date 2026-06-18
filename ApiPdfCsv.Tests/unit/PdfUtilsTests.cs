using ApiPdfCsv.Shared.Utils;
using Xunit;

namespace ApiPdfCsv.Tests.Unit;

public class PdfUtilsTests
{
    [Fact]
    public void ExtrairHistorico_SimplesNacional_ReturnsExpectedHistorico()
    {
        var linha = "1082 SIMPLES NACIONAL 01/2025 100,00 0,00 0,00 100,00";
        var historico = PdfUtils.ExtrairHistorico(linha);
        Assert.Equal("PG. SIMPLES NACIONAL XX", historico);
    }

    [Fact]
    public void ExtrairHistorico_IrpjComSimplesNacional_PriorizaSimplesNacional()
    {
        var linha = "1082 IRPJ - SIMPLES NACIONAL 01/2025 100,00 0,00 0,00 100,00";
        var historico = PdfUtils.ExtrairHistorico(linha);
        Assert.Equal("PG. SIMPLES NACIONAL XX", historico);
    }

    [Fact]
    public void ParseLinhaHistorico_ValidLine_ReturnsPrincipal()
    {
        var linha = "1082 SIMPLES NACIONAL 01/2025 100,00 0,00 0,00 100,00";
        var values = PdfUtils.ParseLinhaHistorico(linha);

        Assert.NotNull(values);
        Assert.Equal(100m, values!.Principal);
        Assert.Equal(100m, values.Total);
    }

    [Fact]
    public void ParseTotaisLinha_ValidLine_ReturnsSomaMultaJuros()
    {
        var linha = "100,00 10,00 5,00 115,00";
        var values = PdfUtils.ParseTotaisLinha(linha);

        Assert.NotNull(values);
        Assert.Equal(15m, values!.SomaMultaJuros);
    }
}
