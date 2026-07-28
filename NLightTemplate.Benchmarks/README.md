# NLightTemplate.Benchmarks

[BenchmarkDotNet](https://benchmarkdotnet.org/) benchmarks for NLightTemplate. Benchmarks must be run in **Release**.

## Run the bundled suite

```bash
dotnet run -c Release --project NLightTemplate.Benchmarks -- --filter "*SampleBenchmarks*"
```

`SampleBenchmarks` renders a fixed sample (`samples/customer.template.txt` + `samples/customer.data.json`) both as a strongly-typed POCO (reflection path) and as an `ExpandoObject` (dynamic path).

## Benchmark your own template and data

`CustomTemplateBenchmarks` renders whatever you point it at (dynamic path). Copy `samples/customer.template.txt` and `samples/customer.data.json` as a starting point, then supply your files via environment variables:

bash / zsh:
```bash
NLT_TEMPLATE_FILE=./my.template.txt NLT_DATA_FILE=./my.data.json \
  dotnet run -c Release --project NLightTemplate.Benchmarks -- --filter "*CustomTemplateBenchmarks*"
```

PowerShell:
```powershell
$env:NLT_TEMPLATE_FILE = ".\my.template.txt"; $env:NLT_DATA_FILE = ".\my.data.json"
dotnet run -c Release --project NLightTemplate.Benchmarks -- --filter "*CustomTemplateBenchmarks*"
```

The data file is JSON, deserialized to an `ExpandoObject` (the same shape NLightTemplate handles for dynamic input). Relative paths are resolved from the directory you run the command in.

## All benchmarks

```bash
dotnet run -c Release --project NLightTemplate.Benchmarks -- --filter "*"
```

Add `--job dry` for a fast smoke run (one iteration, no statistics). Full results are written under `BenchmarkDotNet.Artifacts/` (Markdown, CSV, and HTML).
