using System;
using System.Collections.Generic;
using System.IO;

namespace GdalToolsTest.Helper;

/// <summary>
/// Class to cleanup all created testdata after running all tests in a test-class.
/// Must be added to a testclass as interface : IClassFixture<LayerTestFixture>
/// and parameter in constructur with a private class variable.
///
/// The interface IClassFixture<T>- is udes to define a setup-Logic for all tests of the whole testclass
///   to nint data, to re-use data between testcases and to remove temp data at he end of all tests.
///   The fixture-calls defines the logic that is executed.
/// </summary>
public class LayerTestFixture : IDisposable
{
    public List<string> PathesToCleanup { get; private set; } = new();


    public LayerTestFixture()
    {
        string testDataFolder = TestDataPathProvider.GetTestDataFolder(TestDataPathProvider.TestDataFolderVector);

        var subfolders = new List<string>() {"copiedLayerToFgdb", "copiedLayerToGpkg", "copiedLayerToShp"};

        SetPathToCleanup(testDataFolder, subfolders);
    }


    private void SetPathToCleanup(string testDataFolder, List<string> subfolders)
    {
        foreach (var subfolder in subfolders)
        {
            PathesToCleanup.Add(Path.Combine(testDataFolder, subfolder));
        }
    }

    /// <summary>
    /// delete all created geo-data together with the created-folder
    /// </summary>
    public void Dispose()
    {
        foreach (var path in PathesToCleanup)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path,true);
            }
        }
    }
}





