using BnL.CopyDissolverFGDB;
using BnL.CopyDissolverFGDB.Parameters;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


var workDir = "D:\\Daten\\MMO\\temp\\CopyDissolverTest";

var filterParameters = CopyDissolverHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\filters.txt")
                                .Select(line => new FilterParameter(line));
var bufferParameters = CopyDissolverHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\buffers.txt")
                                .Select(line => new BufferParameter(line));

var unionParameters = CopyDissolverHelpers.GetLinesWithoutComments("D:\\Daten\\MMO\\GDALTools_NET8\\BnL.CopyDissolverFGDB\\unions.txt")
                                .Select(line => new UnionParameter(line));

(string, string)[] renamePatterns = [("_Park_", "_ParkKernzone_")];

string[] dissolveFieldNames = ["ObjNummer", "Name"];

string[] searchDirs =
[
    @"G:\BnL\Daten\Ablage\DNL\Bundesinventare",
    @"G:\BnL\Daten\Ablage\DNL\Schutzgebiete",
];

AnsiConsole.Write(new FigletText("CopyDissolver").Centered().Color(Color.Red));


List<string> allGdbPaths = [];

var root = new Tree("[bold]Found Datasources:[/]");

AnsiConsole.Status().Start("Searching subfolders...", ctx =>
{
    allGdbPaths = searchDirs.SelectMany(searchDir =>
    {
        AnsiConsole.WriteLine($"Searching {searchDir}...");
        var dirRoot = root.AddNode($"[yellow]{searchDir}[/]");
        var gdbs = CopyDissolverHelpers.CollectGeodataFiles(searchDir);

        dirRoot.AddNodes(gdbs.Select(p => new TextPath(p).LeafColor(Color.Yellow)));
        return gdbs;
    }).ToList();
});


AnsiConsole.Write(new Panel(root));

var shouldContinue = AnsiConsole.Prompt(new ConfirmationPrompt("Looks good?"));

if (!shouldContinue) return;

var warningGrid = new Grid().AddColumns(1);
bool hasWarning = false;

var fgdbProcessors = await Task.WhenAll(allGdbPaths.Select(path =>
{
    return Task.Run(() =>
    {

        // 1. Print warnings and info
        FGDBProcessor fGDBProcessor = new(path, dissolveFieldNames, filterParameters, bufferParameters, unionParameters, renamePatterns);
        if (fGDBProcessor.HasWarnings)
        {
            hasWarning = true;
            var gd = new Grid().AddColumn();

            if (fGDBProcessor.layersWithoutDissolveFields.Count > 0)
            {
                gd.AddRow(new Rows(new Text("Layers without dissolve fields:", Color.Red), new Rows(fGDBProcessor.layersWithoutDissolveFields.Select(l => new Text(l)))));
            }

            if (fGDBProcessor.nonPointBufferLayers.Count > 0)
            {
                gd.AddRow(new Rows(new Text("Non-point layers to be buffered:", Color.Red), new Rows(fGDBProcessor.nonPointBufferLayers.Select(l => new Text(l)))));
            }
            warningGrid.AddRow(new Panel(gd).Header($"[yellow]{Path.GetFileName(path)}[/]"));
        }
        return fGDBProcessor;
    });
}).ToArray());

AnsiConsole.Write(warningGrid);
if (hasWarning && !AnsiConsole.Prompt(new ConfirmationPrompt("Continue despite warnings?"))) return;

await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ElapsedTimeColumn(), new SpinnerColumn().CompletedText("[green]Done![/]")).StartAsync(async ctx =>
{
    await Task.WhenAll(fgdbProcessors.Select(processor =>
    {
        var tsk = ctx.AddTask(processor.sourceGdbPath);

        return Task.Run(() =>
        {
            processor.Run(Path.Join(workDir, Path.GetFileName(processor.sourceGdbPath)));
            tsk.StopTask();
        });
    }));
});

Console.WriteLine("Goodbye!");
Console.ReadKey();
