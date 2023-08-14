using System;
using System.IO;

namespace GdalCoreTest.Helper;

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

