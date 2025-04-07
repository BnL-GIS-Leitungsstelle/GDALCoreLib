namespace BnL.CopyDissolverFGDB.Parameters;

public class FilterParameter
{
    public string Theme { get; private set; }
    public int Year { get; private set; }
    public string WhereClause { get; private set; }

    public FilterParameter(string[] line)
    {
        Theme = line[0];
        Year = int.Parse(line[1]);
        WhereClause = line[2];
    }
}

