using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

namespace BnL.FGDBCopy
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            AnsiConsole.Write(new FigletText("FGDB Copy").Color(Color.Green));
            AnsiConsole.WriteLine();

            try
            {
                var options = LoadOptions(args);
                ValidateFolders(options);

                var geodatabases = DiscoverGeodatabases(options.SourceFolder);

                if (geodatabases.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No file geodatabases (.gdb) were found in the configured source folder.[/]");
                    return 0;
                }

                var results = CopyGeodatabases(geodatabases, options);

                AnsiConsole.WriteLine();
                var table = new Table().Border(TableBorder.Rounded).Centered();
                table.AddColumn("[bold]Geodatabase[/]");
                table.AddColumn("[bold]Destination[/]");
                table.AddColumn("[bold]Status[/]");
                table.AddColumn("[bold]Size[/]");

                foreach (var result in results)
                {
                    var status = result.Overwrote ? "[yellow]Overwritten[/]" : "[green]Copied[/]";
                    table.AddRow(Markup.Escape(result.Name), Markup.Escape(result.Destination), status, FormatBytes(result.BytesCopied));
                }

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"\n[green]Completed copying {results.Count} geodatabase(s) to {Markup.Escape(options.TargetFolder)}.[/]");
                AnsiConsole.MarkupLine("[dim]Hint: use --source and --target switches to override configured paths.[/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
                return 1;
            }
        }

        private static FgdbCopyOptions LoadOptions(string[] args)
        {
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--source"] = "FgdbCopy:SourceFolder",
                ["-s"] = "FgdbCopy:SourceFolder",
                ["--target"] = "FgdbCopy:TargetFolder",
                ["-t"] = "FgdbCopy:TargetFolder"
            };

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables("FGDBCOPY_")
                .AddCommandLine(args, switchMappings)
                .Build();

            var options = new FgdbCopyOptions();
            configuration.GetSection("FgdbCopy").Bind(options);

            options.SourceFolder = options.SourceFolder == string.Empty ? configuration["SourceFolder"] ?? string.Empty : options.SourceFolder;
            options.TargetFolder = options.TargetFolder == string.Empty ? configuration["TargetFolder"] ?? string.Empty : options.TargetFolder;

            return options;
        }

        private static void ValidateFolders(FgdbCopyOptions options)
        {
            if (string.IsNullOrWhiteSpace(options.SourceFolder))
            {
                throw new InvalidOperationException("Source folder is not configured. Set FgdbCopy:SourceFolder in appsettings.json or pass --source <path>.");
            }

            if (string.IsNullOrWhiteSpace(options.TargetFolder))
            {
                throw new InvalidOperationException("Target folder is not configured. Set FgdbCopy:TargetFolder in appsettings.json or pass --target <path>.");
            }

            options.SourceFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.SourceFolder.Trim('"')));
            options.TargetFolder = Path.GetFullPath(Environment.ExpandEnvironmentVariables(options.TargetFolder.Trim('"')));

            if (!Directory.Exists(options.SourceFolder))
            {
                throw new DirectoryNotFoundException($"Source folder \"{options.SourceFolder}\" does not exist.");
            }

            Directory.CreateDirectory(options.TargetFolder);
        }

        private static List<string> DiscoverGeodatabases(string sourceFolder)
        {
            return AnsiConsole.Status()
                .Start("Scanning for file geodatabases...", _ =>
                {
                    return Directory.EnumerateDirectories(sourceFolder, "*.gdb", SearchOption.AllDirectories)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                });
        }

        private static List<CopyResult> CopyGeodatabases(IReadOnlyList<string> geodatabases, FgdbCopyOptions options)
        {
            var results = new List<CopyResult>(geodatabases.Count);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[blue]Found {geodatabases.Count} geodatabase(s) under {Markup.Escape(options.SourceFolder)}.[/]");
            AnsiConsole.WriteLine();

            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn()
                })
                .Start(ctx =>
                {
                    var task = ctx.AddTask("Copying geodatabases", maxValue: geodatabases.Count);

                    foreach (var path in geodatabases)
                    {
                        var result = CopyGeodatabaseTree(path, options);
                        results.Add(result);
                        task.Description = $"Copying {result.Name}".ToFixedString(50,'-');
                        task.Increment(1);
                    }

                    task.Description = "Copying geodatabases";
                });

            return results;
        }



        private static CopyResult CopyGeodatabaseTree(string sourceGeodatabase, FgdbCopyOptions options)
        {
            var relative = Path.GetRelativePath(options.SourceFolder, sourceGeodatabase);
            if (string.Equals(relative, ".", StringComparison.Ordinal))
            {
                relative = Path.GetFileName(sourceGeodatabase);
            }

            var destinationRoot = Path.Combine(options.TargetFolder, relative);
            var overwrote = Directory.Exists(destinationRoot);

            if (overwrote)
            {
                Directory.Delete(destinationRoot, true);
            }

            CopyDirectory(sourceGeodatabase, destinationRoot, out var bytesCopied);

            return new CopyResult(Path.GetFileName(sourceGeodatabase), destinationRoot, overwrote, bytesCopied);
        }

        private static void CopyDirectory(string source, string destination, out long bytesCopied)
        {
            bytesCopied = 0;
            Directory.CreateDirectory(destination);

            foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            {
                var targetDir = Path.Combine(destination, Path.GetRelativePath(source, directory));
                Directory.CreateDirectory(targetDir);
            }

            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var targetFile = Path.Combine(destination, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
                bytesCopied += new FileInfo(file).Length;
                File.Copy(file, targetFile, true);
            }
        }

        private static string FormatBytes(long value)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = value;
            var unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return string.Format(CultureInfo.InvariantCulture, "{0:F1} {1}", size, units[unitIndex]);
        }

        private sealed class FgdbCopyOptions
        {
            public string SourceFolder { get; set; } = string.Empty;
            public string TargetFolder { get; set; } = string.Empty;
        }

        private sealed record CopyResult(string Name, string Destination, bool Overwrote, long BytesCopied);
    }
}
