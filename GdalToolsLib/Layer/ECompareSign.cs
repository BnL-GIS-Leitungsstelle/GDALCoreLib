using System.ComponentModel;

namespace OGCToolsNetCoreLib.Layer;

public enum ECompareSign
{
    [Description("=")]
    IsEqual,

    [Description(">")]
    GreaterThan,

    [Description("<")]
    LessThan,

    [Description(">=")]
    IsEqualOrGreaterThan,

    [Description("<=")]
    IsEqualOrLessThan,

    [Description("<>")]
    InNotEqual
}
