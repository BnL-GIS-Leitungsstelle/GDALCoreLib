using System;
using System.Collections.Generic;
using System.IO;

namespace CreateOptimizedGeopackage.Utils;

internal static class FolderUtils
{
    public static IEnumerable<string> GetFilesFromFolder(string folder, string extension)
    {
            return Directory.GetFiles(folder, $"*{extension}");
        }

    public static void CreateDirectoryIfNotExists(string directory)
    {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

    public static void DeleteDirectory(string directory, bool recursive = true)
    {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, recursive);
                }
            }
            catch (Exception) { }
        }
}