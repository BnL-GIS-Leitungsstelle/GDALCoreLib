using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GdalToolsLib.DataAccess;
using GdalToolsLib.Extensions;
using GdalToolsLib.Feature;
using GdalToolsLib.Geometry;
using GdalToolsLib.Models;

namespace OGCGeometryValidatorCore;

public class GeometryValidatorService : IGeometryValidatorService
{
    private readonly ILogger<GeometryValidatorService> _log;
    private readonly IConfiguration _config;

    private string _startpath;

    private bool _attemptRepair;

    private readonly List<string> _fileList;

    private readonly List<LayerValidationResult> _layerErrorList;

    private readonly IParallelLayerGeometryValidator _parallelLayerGeometryValidator;

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
                "OGR/GDAL-based tool to validate geodata regarding OGC-Geometry-Error definitions",
                "(https://www.opengeospatial.org/standards/sfa)",
                "",
                @"Log-files at:'C:\temp\OGCGeometryValidatorCore.[log|json]' ",
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
                "use OGCGeometryValidator with 1 required parameter: ",
                string.Format(" /path= directory to validate geomtries of included geodata (Format: FGDB, GPKG, SHP)"),
                string.Format(@" e.g. /path=C:\Data\Tools"),
                "",
                "--------------------------------------------------------------------------------",
                ""
            };
            return lines;
        }
    }

    public GeometryValidatorService(ILogger<GeometryValidatorService> log, IConfiguration config, IParallelLayerGeometryValidator parallelLayerGeometryValidator)
    {
        _log = log;
        _config = config;
        _parallelLayerGeometryValidator = parallelLayerGeometryValidator;
        _parallelLayerGeometryValidator.GetLimit(_config.GetValue<int>("MinimumRecordsForParallelProcessing"));

        _fileList = new List<string>();
        _layerErrorList = new List<LayerValidationResult>();
    }


    public async Task Run(string[] args)
    {
        for (int i = 0; i < _config.GetValue<int>("LoopTimes"); i++)
        {
            // log the numbers
            _log.LogWarning("Run number {runNumber}", i);  // structured logger stores var-ame and value extra
        }

        ShowAbout();

        ReadArgs(args);


        await ValidateFiles(_attemptRepair);

        ReportValidationResult();
    }

    private async Task ValidateFiles(bool attemptRepair)
    {
        GetSupportedVectorDataFilesInDirectory();

        foreach (var filename in _fileList)
        {
            await ValidateFile(filename, attemptRepair);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="attemptRepair"></param>
    private async Task ValidateFile(string fileName, bool attemptRepair)
    {
        _log.LogInformation("Examine file {file}", fileName);  // structured logger stores var-name and value extra

        using var dataSource = new OgctDataSourceAccessor().OpenOrCreateDatasource(fileName, EAccessLevel.Full);
        var layerNames = dataSource.GetLayerNames();

        foreach (var layerName in layerNames)
        {
            using var layer = dataSource.OpenLayer(layerName);

            _log.LogInformation("--  Examine layer {layer}", layerName);

            Stopwatch timeForGeometryValidation;
            string msgAddition = "";

            timeForGeometryValidation = Stopwatch.StartNew();
            _log.LogInformation(" --  start geometry validation {layer}", layerName);

            _layerErrorList.Add(await layer.ValidateGeometryAsync(attemptRepair:attemptRepair));

            timeForGeometryValidation.Stop();
            _log.LogWarning(" --  finish {msgAddition} geometry validation for layer {layer} in {elapsedTime}", msgAddition, layerName,
                Utils.ToNicelyTimeFormatString(timeForGeometryValidation));
        }
    }





    private void ReportValidationResult()
    {
        foreach (var layerError in _layerErrorList)
        {
            int errorCnt = layerError.InvalidFeatures.Count; // Sum errors

            _log.LogInformation("");
            _log.LogInformation("Validate file : {filename}", layerError.FileName);
            _log.LogInformation("        Layer : {layername} has {errors} Geometry-errors.", layerError.LayerName, errorCnt);

            // group by type
            if (errorCnt > 0)
            {
                var keyGroupList = layerError.InvalidFeatures.GroupBy(x => x.ValidationResultType).ToList();

                foreach (var keyList in keyGroupList)
                {
                    _log.LogInformation("        Type : {key} in {errors} Geometries.", keyList.Key.GetEnumDescription(typeof(EGeometryValidationType)), keyList.Count());

                    foreach (var validationResult in keyList)
                    {
                        if (validationResult.ErrorLevel == EFeatureErrorLevel.Error)
                        {
                            _log.LogError(validationResult.ToString());
                        }
                        if (validationResult.ErrorLevel == EFeatureErrorLevel.Warning)
                        {
                            _log.LogWarning(validationResult.ToString());
                        }
                    }
                }
            }
        }
    }





    private void GetSupportedVectorDataFilesInDirectory()
    {
        foreach (var fileItem in new OgctDataSourceAccessor().GetPathNamesOfSupportedVectordataFormats(_startpath, true))
        {
            _fileList.Add(fileItem[0] as string);
        }
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

        foreach (string argument in args)
        {
            if (argument.StartsWith("/path="))
            {
                _startpath = argument.Remove(0, 6).Replace(@"\\", @"\");
            }
            if (argument.StartsWith("/attemptRepair="))
            {
                string boolString = argument.Remove(0, 15);
                _attemptRepair = boolString.ToLower().Contains("true") || boolString.ToLower().Contains("yes") || boolString.ToLower().Contains("ja");
            }
        }
    }

    private void ShowAbout()
    {
        foreach (var line in About) _log.LogInformation(line);
    }

    private void ShowUsage()
    {
        foreach (var line in Usage) _log.LogInformation(line);
    }

}