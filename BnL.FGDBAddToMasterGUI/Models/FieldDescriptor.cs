using GdalToolsLib.Layer;
using OSGeo.OGR;

namespace BnL.FGDBAddToMasterGUI.Models;

public sealed record FieldDescriptor(
    string Name,
    string TypeName,
    FieldType Type,
    FieldSubType SubType,
    int? Width,
    int? Precision,
    bool IsNullable,
    bool IsUnique)
{
    public static FieldDescriptor From(FieldDefnInfo field) => new(
        field.Name,
        field.TypeName,
        field.Type,
        field.SubType,
        field.Width,
        field.Precision,
        field.IsNullable,
        field.IsUnique);

    public FieldDefnInfo ToFieldDefinition(int ogrIndex)
    {
        return new FieldDefnInfo(Name, Type, Width ?? 0, IsNullable, IsUnique)
        {
            OgrIndex = ogrIndex,
            TypeName = TypeName,
            SubType = SubType,
            Precision = Precision,
            DomainName = null!
        };
    }
}
