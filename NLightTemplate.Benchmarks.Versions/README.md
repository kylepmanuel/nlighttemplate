# NLightTemplate.Benchmarks.Versions

Compares render performance across published NuGet versions of NLightTemplate in a single run.
BenchmarkDotNet swaps the NuGet reference per version, so the comparison is apples-to-apples.
Run in **Release**.

## Run and regenerate the report

Set `NLT_REPORT_DIR` to write the committable report (a Markdown table + an SVG chart). Without it, the
benchmark still runs but no report files are written (the program prints a reminder).

PowerShell (from the repository root):
```powershell
$env:NLT_REPORT_DIR = "docs/benchmarks"
dotnet run -c Release --project NLightTemplate.Benchmarks.Versions -- --filter "*"
```

bash:
```bash
NLT_REPORT_DIR=docs/benchmarks dotnet run -c Release --project NLightTemplate.Benchmarks.Versions -- --filter "*"
```

`NLT_REPORT_DIR` is resolved relative to the directory you run from (or pass an absolute path); the program
prints the final location. This regenerates `docs/benchmarks/benchmarks.md` and `benchmarks.svg`, which the
main README embeds.

## Choose versions

Defaults to `1.1.0,2.0.0`. Override with `NLT_VERSIONS` (comma-separated, oldest first):
```powershell
$env:NLT_VERSIONS = "1.1.0,2.0.0"
```

The baseline `PackageReference` in the `.csproj` must stay at the **oldest** version being compared: a
downgrade trips NuGet's NU1605 and that version fails to build.

## Notes

- Absolute numbers reflect the machine that produced them; read the version-over-version comparison as the signal.
- The `benchmarks.yml` workflow runs this automatically on release / manual dispatch and opens a PR with the
  refreshed report.
