using System;

namespace BnL.GDBSubstringReplacerInTextfields.Configuration
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

            return StartDirectory;
        }
    }
}

