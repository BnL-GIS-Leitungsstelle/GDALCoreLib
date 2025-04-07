using OSGeo.OGR;
using System.Collections.Generic;
using GdalToolsLib.Common;
using System.Linq;

namespace GdalToolsLib.VectorTranslate
{
    /// <summary>
    /// An easier interface for the VectorTranslateOptions specified under <see href="https://gdal.org/en/stable/programs/ogr2ogr.html"/>
    /// Common options are made configurable through properties, instead of just raw strings. 
    /// If you do want to pass raw string options, you can do that using the <see cref="OtherOptions"/> property.
    /// 
    /// Use the <see cref="ToStringArray"/> method to translate the options object back into a list of string arguments GDAL can understand. 
    /// </summary>
    public class VectorTranslateOptions
    {
        public string? Where { get; init; }
        public bool Overwrite { get; init; }
        public bool Update { get; init; }
        public string? SourceLayerName { get; init; }
        public string? NewLayerName { get; init; }
        public wkbGeometryType? NewGeometryType { get; init; }
        public string? Sql { get; init; }
        public string[]? OtherOptions { get; init; }
        public bool MakeValid { get; init; }
        public (string name, object value)[]? LayerCreationOptions { get; init; }
        public bool SkipFailures { get; init; }

        public string[] ToStringArray()
        {
            var options = new List<string>();
            if (SourceLayerName != null) options.Add(SourceLayerName);
            if (Where != null) options.AddRange(["-where", Where]);
            if (Sql != null) options.AddRange(["-sql", Sql]);
            if (NewLayerName != null) options.AddRange(["-nln", NewLayerName]);
            if (NewGeometryType != null) options.AddRange(["-nlt", NewGeometryType.Value.ToStringName()]);
            if (Overwrite) options.Add("-overwrite");
            if (Update) options.Add("-update");
            if (MakeValid) options.Add("-makevalid");
            if (SkipFailures) options.Add("-skipfailures");

            if (LayerCreationOptions != null)
                options.AddRange(LayerCreationOptions.SelectMany(lco => new[]
                    { "-lco", $"{lco.name}={lco.value}" }));
            if (OtherOptions != null) options.AddRange(OtherOptions);
            return options.ToArray();
        }
    }
}