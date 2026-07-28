using System;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace NLightTemplate.Benchmarks.Versions;

public static class Program
{
    public static void Main(string[] args)
    {
        // Which published NuGet versions to compare (oldest -> newest). Override with NLT_VERSIONS,
        // e.g. NLT_VERSIONS="1.1.0,2.0.0,2.1.0".
        var versions = (Environment.GetEnvironmentVariable("NLT_VERSIONS") ?? "1.1.0,2.0.0")
            .Split([','], StringSplitOptions.RemoveEmptyEntries)
            .Select(v => v.Trim())
            .ToArray();

        var config = ManualConfig.CreateMinimumViable()
            .AddDiagnoser(MemoryDiagnoser.Default);

        // BenchmarkDotNet runs the same benchmark once per version by swapping the NuGet reference.
        foreach (var version in versions)
        {
            config = config.AddJob(Job.Default.WithNuGet("NLightTemplate", version).WithId(version));
        }

        var summary = BenchmarkRunner.Run<VersionBenchmarks>(config, args);

        // When NLT_REPORT_DIR is set (e.g. in CI), emit the committable Markdown table + SVG chart.
        var reportDir = Environment.GetEnvironmentVariable("NLT_REPORT_DIR");
        if (!string.IsNullOrEmpty(reportDir))
        {
            ReportGenerator.Write(summary, reportDir);
            Console.WriteLine($"Report written to {Path.GetFullPath(reportDir)}");
        }
        else
        {
            Console.WriteLine("Tip: set NLT_REPORT_DIR (e.g. \"docs/benchmarks\") to also write the Markdown table + SVG chart.");
        }
    }
}
