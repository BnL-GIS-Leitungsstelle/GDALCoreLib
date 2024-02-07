using GdalToolsLib.Extensions;

namespace GdalToolsLib.Feature;

public class FeatureFieldWriteResult
{
    public bool Valid
    {
        get
        {
            switch (ResultValidationType)
            {
                case EFieldWriteErrorType.IsValid:
                    return true;
                default:
                    return false;
            }
        }
    }


    public EFieldWriteErrorType ResultValidationType { get; set; }

    public EFeatureErrorLevel ErrorLevel
    {
        get
        {
            switch (ResultValidationType)
            {
                case EFieldWriteErrorType.IsValid:
                    return EFeatureErrorLevel.None;

                case EFieldWriteErrorType.NullCast:
                case EFieldWriteErrorType.LossOfFractionInInteger:
                    return EFeatureErrorLevel.Warning;

                default:
                    return EFeatureErrorLevel.Error;
            }
        }
    }

    public long FeatureFid { get; set; }

    public string ObjNummer { get; set; }
    public string Name { get; set; }

    public string Remarks { get; set; }

    private FeatureFieldWriteResult()
    { }


    public FeatureFieldWriteResult(long featureFid, EFieldWriteErrorType writeErrorType = EFieldWriteErrorType.Unknown, string remarks = default)
    {
        FeatureFid = featureFid;
        ResultValidationType = writeErrorType;
        Remarks = remarks;

        // log..

    }

    public override string ToString()
    {
        return Valid ? $"Feature in FID = ({FeatureFid}) is VALID" : $" Invalid geometry in FID = ({FeatureFid}, ObjNummer = {ObjNummer}, Name = {Name}), Message: {ResultValidationType.GetEnumDescription(typeof(EFieldWriteErrorType))} ({Remarks})";
    }
}