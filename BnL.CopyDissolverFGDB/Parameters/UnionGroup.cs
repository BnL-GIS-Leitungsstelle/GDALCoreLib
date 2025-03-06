using System;
using System.Collections.Generic;

namespace BnL.CopyDissolverFGDB.Parameters;

public class UnionGroup
{
    public string ResultLayerName { get; set; }

    public List<UnionGroupItem> Items { get; set; }

    public UnionGroup(string resultLayerName, List<WorkLayer> workLayers)
    {
        ResultLayerName = resultLayerName;

        Items = new();

        MapWorkLayersToUnionGroupItems(workLayers);
    }

    private void MapWorkLayersToUnionGroupItems(List<WorkLayer> workLayers)
    {
        if (workLayers.Count < 2)
        {
            Console.WriteLine("***error: too few arguments to create UnionGroupItem");
        }

        if (workLayers.Count == 2)
        {
            Items.Add(new UnionGroupItem(workLayers[0].DataSourcePath, workLayers[0].OriginalLayerName, workLayers[1].OriginalLayerName, ResultLayerName));
            return;
        }

        var resultTempName = String.Empty;

        for (int i = 0; i < workLayers.Count - 1; i++)
        {
            if (i == 0)
            {
                resultTempName = $"{workLayers[0].OriginalLayerName}{workLayers[i + 1].OriginalLayerName}";
                Items.Add(new UnionGroupItem(workLayers[0].DataSourcePath, workLayers[i].OriginalLayerName, workLayers[i + 1].OriginalLayerName, $"{resultTempName}TempUnion"));
            }

            if (i >= 2 && i + 2 < workLayers.Count)
            {
                var tempName = $"{resultTempName}{workLayers[i + 1].OriginalLayerName}";
                Items.Add(new UnionGroupItem(workLayers[0].DataSourcePath, $"{resultTempName}TempUnion", workLayers[i + 1].OriginalLayerName, $"{tempName}TempUnion"));
                resultTempName = tempName;
            }

            if (i + 2 == workLayers.Count)
            {
                Items.Add(new UnionGroupItem(workLayers[0].DataSourcePath, $"{resultTempName}TempUnion", workLayers[i + 1].OriginalLayerName, ResultLayerName));
            }
        }

    }
}
