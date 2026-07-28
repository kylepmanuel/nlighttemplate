using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Running;

namespace NLightTemplate.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        // Resolve any bring-your-own file paths to absolute up front, so they still point at the right
        // files inside BenchmarkDotNet's spawned benchmark processes (which run from a different directory).
        foreach (var variable in new[] { "NLT_TEMPLATE_FILE", "NLT_DATA_FILE" })
        {
            var path = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrEmpty(path))
                Environment.SetEnvironmentVariable(variable, Path.GetFullPath(path));
        }

        BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).Run(args);
    }
}
