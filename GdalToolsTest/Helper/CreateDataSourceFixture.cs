using System;
using System.IO;

namespace GdalCoreTest.Helper;

/// <summary>
/// Class to cleanup all created testdata after running all tests in a test-class.
/// Must be added to a testclass as interface : IClassFixture<CreateDataSourceFixture>
/// and parameter in constructur with a private class variable.
/// </summary>
public class CreateDataSourceFixture : IDisposable
{
    public string PathToCleanup { get; private set; }
    public CreateDataSourceFixture()
    {
        string testDataFolder = TestDataPathProvider.GetTestDataFolder(TestDataPathProvider.TestDataFolderVector);

        PathToCleanup = Path.Combine(testDataFolder, "created");
    }

    /// <summary>
    /// delete all created geo-data together with the created-folder
    /// </summary>
    public void Dispose()
    {
        Directory.Delete(PathToCleanup,true);
    }


}

