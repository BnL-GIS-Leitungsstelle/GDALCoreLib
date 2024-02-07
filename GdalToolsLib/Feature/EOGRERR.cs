namespace GdalToolsLib.Feature;

/// <summary>
/// Error codes by https://gdal.org/doxygen/ogr__core_8h.html

/// not yet found in GDAL - API
/// </summary>
public enum EOGRERR : int
{
    // Success
    OGRERR_NONE = 0,

    // Not enough data to deserialize.
    OGRERR_NOT_ENOUGH_DATA = 1,

    // Not enough memory
    OGRERR_NOT_ENOUGH_MEMORY = 2,

    // Unsupported geometry type.
    OGRERR_UNSUPPORTED_GEOMETRY_TYPE = 3,

    // Unsupported operation.
    OGRERR_UNSUPPORTED_OPERATION = 4,

    // Corrupt data.
    OGRERR_CORRUPT_DATA = 5,

    // Failure.
    OGRERR_FAILURE = 6,

    // Unsupported SRS.
    OGRERR_UNSUPPORTED_SRS = 7,

    // Invalid handle.
    OGRERR_INVALID_HANDLE = 8,

    // Non existing feature
    OGRERR_NON_EXISTING_FEATURE = 9
        
}