using System;
using System.Collections.Generic;

namespace OGCToolsNetCoreLib.GeoProcessor;

public class UnionProcessLayerGroup
{
    public string ResultLayerName { get; set; }


    public List<UnionProcessItem> Items { get; set; }




    public UnionProcessLayerGroup(List<string> layerNames, string resultLayerName)
    {
        Items = new();

        ResultLayerName = resultLayerName;

        BuildItems(layerNames);
    }



    private void BuildItems(List<string> layerNames)
    {
        if (layerNames.Count < 2)
        {
            throw new NotSupportedException("Too few arguments, to create UnionProcessLayer");
        }


        if (layerNames.Count == 2)
        {
            Items.Add(new UnionProcessItem(layerNames[0], layerNames[1], ResultLayerName, false));
        }

        if (layerNames.Count > 2)
        {
            for (int i = 0; i < layerNames.Count; i++)
            {
                bool isTemporaryStep = i < layerNames.Count - 1;

                string tempResultName = layerNames[i] + "TempUnion";
                if (i==0)
                {
                    Items.Add(new UnionProcessItem(layerNames[0], layerNames[1], tempResultName, isTemporaryStep));
                }

                if (i==1)
                {
                    continue; // is unioned in first step
                }

                if (isTemporaryStep)
                {
                    string tempName = tempResultName + layerNames[i] + "Temp";
                    var item = new UnionProcessItem(tempResultName, layerNames[i], tempName, isTemporaryStep);
                    tempResultName = tempName;
                }
                else
                {
                    var item = new UnionProcessItem(tempResultName, layerNames[i], ResultLayerName, false);
                }
            }
        }
    }
}
