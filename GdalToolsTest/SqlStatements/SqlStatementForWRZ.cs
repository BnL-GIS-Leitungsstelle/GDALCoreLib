namespace GdalCoreTest.SqlStatements;

public class SqlStatementForWRZ
{
    public string LayerName { get; set; }
    public string CommandOrParameterToTest { get; set; }

    public string SqlPhrase { get; set; }

    public SqlStatementForWRZ(string layerName, string commandOrParameterToTest, string sqlPhrase)
    {
        LayerName = layerName;
        CommandOrParameterToTest = commandOrParameterToTest;
        SqlPhrase = sqlPhrase;
    }
}

