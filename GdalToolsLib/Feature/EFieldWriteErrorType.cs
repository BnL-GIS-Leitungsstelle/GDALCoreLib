using System.ComponentModel;

namespace OGCToolsNetCoreLib.Feature;


public enum EFieldWriteErrorType
{
    [Description("Invalid Cast")]
    InvalidCast,

    [Description("Invalid Null Cast")]
    InvalidNullCast,

    [Description("Null Cast")]
    NullCast,

    [Description("Invalid cast in not-nullable target")]
    InvalidCastToNotNullable,

    [Description("overflow: value too big for datatype")]
    OverflowInCast,

    [Description("conversions means loss of fraction of real")]
    LossOfFractionInInteger,

    [Description("Format not readable")]
    CastFailureDueToFalseFormat,
    
    [Description("string length exceeds length of target type")]
    StringLengthExceedsTargetTypeLength,
    
    [Description("Not Named")]
    Unknown,

    [Description("Is Valid")]
    IsValid
}