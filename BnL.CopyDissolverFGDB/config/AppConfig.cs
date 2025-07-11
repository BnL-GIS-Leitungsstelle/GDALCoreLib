using System.Collections.Generic;

namespace BnL.CopyDissolverFGDB.config;

public class AppConfig
{
    public string WorkDir { get; set; } = string.Empty;
    public List<RenamePattern> RenamePatterns { get; set; } = [];
    public List<string> DissolveFieldNames { get; set; } = [];
    public List<string> SearchDirs { get; set; } = [];
}