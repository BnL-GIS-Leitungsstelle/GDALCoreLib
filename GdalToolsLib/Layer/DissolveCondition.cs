using System.Collections.Generic;

namespace GdalToolsLib.Layer;

public class DissolveCondition
{
    public List<ConditionGroup> DissolveGroups { get; set; }

    public DissolveCondition()
    {
        DissolveGroups = new List<ConditionGroup>();
    }

    public void AddDissolveGroup(ConditionGroup group)
    {
        DissolveGroups.Add(group);
    }
}