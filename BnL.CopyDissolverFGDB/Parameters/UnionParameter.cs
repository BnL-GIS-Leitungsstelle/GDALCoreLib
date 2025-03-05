using System.Collections.Generic;

namespace BnL.CopyDissolverFGDB.Parameters
{
    public class UnionParameter
    {
        public string ResultLayerName { get; set; }

        public List<LayerParameter> LayerParameters { get; set; }

        public UnionParameter(string resultLayerName, List<LayerParameter> layersToUnion)
        {
            ResultLayerName = resultLayerName;
            LayerParameters = layersToUnion;
        }

        public override string ToString()
        {
            return $"UnionResult {ResultLayerName}, Layers {LayerParameters.Count}";
        }
    }
}
