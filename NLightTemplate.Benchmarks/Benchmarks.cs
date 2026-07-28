using System;
using System.Dynamic;
using System.IO;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;

namespace NLightTemplate.Benchmarks;

/// <summary>
/// The canonical suite: renders the fixed bundled sample as a strongly-typed POCO (reflection path)
/// and as an ExpandoObject (dynamic path). Fixed inputs make it comparable across runs and versions.
/// </summary>
[MemoryDiagnoser]
public class SampleBenchmarks
{
    private string _template;
    private Customer _poco;
    private object _dynamic;

    [GlobalSetup]
    public void Setup()
    {
        _template = SampleData.Template;
        _poco = JsonConvert.DeserializeObject<Customer>(SampleData.Json);
        _dynamic = JsonConvert.DeserializeObject<ExpandoObject>(SampleData.Json);
    }

    [Benchmark(Baseline = true)]
    public string Poco() => StringTemplate.Render(_template, _poco);

    [Benchmark]
    public string Dynamic() => StringTemplate.Render(_template, _dynamic);
}

/// <summary>
/// Bring-your-own benchmark: renders the template and data you point it at (dynamic path), falling back to
/// the bundled sample when no files are supplied. Set NLT_TEMPLATE_FILE and NLT_DATA_FILE (paths are
/// resolved to absolute in <see cref="Program"/> so relative paths work).
/// </summary>
[MemoryDiagnoser]
public class CustomTemplateBenchmarks
{
    private string _template;
    private object _data;

    [GlobalSetup]
    public void Setup()
    {
        var templatePath = Environment.GetEnvironmentVariable("NLT_TEMPLATE_FILE");
        var dataPath = Environment.GetEnvironmentVariable("NLT_DATA_FILE");

        _template = templatePath != null ? File.ReadAllText(templatePath) : SampleData.Template;
        var json = dataPath != null ? File.ReadAllText(dataPath) : SampleData.Json;
        _data = JsonConvert.DeserializeObject<ExpandoObject>(json);
    }

    [Benchmark]
    public string Render() => StringTemplate.Render(_template, _data);
}
