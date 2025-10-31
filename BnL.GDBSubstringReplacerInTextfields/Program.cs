using System;
using BnL.GDBSubstringReplacerInTextfields.Configuration;
using BnL.GDBSubstringReplacerInTextfields.Services;
using BnL.GDBSubstringReplacerInTextfields.UI;
using GdalToolsLib.Models;

namespace BnL.GDBSubstringReplacerInTextfields
{
    internal static class Program
    {
        private const string OldSubstring = "data.geo.admin.ch";
        private const string NewSubstring = "api3.geo.admin.ch/featureattachments";

        private static int Main(string[] args)
        {
            try
            {
                ConsoleUi.Banner();
                var options = OptionsLoader.Load(args);

                ConsoleUi.ShowScanIntro(options.GetValidatedStartDirectory(), OldSubstring, NewSubstring);

                var accessor = new OgctDataSourceAccessor();
                var scanner = new GeodatabaseScanner(accessor, OldSubstring);
                var candidates = scanner.Scan(options.GetValidatedStartDirectory());

                if (candidates.Count == 0)
                {
                    Console.WriteLine("No text fields containing the targeted URL were discovered.");
                    return 0;
                }

                ConsoleUi.ShowCandidatesSummary(candidates);
                if (!ConsoleUi.ConfirmProceed())
                {
                    Console.WriteLine("No replacements were performed.");
                    return 0;
                }

                var replacer = new ReplacementService(accessor, OldSubstring, NewSubstring);
                replacer.Execute(candidates);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled error: {ex.Message}");
                return 1;
            }
        }
    }
}
