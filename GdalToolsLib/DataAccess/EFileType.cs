namespace GdalToolsLib.DataAccess;

public enum EFileType
{
    /// <summary>
    /// data source consist of one file, eg. gpkg
    /// </summary>
    File,

    /// <summary>
    /// data source consist of many files with same name, but different extensions. eg. a single shp
    /// </summary>
    MultiFile,
        
    /// <summary>
    /// data source consist of a folder, that contains files belonging to a file-based database, eg. fgdb or a folder of shapes
    /// </summary>
    Folder,

}