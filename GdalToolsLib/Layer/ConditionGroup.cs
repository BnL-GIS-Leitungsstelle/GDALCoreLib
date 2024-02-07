using System.Collections.Generic;
using OGCToolsNetCoreLib.Helpers;

namespace OGCToolsNetCoreLib.Layer;

public class ConditionGroup
{
    public List<WhereClauseCondition> FieldConditions { get; set; }

    public ConditionGroup()
    {
        FieldConditions = new List<WhereClauseCondition>();
    }

    public void AddFieldCondition(FieldDefnInfo field, ECompareSign compareSign, object value)
    {
        var clauseCondition = new WhereClauseCondition();
        clauseCondition.AddField(field.Name, field.Type, (int)field.Width);
        clauseCondition.AddCompareSign(compareSign);
        clauseCondition.AddContent(value);

        FieldConditions.Add(clauseCondition);
    }

    public string ToSql()
    {
        return QueryHelpers.BuildWhereClause(FieldConditions);
    }
}