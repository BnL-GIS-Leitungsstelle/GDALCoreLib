using System;
using System.IO;
using GdalCoreTest.Helper;

namespace GdalToolsTest.Helper;

/// <summary>
/// Class to cleanup all created testdata after running all tests in a test-class.
/// Must be added to a testclass as interface : IClassFixture<DataAccessSourceFixture>
/// and parameter in constructur with a private class variable.
///
/// Die Nutzung des IClassFixture<T>-Interfaces in XUnit wird verwendet, um eine gemeinsame Setup-Logik
/// für alle Tests in einer Testklasse bereitzustellen.
///   Dies ist nützlich, um teure Initialisierungen oder Setups einmal auszuführen
///   und die Ergebnisse zwischen Tests wiederzuverwenden.
/// 
/// Eine Fixture-Klasse wird erstellt, um die Setup-Logik zu kapseln.
/// Diese Klasse kann Ressourcen initialisieren und bereinigen, die für alle Tests in der Testklasse verwendet werden.
/// </summary>
public class DataAccessSourceFixture : IDisposable
{
    public string PathToCleanup { get; private set; }


    public DataAccessSourceFixture()
    {
        string testDataFolder = TestDataPathProvider.GetTestDataFolder(TestDataPathProvider.TestDataFolderVector);

        SetPathToCleanup(testDataFolder);
    }


    private void SetPathToCleanup(string testDataFolder)
    {
        PathToCleanup = Path.Combine(testDataFolder, "created");
    }

    /// <summary>
    /// delete all created geo-data together with the created-folder
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(PathToCleanup))
        {
            Directory.Delete(PathToCleanup,true);
        }
    }
}





