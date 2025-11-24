using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace BnL.TextFieldUrlContentValidator.Configuration
{
    internal static class OptionsLoader
    {
        public static ToolOptions Load(string[] args)
        {
            var switchMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["--start"] = "UrlContentValidator:StartDirectory",
                ["-s"] = "UrlContentValidator:StartDirectory"
            };

            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables("URLCONTENTVALIDATOR_")
                .AddCommandLine(args, switchMappings)
                .Build();

            var options = new ToolOptions();
            configuration.GetSection("UrlContentValidator").Bind(options);

            options.StartDirectory = NormalizeDirectory(
                options.StartDirectory ?? configuration["StartDirectory"]);

            if (string.IsNullOrWhiteSpace(options.StartDirectory))
            {
                throw new InvalidOperationException("Configure UrlContentValidator:StartDirectory in appsettings.json or pass --start.");
            }

            if (!Directory.Exists(options.StartDirectory))
            {
                throw new DirectoryNotFoundException($"Configured start directory \"{options.StartDirectory}\" does not exist.");
            }

            return options;
        }

        private static string NormalizeDirectory(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            var expanded = Environment.ExpandEnvironmentVariables(path.Trim());
            expanded = expanded.Trim('"');

            return Path.GetFullPath(expanded);
        }
    }
}

