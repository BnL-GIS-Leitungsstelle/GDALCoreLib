namespace GdalToolsLib.Common;

public static class OgcConstants
{
    public const string OptionOverwriteYes = "OVERWRITE=YES";
    public const string OptionOverwriteNo = "OVERWRITE=NO";

    public const string OptionCreateShapeAreaAndLengthFieldsYes = "CREATE_SHAPE_AREA_AND_LENGTH_FIELDS=YES";
    public const string OptionCreateShapeAreaAndLengthFieldsNo = "CREATE_SHAPE_AREA_AND_LENGTH_FIELDS=NO";

    public const string OptionDocumentationPrefix = "DOCUMENTATION=";

    public const string GpkgSqlDialect = "SQLITE3";
    public const string? OgrSqlDialect = "";
    public const string SQLiteSqlDialect = "SQLITE";
    public const string IndirectSQLiteSqlDialect = "INDIRECT_SQLITE";
}