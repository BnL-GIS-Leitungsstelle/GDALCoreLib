using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Layer;

public class WhereClauseCondition
{
    public FieldDefnInfo Field { get; set; }

    public ECompareSign CompareSign { get; set; }

    public string Content { get; set; }


    public void AddField(string name, FieldType type, int width,bool isNullable = true, bool isUnique = false )
    {
        Field = new FieldDefnInfo(name, type, width, isNullable, isUnique);
    }

    public void AddCompareSign(ECompareSign value)
    {
        CompareSign = value;
    }


    public void AddContent(object value)
    {
        Content = value.ToString();
    }

    public override string ToString()
    {
        return $" {Field.Name} {Field.Type} = {Content}";
    }


}