using System;
using System.Net.Http;
using System.Threading.Tasks;
using BnL.TextFieldUrlContentValidator.Configuration;
using BnL.TextFieldUrlContentValidator.Services;
using BnL.TextFieldUrlContentValidator.UI;
using GdalToolsLib.Models;

namespace BnL.TextFieldUrlContentValidator
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                ConsoleUi.Banner();

                var options = OptionsLoader.Load(args);
                ConsoleUi.ShowScanIntro(options.GetValidatedStartDirectory());

                var accessor = new OgctDataSourceAccessor();
                var scanner = new GeodatabaseScanner(accessor);
                var candidates = scanner.Scan(options.GetValidatedStartDirectory());

                if (candidates.Count == 0)
                {
                    Console.WriteLine("No text fields starting with 'http' were discovered.");
                    return 0;
                }

                ConsoleUi.ShowCandidatesSummary(candidates);
                if (!ConsoleUi.ConfirmProceed())
                {
                    Console.WriteLine("No validation performed.");
                    return 0;
                }

                using var httpClient = CreateHttpClient();
                var validator = new PdfValidationService(accessor, httpClient);
                var summary = validator.ExecuteAsync(candidates).GetAwaiter().GetResult();

                ConsoleUi.ShowValidationSummary(summary);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled error: {ex.Message}");
                return 1;
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BnL.TextFieldUrlContentValidator/1.0");
            return client;
        }
    }
}
