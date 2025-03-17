using BnL.CopyDissolverFGDB;
using BnL.CopyDissolverFGDB.Parameters;
using ESRIFileGeodatabaseAPI;
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

if (shouldContinue)
{
    await AnsiConsole.Progress().Columns(new TaskDescriptionColumn(), new ElapsedTimeColumn(), new SpinnerColumn().CompletedText("[green]Done![/]")).StartAsync(async ctx =>
    {
        await Task.WhenAll(allGdbPaths.Select(path =>
        {
            var tsk = ctx.AddTask(path);

            return Task.Run(() =>
            {
                var outPath = Path.Join(workDir, Path.GetFileName(path));
                FGDBProcessor fGDBProcessor = new(path, dissolveFieldNames, filterParameters, bufferParameters, unionParameters);
                fGDBProcessor.Run(outPath);
                tsk.StopTask();
            });
        }).ToArray());

    });
}

Console.WriteLine("Goodbye!");
Console.ReadKey();
