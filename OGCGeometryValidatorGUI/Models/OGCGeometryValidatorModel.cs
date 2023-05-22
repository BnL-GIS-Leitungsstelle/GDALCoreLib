using OGCToolsNetCoreLib;
using OGCToolsNetCoreLib.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OGCToolsNetCoreLib.DataAccess;
using OGCToolsNetCoreLib.Feature;
using OSGeo.OGR;

namespace OGCGeometryValidatorGUI.Models
{
    /// <summary>
    /// domain logic here
    /// </summary>
    public class OGCGeometryValidatorModel
    {
        private List<LayerValidationResult> _layerErrorList;

        private List<string> _fileList;


        public AppInfoModel AppInfo { get; set; }


        public OGCGeometryValidatorModel()
        {
            _layerErrorList = new List<LayerValidationResult>();
            _fileList = new List<string>();

            AppInfo = new AppInfoModel();   
        }


        public async Task ValidateFilesAsnyc(IProgress<ProgressModel> progress, CancellationToken cancellationToken)
        {
            int counter = 0;

            foreach (var fileName in _fileList)
            {
                var progressModel = new ProgressModel();
                progressModel.Percentage = ++counter * 100 / _fileList.Count;

                await Task.Run(async () =>
                {
                    using var dataSource = new GeoDataSourceAccessor().OpenDatasource(fileName);
                    var layerNames = dataSource.GetLayerNames();

                    foreach (var layerName in layerNames)
                    {
                        using var layer = dataSource.OpenLayer(layerName);
                        var layerValidationResult = await layer.ValidateGeometryAsync();
                        progressModel.ValidationResult = layerValidationResult; 
                        progress.Report(progressModel);
                        _layerErrorList.Add(layerValidationResult);
                    }
                });
            }

        }



        private void ReportErrors()
        {
            foreach (var layerError in _layerErrorList)
            {
                int errorCnt = layerError.InvalidFeatures.Count; // Sum errors

                //_log.LogInformation("");
                //_log.LogInformation("Validate file : {filename}", layerError.FileName);
                //_log.LogInformation("        Layer : {layername} has {errors} Geometry-errors.", layerError.LayerName, errorCnt);

                // group by type
                if (errorCnt > 0)
                {
                    var keyGroupList = layerError.InvalidFeatures.GroupBy(x => x.ValidationResultType).ToList();

                    foreach (var keyList in keyGroupList)
                    {
                        // _log.LogInformation("        Type : {key} in {errors} Geometries.", keyList.Key.GetEnumDescription(typeof(EGeometryValidationType)), keyList.Count());

                        foreach (var validationResult in keyList)
                        {
                            if (validationResult.ErrorLevel == EFeatureErrorLevel.Error)
                            {
                                //_log.LogError(validationResult.ToString());
                            }
                            if (validationResult.ErrorLevel == EFeatureErrorLevel.Warning)
                            {
                                //_log.LogWarning(validationResult.ToString());
                            }
                        }
                    }
                }
            }
        }

        private async Task ValidateGeodataFile(string fileName, ProgressModel progressModel)
        {
            using var dataSource = new GeoDataSourceAccessor().OpenDatasource(fileName);

            var layerNames = dataSource.GetLayerNames();

            foreach (var layerName in layerNames)
            {
                using var layer = dataSource.OpenLayer(layerName);
                _layerErrorList.Add(await layer.ValidateGeometryAsync());
            }
        }

        public int GetSupportedVectorDataFilesInDirectory(string startPath)
        {
            foreach (var fileItem in new GeoDataSourceAccessor().GetSupportedVectorData(startPath))
            {
                _fileList.Add(fileItem[0] as string);
            }

            return _fileList.Count;
        }

    }
}
