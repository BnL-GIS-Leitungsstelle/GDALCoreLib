using GdalToolsLib.Layer;

namespace BnL.CopyDissolverFGDB.Parameters
{
    public class UnionParameter : LayerParameter
    {
        public string ResultLayerName { get; private set; }

        public UnionParameter(string[] line) : base(line[3], line[1], line[2])
        {
            ResultLayerName = line[0];
        }

        public override string ToString()
        {
            return $"{ResultLayerName}, {Year,4}, {Theme,30}, {LegalState,15} ";
        }
    }
}
