using System.Collections.Generic;

namespace GdalToolsLib.GeoProcessor;

public static class GeoProcessingOptions
{

    /// <summary>
    /// builds the processing options for layer operator UNION
    ///
    /// SKIP_FAILURES=YES/NO.Set it to YES to go on, even when a feature could not be inserted or a GEOS call failed.
    ///    PROMOTE_TO_MULTI=YES/NO.Set it to YES to convert Polygons into MultiPolygons, or LineStrings to MultiLineStrings.
    ///   INPUT_PREFIX=string. Set a prefix for the field names that will be created from the fields of the input layer.
    ///   METHOD_PREFIX= string.Set a prefix for the field names that will be created from the fields of the method layer.
    ///    USE_PREPARED_GEOMETRIES= YES / NO.Set to NO to not use prepared geometries to pretest intersection of features of method layer with features of this layer.
    ///  KEEP_LOWER_DIMENSION_GEOMETRIES= YES / NO.Set to NO to skip result features with lower dimension geometry that would otherwise be added to the result layer.The default is to add but only if the result layer has an unknown geometry type.
    /// </summary>
    /// <param name="inputPrefix"></param>
    /// <param name="methodPrefix"></param>
    /// <param name="skipFailures"></param>
    /// <param name="promoteToMulti"></param>
    /// <param name="usePreparedGeometries"></param>
    /// <param name="keepLowerDimensionGeometries"></param>
    /// <returns></returns>
    public static List<string> BuildUnionOptions(string inputPrefix = "inLayer", string methodPrefix = "methodLayer", bool skipFailures = true, bool promoteToMulti = true, bool usePreparedGeometries = false, bool keepLowerDimensionGeometries = false)
    {
        List<string> options = new();
        options.Add($"SKIP_FAILURES={GetBoolAsString(skipFailures)}");
        options.Add($"PROMOTE_TO_MULTI={GetBoolAsString(promoteToMulti)}");
        options.Add($"USE_PREPARED_GEOMETRIES={GetBoolAsString(usePreparedGeometries)}");
        options.Add($"KEEP_LOWER_DIMENSION_GEOMETRIES={GetBoolAsString(keepLowerDimensionGeometries)}");

        options.Add($"INPUT_PREFIX={inputPrefix}");
        options.Add($"METHOD_PREFIX={methodPrefix}");

        return options;
    }


    private static string GetBoolAsString(bool value)
    {
        return value ? "YES" : "NO";
    }

}