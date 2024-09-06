using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using BnL.CopyDissolverFGDB.Parameters;
using GdalToolsLib.DataAccess;
using GdalToolsLib.GeoProcessor;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using OSGeo.OGR;

namespace BnL.CopyDissolverFGDB;

/// <summary>
/// the overall use case
/// </summary>
public class PrepareOptimizedGeodataOfProtectedAreasForFurtherAnalysisUseCase
{
    /// <summary>
    /// Information about the tool
    /// </summary>
    public IEnumerable<string> About
    {
        get
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var companyName = fvi.CompanyName;
            var productName = fvi.ProductName;
            var productVersion = fvi.ProductVersion;

            var lines = new List<string>
            {
                $"{productName} Version: {productVersion} ",
                $"Author: {companyName}",
                "",
                "=================================================================================================",
                "OGR/GDAL-based tool to prepare the geodata of the federal inventories and protected sites towards statistical analysis.",
                "This means filtering, buffering and dissolving a selection of layers to have them ready for further analysis.",
                "This can be done by creating lists and charts that summarize areas and number of objects.",
                "=================================================================================================",
                ""
            };
            return lines;
        }
    }


    public List<string> SourcePaths { get; set; }
    public string TargetPath { get; set; }

    private ProcessParameter ProcessParameter { get; set; }


    /// <summary>
    /// lock object to synchronize access to console writeline
    /// </summary>
    private static Object m_Lock = new Object();

    public List<string> DissolveFields { get; set; }
    public WorkList Worklist { get; set; }

    // Depth of directory-tree to search for geodata
    private int _subFolderDepthLevel = 2;

    public PrepareOptimizedGeodataOfProtectedAreasForFurtherAnalysisUseCase(List<string> sourcePaths, string targetPath, List<string> dissolveFields)
    {
        SourcePaths = sourcePaths;
        TargetPath = targetPath;
        DissolveFields = dissolveFields;

        Worklist = new WorkList();

        ProcessParameter = new ProcessParameter();
    }


    public void ShowAbout()
    {
        foreach (var line in About)
        {
            System.Console.WriteLine();
            Console.WriteLine(line);
        }
    }

    public void Run()
    {
        Console.WriteLine("Copy dissolver has started.");

        // Load Filters
        ProcessParameter.LoadFilterParameter("../../../filters.txt");
        ProcessParameter.ShowFilters();

        // Load Buffers
        ProcessParameter.LoadBufferParameter("../../../buffers.txt");
        ProcessParameter.ShowBuffers();

        // Load Union
        ProcessParameter.LoadUnionParameter("../../../unions.txt");
        ProcessParameter.ShowUnions();


        AddLayersToWorkList(CollectGeodataFiles(SourcePaths, EDataSourceType.OpenFGDB));

        // Report invalid data and Report (Geometry , Dissolve Fields) issues that influence the usability of the result of the whole process
        ReportLayerWithoutRequiredDissolveFieldsMissesGeometryValidation();

        // to let the origins untouched
        CopyValidFgdbsIntoTargetFolder("Core");

        Worklist.Clear();

        AddLayersToWorkList(CollectGeodataFiles(new List<string>() { TargetPath }, EDataSourceType.OpenFGDB));

        CleanupLayersWithoutValidDissolveFields();

        Worklist.Clear();

        AddLayersToWorkList(CollectGeodataFiles(new List<string>() { TargetPath }, EDataSourceType.OpenFGDB));

        // Preprocessing before dissolving
        FilterAndBufferSomeLayers();

        Worklist.Clear();

        AddLayersToWorkList(CollectGeodataFiles(new List<string>() { TargetPath }, EDataSourceType.OpenFGDB));

        // to summarize data based on objNummer and Name
        DissolveLayers();

        Worklist.Clear();

        AddLayersToWorkList(CollectGeodataFiles(new List<string>() { TargetPath }, EDataSourceType.OpenFGDB));

        // Unify some (e.g.amphibien)
        UnifySomeLayers();

        Worklist.Clear();

        AddLayersToWorkList(CollectGeodataFiles(new List<string>() { TargetPath }, EDataSourceType.OpenFGDB));

        // Cleanup all non-dissolved or non-unified layers
        CleanupNonDissolvedAndNonUnifiedLayers();


        // for convenience only
        // ConvertBackTo FGDBs

    }

    private void CleanupNonDissolvedAndNonUnifiedLayers()
    {

        var finalLayers = Worklist.QueryWorkLayerToSelectFinalLayers(new List<string>() { "Dissolve", "Union" });

        var filesToExamine = finalLayers.Select(x => x.FileName).Distinct().ToList();
        var finalLayerNames = finalLayers.Select(x => x.LayerName).Distinct().ToList();

        foreach (var fileName in filesToExamine)
        {
            using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(fileName, EAccessLevel.Full))
            {
                foreach (var layerName in ds.GetLayerNames(ELayerType.AllGeometry))
                {
                    if (finalLayerNames.Contains(layerName))
                        continue;

                    var success = ds.DeleteLayer(layerName);

                    if (success == false)
                    {
                        throw new ApplicationException($"**error: Could not delete layer {layerName} from file {fileName}");
                    }
                }
            }
        }

    }

    /// <summary>
    /// filter out non-protected areas, z.B. Kartiereinheiten ohne Schutzstatus, Schadensperimeter..
    /// TODO:Park -Kernzone
    /// </summary>
    private void FilterAndBufferSomeLayers()
    {
        foreach (var filter in ProcessParameter.Filters)
        {
            var layerToFilter = Worklist.QueryWorkLayerFilterForProtectedArea(Convert.ToInt32(filter.Year), filter.Theme);

            if (layerToFilter.Count == 0)
            {
                Console.WriteLine($"*** error: missing layer to filter: Year={filter.Year}, LayerCategory={filter.Theme}.");
                //UserConfirmation();
                continue;
            }

            foreach (var workLayer in layerToFilter)
            {
                using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(workLayer.FileName, EAccessLevel.Full))
                {
                    using (var layer = ds.OpenLayer(workLayer.LayerName))  // open layer
                    {
                        var filteredRecordNumber = layer.FilterByAttributeOnlyRespectedInNextFeatureLoop(filter.WhereClause);

                        var copyRecordNumber = layer.CopyToLayer(ds, $"{workLayer.LayerName}Filtered");

                        if (filteredRecordNumber != copyRecordNumber)
                        {
                            Console.WriteLine($"*** error: software fails: {filteredRecordNumber} filtered records vs. {copyRecordNumber} copied records.");
                            UserConfirmation();
                        }

                        Console.WriteLine($" -- > Filter protected areas in {workLayer.LayerName}");
                        //TODO: überprüfen mit altem Ergebnis

                        ds.RenameLayerOpenFgdb(workLayer.LayerName, $"{workLayer.LayerName}ToBeDeleted");
                    }
                }

                using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(workLayer.FileName, EAccessLevel.Full))
                {
                    if (ds.HasLayer($"{workLayer.LayerName}Filtered"))
                    {
                        var newLayerName = workLayer.LayerName;
                        if (workLayer.LayerName.Contains("_Park_"))
                        {
                            newLayerName = workLayer.LayerName.Replace("_Park_", "_ParkKernzone_");
                        }

                        ds.RenameLayerOpenFgdb($"{workLayer.LayerName}Filtered", newLayerName);
                    }
                }
            }
        }

        foreach (var buffer in ProcessParameter.Buffers)
        {
            var layerToBuffer = Worklist.QueryWorkLayerToBuffer(buffer.LegalState, buffer.Theme);

            if (layerToBuffer.Count == 0)
            {
                Console.WriteLine($"*** error: missing layer to buffer: Legal state={buffer.LegalState}, LayerCategory={buffer.Theme}.");
                // UserConfirmation();
            }
            foreach (var workLayer in layerToBuffer)
            {
                using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(workLayer.FileName, EAccessLevel.Full))
                {
                    using var layer = ds.OpenLayer(workLayer.LayerName);  // open layer

                    if (layer.LayerDetails.GeomType != wkbGeometryType.wkbMultiPoint &&
                        layer.LayerDetails.GeomType != wkbGeometryType.wkbPoint)
                    {
                        Console.WriteLine($"*** error: non-point layer in buffer process: {workLayer.LayerName}.");
                        UserConfirmation();
                    }

                    Console.WriteLine($" -- > Buffer protected areas in {workLayer.LayerName}");

                    layer.BufferToLayer(ds, buffer.BufferDistanceMeter);

                    ds.RenameLayerOpenFgdb(workLayer.LayerName, $"{workLayer.LayerName}ToBeDeleted");
                }

                using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(workLayer.FileName, EAccessLevel.Full))
                {
                    if (ds.HasLayer($"{workLayer.LayerName}Buffer"))
                    {
                        ds.RenameLayerOpenFgdb($"{workLayer.LayerName}Buffer", workLayer.LayerName);
                    }
                }
            }
        }
    }

    /// <summary>
    /// unify more or less identical area into one. eg. coming from cantons and "Pro Natura"
    /// </summary>
    private void UnifySomeLayers()
    {
        foreach (var union in ProcessParameter.Unions)
        {
            var layerToUnion = Worklist.QueryWorkLayerToUnion(union.LayerParameters);

            if (layerToUnion.Count < 2)
            {
                Console.WriteLine($"*** error: missing layers to union: Year={union.LayerParameters[0].Year}, Legal State: {union.LayerParameters[0].LegalState} Theme={union.LayerParameters[0].Theme}.");
                UserConfirmation();
                continue;
            }


            using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(layerToUnion[0].FileName, EAccessLevel.Full))
            {
                var unionGroup = new UnionGroup(union.ResultLayerName, layerToUnion);

                foreach (var item in unionGroup.Items)
                {
                    using var layer = ds.OpenLayer(item.LayerName);  // open layer

                    using var otherLayer = ds.OpenLayer(item.LayerNameOther);

                    Console.Write($" -- > Unify areas from {item.LayerName} and {item.LayerNameOther} ");

                    var outputLayerName = layer.GeoProcessWithLayer(EGeoProcess.Union, otherLayer, item.ResultLayerName);

                    Console.WriteLine($" into {outputLayerName}.");
                }
            }
        }
    }


    private void DissolveLayers()
    {
        var gdbs = Worklist.QueryWorkLayerToDissolve().Select(x => x.FileName).Distinct().ToList();

        //foreach (var gdpPath in gdbs)
        //{
        //    var layerNames = Worklist.QueryWorkLayerToDissolve().Where(x => x.FileName == gdpPath).Select(x => x.LayerName).ToList();

        //    DissolveLayersPerGdb(gdpPath, layerNames);
        //}

        Parallel.ForEach(gdbs, gdpPath =>
        {
            var layerNames = Worklist.QueryWorkLayerToDissolve().Where(x => x.FileName == gdpPath).Select(x => x.LayerName).ToList();

            DissolveLayersPerGdb(gdpPath, layerNames);
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gdbPath"></param>
    /// <param name="layerNames"></param>
    private void DissolveLayersPerGdb(string gdbPath, List<string> layerNames)
    {
        using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(gdbPath, EAccessLevel.Full);

        foreach (var layerName in layerNames)
        {
            using var layer = ds.OpenLayer(layerName);

            WriteLine($" -- > Dissolve layer {layerName}.");

            List<FieldDefnInfo> dissolveFieldList = layer.LayerDetails.Schema.FieldList.Where(x => DissolveFields.Contains(x.Name)).ToList();

            layer.DissolveToLayer(layer.DataSource, dissolveFieldList);
        }
    }

    private void CleanupLayersWithoutValidDissolveFields()
    {
        var gdbsWithLayersToRemove = Worklist.WorkLayers
            .Where(w => w.WorkState != EWorkState.ValidDissolveFields)
            .Select(x => x.FileName)
            .Distinct();

        foreach (var gdbFileName in gdbsWithLayersToRemove)
        {
            var layersToRemove = Worklist.WorkLayers
                .Where(w => w.FileName == gdbFileName && w.WorkState != EWorkState.ValidDissolveFields)
                .Select(w => w.LayerName)
                .Distinct()
                .ToArray();

            Console.WriteLine($"Remove {layersToRemove.Length} Layers with missing dissolve fields from GDB {gdbFileName}.");

            using var gdb = new OgctDataSourceAccessor().OpenOrCreateDatasource(gdbFileName, EAccessLevel.Full);
            foreach (var layer in layersToRemove) 
            {
                var success = gdb.DeleteLayer(layer);
                
                if (!success)
                {
                    throw new ApplicationException($"**error: Could not delete layer {layer} from file {gdbFileName}");
                }
            }
        }
    }

    public static void WriteLine(String str, Boolean line = true)
    {
        lock (m_Lock)
        {
            if (line)
            {
                Console.WriteLine(str);
            }
            else
            {
                Console.Write(str);
            }
        }
    }

    /// <summary>
    /// report invalid layers to the user
    /// </summary>
    private void ReportLayerWithoutRequiredDissolveFieldsMissesGeometryValidation()
    {
        var workLayersInvalid = Worklist.WorkLayers.Where(w => w.WorkState == EWorkState.MissingDissolveFields).ToList();

        if (workLayersInvalid.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"--> check {workLayersInvalid.Count} layers manually for missing dissolve fields:");

            foreach (var workLayer in workLayersInvalid)
            {
                Console.WriteLine($"*** error: missing some dissolve-fields in {workLayer.LayerName} of {workLayer.FileName} ");
            }
            UserConfirmation();
        }
    }

    /// <summary>
    /// Copy valid FGDBs with into target - folder
    /// </summary>
    private void CopyValidFgdbsIntoTargetFolder(string gdbNameAppendix = "")
    {
        // continue with valid data
        var gdbList = Worklist.WorkLayers.Where(w => w.WorkState == EWorkState.ValidDissolveFields).Select(x => x.FileName).Distinct()
            .ToList();
        Console.WriteLine($"{gdbList.Count} valid FGDBs have {Worklist.WorkLayers.Count} valid layers.\n");


        if (Directory.Exists(TargetPath)) DeleteDirectory(TargetPath);
        Directory.CreateDirectory(TargetPath);

        Console.WriteLine($"Copy {gdbList.Count} valid FGDBs to target folder.\n");
        foreach (var gdbFile in gdbList)
        {
            Console.WriteLine($" -- Copy {gdbFile}.");

            var outputFileName = Path.GetFileName(gdbFile);
            outputFileName = outputFileName.Replace(".gdb", $"{gdbNameAppendix}.gdb");

            // Targetpath and last subdirectory of gdb
            string outputdirectory = Path.Combine(TargetPath, new DirectoryInfo(Path.GetDirectoryName(gdbFile)).Name);

            if (Directory.Exists(outputdirectory) == false && outputdirectory.EndsWith(".gdb") == false) Directory.CreateDirectory(outputdirectory);

            new OgctDataSourceAccessor().CopyDatasource(gdbFile, outputdirectory, outputFileName);
        }
    }



    /// <summary>
    /// Collects fgdb-files from a starting directory
    /// </summary>
    /// <param name="pathes"></param>
    /// <param name="datasoureType"></param>
    /// <returns>list of gdbs</returns>
    private List<string> CollectGeodataFiles(List<string> pathes, EDataSourceType datasoureType)
    {
        var fileList = new List<string>();

        foreach (var path in pathes)
        {
            int startLevel = path.Split('\\').Length;
            int targetLevel = startLevel + _subFolderDepthLevel;

            var supportedDataSource = SupportedDatasource.GetSupportedDatasource(datasoureType);

            if (supportedDataSource.FileType == EFileType.Folder)
            {
                fileList.AddRange(Directory.GetDirectories(path, "*" + supportedDataSource.Extension, SearchOption.AllDirectories)
                    .Where(folder => folder.Split('\\').Length <= targetLevel).ToList());
            }
            if (supportedDataSource.FileType == EFileType.File)
            {
                fileList.AddRange(Directory.GetFiles(path, "*" + supportedDataSource.Extension, SearchOption.AllDirectories)
                    .Where(folder => folder.Split('\\').Length <= targetLevel).ToList());
            }
        }

        Console.WriteLine($"Found {fileList.Count} {datasoureType} in {SourcePaths.Count} source folders.\n");

        return fileList;
    }

    /// <summary>
    /// reports layer without dissolve fields (excl. tables without geometry)
    /// and 
    /// </summary>
    /// <param name="inputGdbs"></param>
    /// <returns></returns>
    private void AddLayersToWorkList(List<string> inputGdbs)
    {
        foreach (string gdbPath in inputGdbs)
        {
            using var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(gdbPath);

            var layerNameList = ds.GetLayerNames();

            foreach (var layerName in layerNameList)
            {
                using var layer = ds.OpenLayer(layerName);

                int dissolveFieldsInLayer = layer.LayerDetails.Schema.FieldList.Count(x => DissolveFields.Contains(x.Name));

                if (dissolveFieldsInLayer < DissolveFields.Count)
                {
                    Worklist.AddLayer(new WorkLayer(layer.LayerDetails, EWorkState.MissingDissolveFields));
                    continue;
                }

                if (layer.LayerDetails.GeomType == wkbGeometryType.wkbNone)  // exclude tables
                {
                    Worklist.AddLayer(new WorkLayer(layer.LayerDetails, EWorkState.IsTableHasNoGeometry));
                    continue;
                }

                if (layer.Name.EndsWith("ToBeDeleted"))  // exclude temporary results
                {
                    Worklist.AddLayer(new WorkLayer(layer.LayerDetails, EWorkState.IsTemporaryResult));
                    continue;
                }

                if (layer.Name.EndsWith("Dissolve"))  // exclude temporary results
                {
                    Worklist.AddLayer(new WorkLayer(layer.LayerDetails, EWorkState.IsDissolved));
                    continue;
                }

                Worklist.AddLayer(new WorkLayer(layer.LayerDetails, EWorkState.ValidDissolveFields));
            }
        }
    }

    /// <summary>
    /// Deletes all files and subdirectories in given directory
    /// </summary>
    /// <param name="directoryToDelete"></param>
    private static void DeleteDirectory(string directoryToDelete)
    {
        string[] files = Directory.GetFiles(directoryToDelete);
        string[] dirs = Directory.GetDirectories(directoryToDelete);

        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            DeleteDirectory(dir);
        }

        Directory.Delete(directoryToDelete, false);
    }
    private static void UserConfirmation()
    {
        Console.WriteLine("Press any key to continue..");
        Console.ReadKey();
    }
}
