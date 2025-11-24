using System;
using System.IO;

namespace BnL.TextFieldUrlContentValidator.Configuration
{
    internal sealed class ToolOptions
    {
        public string? StartDirectory { get; set; }

        public string GetValidatedStartDirectory()
        {
            if (string.IsNullOrWhiteSpace(StartDirectory))
            {
                throw new InvalidOperationException("StartDirectory must be provided.");
            }

            if (Path.Exists(StartDirectory) == false)
            {
                throw new InvalidOperationException($"StartDirectory {StartDirectory} not found.");
            }

            return StartDirectory;
        }
    }
}

