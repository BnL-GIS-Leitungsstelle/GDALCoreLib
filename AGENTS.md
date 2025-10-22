# Repository Guidelines

## Project Structure & Module Organization
`GdalToolsLib/` hosts the core GDAL/OGR abstractions that every app consumes; treat it as the single source of truth for geodata utilities. `ESRIFileGeodatabaseAPI/` bridges to native FGDB bindings, while the CLI tools (`BnL.*`, `CreateOptimizedGeopackage/`, `ReadLayerDataIntoExcel/`) show concrete workflows wired through appsettings under their `config/` or `Parameters/` folders. Automated tests and sample datasets live in `GdalToolsTest/`, with reusable fixtures in `Helper/` and inputs under `samples-*` and `testdata/`. Diagrams and supporting docs belong in `doc/`.

## Build, Test, and Development Commands
- `dotnet restore GdalTools.App.sln` — hydrate all SDK and native GDAL packages.
- `dotnet build GdalTools.App.sln -c Release` — validate the solution builds on your platform; use Debug during iterative work.
- `dotnet test GdalTools.App.sln --collect:"XPlat Code Coverage"` — run the xUnit suite and emit coverlet results; keep failures attached to the PR.
- `dotnet run --project BnL.CopyDissolverFGDB/BnL.CopyDissolverFGDB.csproj -- --help` — verify CLI UX before submitting command-line changes.

## Coding Style & Naming Conventions
Project files disable implicit usings and enable nullable reference types, so declare every `using` and fix nullable warnings. Stick to 4-space indentation, PascalCase for types, and camelCase for locals; prefix private fields with `_`. Async members should carry the `Async` suffix and return `Task`. Prefer dependency injection through constructors (see `Microsoft.Extensions.DependencyInjection` usage) and encapsulate GDAL handles in disposable wrappers. Run `dotnet format` prior to review to keep whitespace and usings tidy.

## Testing Guidelines
Author new coverage in `GdalToolsTest/`, mirroring production namespaces and ending files with `Tests`. Use xUnit `[Theory]` plus `[InlineData]` when you can exercise multiple datasets and FluentAssertions for expressive checks. Place fixtures, SQL, and sample geodata in the existing `samples-*` folders rather than inventing new roots, and keep binary inputs as small as possible. Always reproduce bugs with a failing test before fixing them, and include coverage that proves GDAL configuration (e.g., runtime path resolution) for the scenario.

## Commit & Pull Request Guidelines
Aim for short, imperative commit subjects (`gdaltools: enable xml export`); bundle related code and data changes together. Reference issues or tickets with `#id` when applicable. Every PR needs: a concise problem statement, a summary of the solution, test evidence (`dotnet test` output or coverage diff), and notes about required dataset or config changes. Capture CLI output or GUI screenshots when user-visible behavior shifts, and call out any new native dependencies so reviewers can replicate the environment.

