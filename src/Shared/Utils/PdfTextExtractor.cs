using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig.Content;

namespace ApiPdfCsv.Shared.Utils;

public static class PdfTextExtractor
{
    public static List<string> ExtractLines(Page page)
    {
        return page.GetWords()
            .GroupBy(word => System.Math.Round(word.BoundingBox.Bottom, 0))
            .OrderByDescending(group => group.Key)
            .Select(group => string.Join(" ", group.OrderBy(word => word.BoundingBox.Left).Select(word => word.Text)))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }
}
