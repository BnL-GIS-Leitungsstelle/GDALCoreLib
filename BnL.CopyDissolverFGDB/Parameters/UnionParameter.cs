using GdalToolsLib.Layer;

namespace BnL.CopyDissolverFGDB.Parameters
{
    public class UnionParameter
    {
        public string ResultLayerName { get; }
        public int Year { get; }
        public string LegalState { get; }
        public string Theme { get; }

        public UnionParameter(string[] line)
        {
            ResultLayerName = line[0];
            Year = int.Parse(line[1]);
            LegalState = line[2];
            Theme = line[3];
        }
    }
}
