namespace BnL.CopyDissolverFGDB.Parameters;

public class FilterParameter : LayerParameter
{
    public string WhereClause { get; private set; }

    public FilterParameter(string theme, string year, string legalState, string filter) : base(theme, year, legalState)
    {
        WhereClause = filter;
    }

    public FilterParameter(string[] line) : this(line[0], line[1], "N.N.", line[2]) { }

    public override string ToString()
    {
        return $"{Year} {Theme,20}, {WhereClause} ";
    }
}

