namespace BnL.CopyDissolverFGDB.Parameters
{
    public class UnionParameterLayer
    {
        public string ResultLayerName { get; private set; }
        public LayerParameter LayerParameter { get; private set; }


        public UnionParameterLayer(string resultLayerName, LayerParameter layerParameter)
        {
            ResultLayerName = resultLayerName;
            LayerParameter = layerParameter;
        }

        public override string ToString()
        {
            return $"{ResultLayerName}, {LayerParameter.Year,4}, {LayerParameter.Theme,30}, {LayerParameter.LegalState,15} ";
        }
    }
}
