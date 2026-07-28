using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Reports;

namespace NLightTemplate.Benchmarks.Versions;

/// <summary>
/// Turns a BenchmarkDotNet <see cref="Summary"/> into committable artifacts: a Markdown table and an SVG
/// bar chart of render time per version, written to the directory in the NLT_REPORT_DIR environment variable.
/// </summary>
internal static class ReportGenerator
{
    public static void Write(Summary summary, string outputDir)
    {
        var rows = summary.Reports
            .Select(r => (
                Version: r.BenchmarkCase.Job.ResolvedId,
                MeanUs: (r.ResultStatistics?.Mean ?? 0) / 1000.0,          // ns -> us
                AllocKb: AllocatedBytes(r) / 1024.0))
            .Where(r => r.MeanUs > 0)
            .OrderBy(r => r.Version, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (rows.Count == 0) return;

        Directory.CreateDirectory(outputDir);
        File.WriteAllText(Path.Combine(outputDir, "benchmarks.svg"), BuildSvg(rows));
        File.WriteAllText(Path.Combine(outputDir, "benchmarks.md"), BuildMarkdown(rows));
    }

    private static double AllocatedBytes(BenchmarkReport report)
    {
        foreach (var metric in report.Metrics)
        {
            if (metric.Key.Contains("Allocated", StringComparison.OrdinalIgnoreCase))
            {
                return metric.Value.Value;
            }
        }

        return double.NaN;
    }

    private static string BuildMarkdown(List<(string Version, double MeanUs, double AllocKb)> rows)
    {
        var baseline = rows[0].MeanUs;
        var sb = new StringBuilder();
        sb.AppendLine("# Render performance by version");
        sb.AppendLine();
        sb.AppendLine($"Generated {DateTime.UtcNow:yyyy-MM-dd} (UTC). Lower is better. Numbers reflect the machine that produced them, so treat the version-over-version comparison as the signal, not the absolute values.");
        sb.AppendLine();
        sb.AppendLine("| Version | Mean | vs oldest | Allocated |");
        sb.AppendLine("|---------|------|-----------|-----------|");
        foreach (var r in rows)
        {
            var ratio = baseline > 0 ? r.MeanUs / baseline : 1;
            var alloc = double.IsNaN(r.AllocKb) ? "-" : r.AllocKb.ToString("0.0", CultureInfo.InvariantCulture) + " KB";
            sb.AppendLine($"| {r.Version} | {r.MeanUs.ToString("0.0", CultureInfo.InvariantCulture)} us | {ratio.ToString("0.00", CultureInfo.InvariantCulture)}x | {alloc} |");
        }
        sb.AppendLine();
        sb.AppendLine("![Render time by version](benchmarks.svg)");
        return sb.ToString();
    }

    private static string BuildSvg(List<(string Version, double MeanUs, double AllocKb)> rows)
    {
        const int width = 680, rowHeight = 46, top = 66, left = 96, rightPad = 150;
        var barMax = width - left - rightPad;
        var height = top + rows.Count * rowHeight + 16;
        var maxMean = rows.Max(r => r.MeanUs);

        static string N(double v) => v.ToString("0.0", CultureInfo.InvariantCulture);

        var sb = new StringBuilder();
        sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" font-family=\"Segoe UI, Helvetica, Arial, sans-serif\">");
        sb.Append($"<rect width=\"{width}\" height=\"{height}\" fill=\"#ffffff\"/>");
        sb.Append($"<text x=\"{left}\" y=\"30\" font-size=\"18\" font-weight=\"700\" fill=\"#111827\">NLightTemplate render time by version</text>");
        sb.Append($"<text x=\"{left}\" y=\"50\" font-size=\"12\" fill=\"#6b7280\">lower is better &#183; mean microseconds (allocated KB)</text>");

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var y = top + i * rowHeight;
            var barWidth = maxMean > 0 ? (int)Math.Round(barMax * r.MeanUs / maxMean) : 0;
            sb.Append($"<text x=\"{left - 12}\" y=\"{y + 23}\" font-size=\"13\" text-anchor=\"end\" fill=\"#111827\">{r.Version}</text>");
            sb.Append($"<rect x=\"{left}\" y=\"{y + 8}\" width=\"{barWidth}\" height=\"24\" rx=\"3\" fill=\"#4c78a8\"/>");
            var alloc = double.IsNaN(r.AllocKb) ? "" : $" ({N(r.AllocKb)} KB)";
            sb.Append($"<text x=\"{left + barWidth + 10}\" y=\"{y + 25}\" font-size=\"12\" fill=\"#374151\">{N(r.MeanUs)} us{alloc}</text>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }
}
