using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BnL.TextFieldUrlContentValidator.Models;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Models;

namespace BnL.TextFieldUrlContentValidator.Services
{
    internal sealed class PdfValidationService
    {
        private static readonly byte[] PdfSignature = { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"

        private readonly OgctDataSourceAccessor _accessor;
        private readonly HttpClient _httpClient;
        private readonly HashSet<string> _validatedUrls = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _failedUrls = new(StringComparer.OrdinalIgnoreCase);

        public PdfValidationService(OgctDataSourceAccessor accessor, HttpClient httpClient)
        {
            _accessor = accessor;
            _httpClient = httpClient;
        }

        public async Task<ValidationSummary> ExecuteAsync(IEnumerable<LayerCandidate> candidates, CancellationToken cancellationToken = default)
        {
            int layersProcessed = 0;
            int urlsChecked = 0;
            int urlsValidated = 0;
            int urlsSkippedCached = 0;
            int urlsSkippedFailedCached = 0;
            int urlsFailed = 0;

            foreach (var group in candidates.GroupBy(c => c.GeodatabasePath))
            {
                using var ds = _accessor.OpenOrCreateDatasource(group.Key, EAccessLevel.ReadOnly);

                foreach (var candidate in group)
                {
                    layersProcessed++;

                    using var layer = ds.OpenLayer(candidate.LayerName);
                    var schema = layer.LayerDetails.Schema;
                    if (schema == null)
                    {
                        continue;
                    }

                    var field = schema.FieldList.FirstOrDefault(f => string.Equals(f.Name, candidate.FieldName, StringComparison.OrdinalIgnoreCase));
                    if (field == null)
                    {
                        Console.Error.WriteLine($"  ! Field not found: {candidate.LayerName}.{candidate.FieldName}");
                        continue;
                    }

                    layer.ResetReading();
                    while (true)
                    {
                        using var feature = layer.OpenNextFeature();
                        if (feature == null)
                        {
                            break;
                        }

                        var value = feature.ReadValue(field);
                        if (value is not string text || string.IsNullOrWhiteSpace(text))
                        {
                            continue;
                        }

                        text = text.Trim();
                        if (!IsHttpPdfUrl(text))
                        {
                            continue;
                        }

                        if (_validatedUrls.Contains(text))
                        {
                            urlsSkippedCached++;
                            continue;
                        }

                        if (_failedUrls.Contains(text))
                        {
                            urlsSkippedFailedCached++;
                            continue;
                        }

                        urlsChecked++;
                        var ok = await DownloadAndValidatePdfAsync(text, cancellationToken).ConfigureAwait(false);
                        if (ok)
                        {
                            _validatedUrls.Add(text);
                            urlsValidated++;
                            string spaces = new string(' ', Console.WindowWidth - 1);
                            Console.Write($"\r {spaces}");
                            Console.Write($"\r  Valid PDF: {text}");
                        }
                        else
                        {
                            _failedUrls.Add(text);
                            urlsFailed++;
                            Console.WriteLine();
                            Console.WriteLine($"  Invalid PDF: {text}");
                        }
                    }
                }
            }

            return new ValidationSummary(layersProcessed, urlsChecked, urlsValidated, urlsSkippedCached, urlsSkippedFailedCached, urlsFailed);
        }

        private static bool IsHttpPdfUrl(string value)
        {
            if (!value.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return value.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<bool> DownloadAndValidatePdfAsync(string url, CancellationToken cancellationToken)
        {
            string? tempFile = null;
            try
            {
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                tempFile = Path.GetTempFileName();
                await using (var fs = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
                }

                var fileInfo = new FileInfo(tempFile);
                if (!fileInfo.Exists || fileInfo.Length < 4)
                {
                    return false;
                }

                var header = new byte[4];
                await using (var read = File.Open(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var readCount = await read.ReadAsync(header, 0, 4, cancellationToken).ConfigureAwait(false);
                    if (readCount < 4)
                    {
                        return false;
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    if (header[i] != PdfSignature[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempFile))
                {
                    try { File.Delete(tempFile); } catch { /* ignore */ }
                }
            }
        }
    }

    internal sealed record ValidationSummary(int LayersProcessed, int UrlsChecked, int UrlsValidated, int UrlsSkippedCached, int UrlsSkippedFailedCached, int UrlsFailed);
}


