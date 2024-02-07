namespace OGCToolsNetCoreLib.Feature;


/// <summary>
/// https://gdal.org/development/rfc/rfc53_ogr_notnull_default.html
/// substitute: nFlag not yet found in GDAL
/// </summary>
public enum EOGR_F_VAL: int
{
    // Validate that fields respect not-null constraints.
    OGR_F_VAL_NULL = 1,

    // Validate that geometries respect geometry column type.
    OGR_F_VAL_GEOM_TYPE = 2,

    // Validate that (string) fields respect field width.
    OGR_F_VAL_WIDTH = 4,

    // Validate that (string) fields respect field width.
    OGR_F_VAL_ALLOW_NULL_WHEN_DEFAULT = 8,

    // Validate that (string) fields respect field width.
    OGR_F_VAL_ALL = -1
}