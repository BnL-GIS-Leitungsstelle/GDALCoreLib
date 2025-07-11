using BnL.CopyDissolverFGDB;
using BnL.CopyDissolverFGDB.config;
using BnL.CopyDissolverFGDB.Parameters;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // configuration builder
        // Lies Konfiguration aus appsettings.json
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        // Optional: In eine eigene Klasse binden
        var appConfig = config.Get<AppConfig>();

        // Arbeitsverzeichnis mit Datum ergänzen:
        var today = DateTime.Now.ToString("yyyyMMdd_hhmmss");
        var workDir = appConfig.WorkDir + today;

        // Umwandlung der RenamePatterns in Tupel, falls benötigt:
        //(string, string)[] renamePatterns = appConfig.RenamePatterns
        //    .Select(rp => (rp.From, rp.To))
        //    .ToArray();

 

        var filterParameters = CopyDissolverHelpers
            .GetLinesWithoutComments("filters.txt")
            .Select(line => new FilterParameter(line))
            .ToList()
            .AsReadOnly();
        var bufferParameters = CopyDissolverHelpers
            .GetLinesWithoutComments("buffers.txt")
            .Select(line => new BufferParameter(line))
            .ToList()
            .AsReadOnly();

        var unionParameters = CopyDissolverHelpers
            .GetLinesWithoutComments("unions.txt")
            .Select(line => new UnionParameter(line))
            .ToList()
            .AsReadOnly();


        AnsiConsole.Write(new FigletText("CopyDissolver").Centered().Color(Color.Red));


        List<string> allGdbPaths = [];

        AnsiConsole.Status().Start("Searching subfolders...", ctx =>
        {
            var root = new Tree("[bold]Found Datasources:[/]");
            allGdbPaths = appConfig.SearchDirs.SelectMany(searchDir =>
            {
                AnsiConsole.WriteLine($"Searching {searchDir}...");
                var dirRoot = root.AddNode($"[yellow]{searchDir}[/]");
                var gdbs = CopyDissolverHelpers.CollectGeodataFiles(searchDir);

                dirRoot.AddNodes(gdbs.Select(p => new TextPath(p).LeafColor(Color.Yellow)));
                return gdbs;
            }).ToList();
            AnsiConsole.Write(new Panel(root));
        });

        var hasWarning = false;

        AnsiConsole.MarkupLine("[bold]Warnings:[/]");
        var fgdbProcessors = await Task.WhenAll(allGdbPaths.Select(path =>
        {
            return Task.Run(() =>
            {
                FGDBProcessor fGDBProcessor = new(path, appConfig.DissolveFieldNames.ToArray(), filterParameters, bufferParameters, unionParameters,
                    appConfig.RenamePatterns.Select(rp => (rp.From, rp.To)).ToArray());
                if (!fGDBProcessor.HasWarnings) return fGDBProcessor;
                hasWarning = true;
                var gd = new Grid().AddColumn();

                if (fGDBProcessor.layersWithoutDissolveFields.Count > 0)
                {
                    gd.AddRow(new Rows(new Text("Layers without dissolve fields:", Color.Red),
                        new Rows(fGDBProcessor.layersWithoutDissolveFields.Select(l => new Text(l)))));
                }

                if (fGDBProcessor.nonPointBufferLayers.Count > 0)
                {
                    gd.AddRow(new Rows(new Text("Non-point layers to be buffered:", Color.Red),
                        new Rows(fGDBProcessor.nonPointBufferLayers.Select(l => new Text(l)))));
                }

                if (fGDBProcessor.zMGeometryLayers.Count > 0)
                {
                    gd.AddRow(new Rows(new Text("Layers with ZM geometry:", Color.Red),
                        new Rows(fGDBProcessor.zMGeometryLayers.Select(l => new Text(l)))));
                }

                AnsiConsole.Write(new Panel(gd).Header($"[yellow]{Path.GetFileName(path)}[/]"));

                return fGDBProcessor;
            });
        }).ToArray());

        if (hasWarning && !AnsiConsole.Prompt(new ConfirmationPrompt("Continue despite warnings?"))) return;

        Directory.CreateDirectory(workDir);

        await AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new ElapsedTimeColumn(), new SpinnerColumn().CompletedText("[green]Done![/]"))
            .StartAsync(async ctx =>
            {
                await Task.WhenAll(fgdbProcessors.Select(processor =>
                {
                    var tsk = ctx.AddTask(processor.sourceGdbPath, false);

                    return Task.Run(() =>
                    {
                        tsk.StartTask();
                        processor.Run(Path.Join(workDir, Path.GetFileName(processor.sourceGdbPath)));
                        tsk.StopTask();
                    });
                }));
            });

        Console.WriteLine("Goodbye!");
        Console.ReadKey();



    }
}