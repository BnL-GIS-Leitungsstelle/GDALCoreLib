namespace BnL.CopyDissolverFGDB.Parameters;

public class LayerParameter
{
    public string Theme { get; private set; }
    public string Year { get; private set; }

    public string LegalState { get; private set; }

    public LayerParameter(string theme, string year, string legalState)
    {
        Theme = theme;
        Year = year;
        LegalState = legalState;
    }

    public override string ToString()
    {
        return $"{Year} {Theme,20}, {LegalState,15} ";
    }
}

