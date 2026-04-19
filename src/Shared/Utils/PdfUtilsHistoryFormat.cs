using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace ApiPdfCsv.Shared.Utils;

public static class PdfUtils
{
    public static string ExtrairHistorico(string linha)
    {
        var linhaMaiuscula = linha.ToUpper();

        var termosComuns = new List<string>
        {
            "SIMPLES NACIONAL",
            "MULTA E JUROS",
            "MULTA"
        };

        var tributosComParcelamento = new List<string>
        {
            "PIS",
            "COFINS",
            "IRPJ",
            "CSLL",
            "ISS",
            // "IRRF"
        };

        var termosEspeciais = new Dictionary<string, string>
        {
            { "SIMP NAC", "SIMPLES NACIONAL" },
            { "CONTR PREV DESCONTA SEGURADO", "DCTFWEB" },
            { "CP", "DCTFWEB" },
            { "CONTRIB PREVID PATRONAL", "DCTFWEB" },
            { "CONTRIBUIÇÃO PREVID SEGURADOS", "DCTFWEB" },
            { "CONTR PREVIDENCIÁRIA EMPREGADOR/EMPRESA", "DCTFWEB" },
            { "CONTRIB PREV RISCO AMBIENTAL/APOSENT ESPECIAL", "DCTFWEB" },
            { "CONTRIBUIÇÃO TERCEIROS", "DCTFWEB" },
            { "CIDE", "DCTFWEB" },
            { "CONTRIBUIÇÃO EMPRESA/EMPREGADOR", "DCTFWEB" },
            { "CONTRIB TERC", "DCTFWEB" },
            { "CONTRIB RISCO AMB/APOSENT ESPECIAL", "DCTFWEB" },
            { "RET DE CONTRIBUICOES PAGT PJ A PJ DE DIR PRIV", "DCTFWEB" },
            { "IRRF", "DCTFWEB" },
            {"CONTRIB PREVID DESCONTADA DE SEGURADOS", "DCTFWEB"},
            {"CONTRIB PREV DESCONTADA DE SEGURADO CONTRIBUINTE INDIVIDUAL", "DCTFWEB"},
            {"CONTRIBUIÇÃO PREVIDENCIÁRIA EMPREGADOR/EMPRESA", "DCTFWEB"},
            {"CONTRIB PREVID RISCO AMBIENTAL/APOSENTADORIA ESPECIAL", "DCTFWEB"},
        };

        var prioridades = new List<string>();
        prioridades.AddRange(termosComuns);
        prioridades.AddRange(tributosComParcelamento);
        prioridades.AddRange(termosEspeciais.Keys);

        // Quando vários tributos aparecem na mesma descrição (ex.: "CSLL - ... IRPJ ..."),
        // vale o que estiver mais à esquerda. Em empate de posição, mantém a ordem de prioridades acima.
        string? termoVencedor = null;
        var melhorIndice = int.MaxValue;
        var melhorOrdemNaLista = int.MaxValue;

        for (var ordem = 0; ordem < prioridades.Count; ordem++)
        {
            var termo = prioridades[ordem];
            var indice = linhaMaiuscula.IndexOf(termo, StringComparison.Ordinal);
            if (indice < 0)
                continue;

            if (indice < melhorIndice
                || (indice == melhorIndice && ordem < melhorOrdemNaLista))
            {
                melhorIndice = indice;
                melhorOrdemNaLista = ordem;
                termoVencedor = termo;
            }
        }

        if (string.IsNullOrEmpty(termoVencedor))
            return "PG. DESCONHECIDO XX";

        var historico = termosEspeciais.ContainsKey(termoVencedor) ? termosEspeciais[termoVencedor] : termoVencedor;

        var temParcelamento = tributosComParcelamento.Contains(termoVencedor) &&
                              linhaMaiuscula.Contains("PARCELAMENTO");

        if (temParcelamento)
        {
            historico += " PARCELAMENTO";
        }

        return $"PG. {historico} XX";
    }

    public static List<decimal> MapearDebito(List<string> historico)
    {
        return historico.Select(item =>
        {
            var h = item.ToUpper();

            if (h.Contains("SIMPLES NACIONAL")) return 531m;
            if (h.Contains("PIS")) return 179m;
            if (h.Contains("COFINS")) return 180m;
            if (h.Contains("IRPJ")) return 174m;
            if (h.Contains("CSLL")) return 175m;
            if (h.Contains("ISS")) return 173m;
            if (h.Contains("MULTA E JUROS")) return 352m;
            if (h.Contains("MULTA") || h.Contains("DESCONHECIDO")) return 350m;
            if (h.Contains("INSS")) return 191m;
            if (h.Contains("IRRF")) return 178m;

            return 0m;
        }).ToList();
    }

    public static (List<string> Descricoes, List<decimal> Valores) AgruparDescricoesEValores(
        List<string> descricoes, List<decimal> totais)
    {
        var mapa = new Dictionary<string, decimal>();

        for (var i = 0; i < descricoes.Count; i++)
        {
            var descricao = descricoes[i];
            var valor = totais[i];

            if (mapa.ContainsKey(descricao))
            {
                mapa[descricao] += valor;
            }
            else
            {
                mapa[descricao] = valor;
            }
        }

        return (
            mapa.Keys.ToList(),
            mapa.Values.Select(v => decimal.Round(v, 2)).ToList()
        );
    }

    public static HistoricoValues? ParseLinhaHistorico(string linha)
    {
        var regex = new Regex(@"(-|\d{1,3}(?:\.\d{3})*,\d{2})");
        var matches = regex.Matches(linha);
        var valores = matches.Select(m => m.Value).ToList();

        if (valores.Count < 4) return null;

        var principal = valores[^4];
        var multa = valores[^3];
        var juros = valores[^2];
        var total = valores[^1];

        decimal ToNumber(string val)
        {
            var cleaned = (val == "-" ? "0,00" : val)
                .Replace(".", "")
                .Replace(",", ".");
            return decimal.Parse(cleaned, CultureInfo.InvariantCulture);
        }

        return new HistoricoValues
        {
            Principal = ToNumber(principal),
            Multa = ToNumber(multa),
            Juros = ToNumber(juros),
            Total = ToNumber(total)
        };
    }

    public static TotaisValues? ParseTotaisLinha(string linha)
    {
        var regex = new Regex(@"(\d{1,3}(?:\.\d{3})*,\d{2})");
        var matches = regex.Matches(linha.Replace("-", "0,00"));
        var valoresStr = matches.Select(m => m.Value).ToList();

        if (valoresStr.Count != 4) return null;

        var valoresNum = valoresStr.Select(v =>
            decimal.Parse(v.Replace(".", "").Replace(",", "."), CultureInfo.InvariantCulture)
        ).ToList();

        var somaMultaJuros = decimal.Round(valoresNum[1] + valoresNum[2], 2);

        return new TotaisValues
        {
            Principal = valoresNum[0],
            Multa = valoresNum[1],
            Juros = valoresNum[2],
            Total = valoresNum[3],
            SomaMultaJuros = somaMultaJuros
        };
    }
}

public class HistoricoValues
{
    public decimal Principal { get; set; }
    public decimal Multa { get; set; }
    public decimal Juros { get; set; }
    public decimal Total { get; set; }
}

public class TotaisValues
{
    public decimal Principal { get; set; }
    public decimal Multa { get; set; }
    public decimal Juros { get; set; }
    public decimal Total { get; set; }
    public decimal SomaMultaJuros { get; set; }
}