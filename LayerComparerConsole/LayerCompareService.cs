using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Feature;
using GdalToolsLib.Layer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace LayerComparerConsole;

public class LayerCompareService : ILayerCompareService
{
    private readonly ILogger<LayerCompareService> _log;
    private readonly IConfiguration _config;

    private string _file1;
    private string _layer1;
    private string _file2;
    private string _layer2;

    //    private readonly List<LayerComparisonResult> _layerCompareResultList;




    /// <summary>
    /// Information about the tool
    /// </summary>
    public IEnumerable<string> About
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            var lines = new List<string>
            {
                $"{fvi.ProductName} by {fvi.CompanyName}",
                $"Version: {fvi.FileVersion} ({fvi.LegalCopyright})",
                "",
                "OGR/GDAL-based tool to compare two layers that are expected to be similar.",
                "The comparison includes: ",
                " 1. Layer-comparison: number of records, geometry-type, layer-type, projection, extent",
                " 2. Schema-comparison (excl. oid, Shape_length, Shape_area)",
                " 3. Feature-comparison: attribute-values, geometry",
                "",
                @"Log-files at:'C:\temp\LayerCompareConsole.[log|json]' ",
                "","",
            };
            return lines;

        }
    }

    /// <summary>
    /// Information on usage
    /// </summary>
    public IEnumerable<string> Usage
    {
        get
        {
            var lines = new List<string>
            {
                "",
                "---------------- USAGE ---------------------------------------------------------",
                "use LayerComparer with 4 required parameters: ",
                string.Format(" /file1= master geo-database (Format: FGDB, GPKG, SHP)"),
                string.Format(" /layer1= name of layer"),
                string.Format(" /file2= candidate geo-database (Format: FGDB, GPKG, SHP)"),
                string.Format(" /layer2= name of layer"),

                string.Format(@" e.g. /file1=C:\Data\Tools\data.gdb /layer1=\Auengebiete ..."),
                "",
                "--------------------------------------------------------------------------------",
                ""
            };
            return lines;
        }
    }


    public LayerCompareService(ILogger<LayerCompareService> log, IConfiguration config)
    {
        _log = log;
        _config = config;
    }


    public void Compare(string file1, string layer1, string file2, string layer2)
    {
        _file1= file1;
        _layer1= layer1;
        _file2= file2;
        _layer2= layer2;

        for (int i = 0; i < _config.GetValue<int>("LoopTimes"); i++)
        {
            // log the numbers
            _log.LogWarning("Compare number {runNumber}", i);  // structured logger stores var-ame and value extra
        }

        CompareLayer();
    }


    private void ReadArgs(string[] args)
    {
        if (args.Length == 0)
        {
            ShowUsage();
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            Environment.Exit(0);
        }


    }


    private void CompareLayer()
    {
        _log.LogInformation("Compare MASTER file {file}, layer {layer}, ", _file1, _layer1);  // structured logger stores var-name and value extra
        _log.LogInformation("with CANDIDATE file {file}, layer {layer}, ", _file2, _layer2);

        using var masterDataSource = new GeoDataSourceAccessor().OpenDatasource(_file1);
        using var candidateDataSource = new GeoDataSourceAccessor().OpenDatasource(_file2);

        using var masterLayer = masterDataSource.OpenLayer(_layer1);
        using var candidateLayer = candidateDataSource.OpenLayer(_layer2);

        var masterLayerInfo = masterLayer.LayerDetails;
        var candidateLayerInfo = candidateLayer.LayerDetails;

        masterLayerInfo.Schema.RemoveStandardShape_Area_and_Shape_Length_FieldsIfPresent();
        candidateLayerInfo.Schema.RemoveStandardShape_Area_and_Shape_Length_FieldsIfPresent();

        _log.LogInformation(" --  start layer details comparison");
        var layerDetailsComparer = new LayerDetailComparer(masterLayerInfo, candidateLayerInfo);
        layerDetailsComparer.Run();
        ReportLayerDetailsComparisonResults(layerDetailsComparer.DetailDifferences);



        _log.LogInformation(" --  start layer features comparison");
        var layerFeaturesComparer = new LayerFeaturesComparer(masterLayerInfo, candidateLayerInfo,"ObjNummer");
        layerFeaturesComparer.RunCompareAttributeValues();
        ReportLayerFeatureAttributeValuesComparisonResults(layerFeaturesComparer.DifferenceFeatureList);

        layerFeaturesComparer.RunCompareGeometries();
        ReportLayerFeatureGeometryComparisonResults(layerFeaturesComparer.DifferenceFeatureList);

    }

    private void ReportLayerDetailsComparisonResults(List<LayerComparisonResult> comparisonResults)
    {
        foreach (var item in comparisonResults)
        {
            _log.LogWarning(" --  {item}", item);
        }
    }

    private void ReportLayerFeatureAttributeValuesComparisonResults(List<FeatureComparisonResult> comparisonResults)
    {
        int cntFields = 0;
        foreach (var item in comparisonResults)
        {
            cntFields += item.DifferentFieldValueList.Count;
        }
        _log.LogWarning("Found differences in {cnt} features, in {cntFields} fields", comparisonResults.Count, cntFields);

        foreach (var item in comparisonResults)
        {
            _log.LogWarning(" --  {item}", item);
            foreach (var fieldComparison in item.DifferentFieldValueList)
            {
                _log.LogWarning("   --  {fieldComparison}", fieldComparison);
            }
        }

        comparisonResults.Clear();
    }

    private void ReportLayerFeatureGeometryComparisonResults(List<FeatureComparisonResult> comparisonResults)
    {

        _log.LogWarning("Found different geometries in {cnt} features", comparisonResults.Count);

        foreach (var item in comparisonResults)
        {
            _log.LogWarning(" --  {item}", item);
            foreach (var fieldComparison in item.DifferentFieldValueList)
            {
                _log.LogWarning("   --  {fieldComparison}", fieldComparison.ShowGeometricDifference());
            }
        }

        comparisonResults.Clear();
    }


    public void ShowAbout()
    {
        foreach (var line in About) _log.LogInformation(line);
    }

    private void ShowUsage()
    {
        foreach (var line in Usage) _log.LogInformation(line);
    }

}