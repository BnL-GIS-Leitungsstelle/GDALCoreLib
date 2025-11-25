using System;
using System.Collections.Generic;
using System.Linq;
using BnL.TextFieldUrlContentValidator.Models;
using Spectre.Console;

namespace BnL.TextFieldUrlContentValidator.UI
{
    internal static class ConsoleUi
    {
        public static void Banner()
        {
            try
            {
                AnsiConsole.Write(new FigletText("URL Content Validator").Color(Color.Blue));
            }
            catch
            {
                AnsiConsole.MarkupLine("[bold blue]URL Content Validator[/]");
            }
        }

        public static void ShowScanIntro(string startDirectory)
        {
            var panel = new Panel(new Markup($"[grey]Scanning[/] [bold]{Escape(startDirectory)}[/]\n[grey]Detect[/] [yellow]text fields where first record starts with 'http'[/]"))
            {
                Border = BoxBorder.Rounded,
                Header = new PanelHeader("Configuration"),
                Padding = new Padding(1, 1, 1, 1)
            };
            AnsiConsole.Write(panel);
        }

        public static void ShowCandidatesSummary(IReadOnlyCollection<LayerCandidate> candidates)
        {
            var summary = candidates
                .GroupBy(c => c.GeodatabasePath)
                .Select(g => new
                {
                    Geodatabase = g.Key,
                    Count = g.Count(),
                    Layers = g.GroupBy(x => x.LayerName).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                })
                .OrderBy(x => x.Geodatabase, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var table = new Table().Border(TableBorder.Rounded).Title("[bold]Candidate Summary[/]");
            table.AddColumn("Geodatabase");
            table.AddColumn("Fields Found");
            table.AddColumn("Layers (sample)");

            foreach (var item in summary)
            {
                var layerSamples = item.Layers
                    .Select(l => $"{l.Key} (" + string.Join(", ", l.Select(f => f.FieldName).Distinct().Take(3)) + (l.Count() > 3 ? ", …" : ")"))
                    .Take(5);

                table.AddRow(Escape(item.Geodatabase), item.Count.ToString(), Escape(string.Join("; ", layerSamples)));
            }

            AnsiConsole.Write(table);

            var note = new Panel(new Markup($"[grey]{candidates.Count} field(s) match. Review and confirm to proceed.[/]") )
            {
                Border = BoxBorder.Ascii,
                Padding = new Padding(1, 0, 1, 0)
            };
            AnsiConsole.Write(note);
        }

        public static bool ConfirmProceed()
        {
            return AnsiConsole.Confirm("Proceed with URL content validation?", defaultValue: false);
        }

        public static void ShowValidationSummary(Services.ValidationSummary summary)
        {
            var table = new Table().Border(TableBorder.Rounded).Title("[bold]Validation Summary[/]");
            table.AddColumn("Metric");
            table.AddColumn("Count");
            table.AddRow("Layers processed", summary.LayersProcessed.ToString());
            table.AddRow("URLs checked", summary.UrlsChecked.ToString());
            table.AddRow("Validated PDFs", summary.UrlsValidated.ToString());
            table.AddRow("Skipped (cached valid)", summary.UrlsSkippedCached.ToString());
            table.AddRow("Skipped (cached failed)", summary.UrlsSkippedFailedCached.ToString());
            table.AddRow("Failed", summary.UrlsFailed.ToString());
            AnsiConsole.Write(table);
        }

        private static string Escape(string value) => Markup.Escape(value);
    }
}
