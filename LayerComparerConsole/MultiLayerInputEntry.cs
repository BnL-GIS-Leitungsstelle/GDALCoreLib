namespace LayerComparerConsole
{
    public record MultiLayerInputEntry(
        string MasterGdb, 
        string MasterLayer, 
        string CandidateGdb, 
        string CandidateLayer);
}
