namespace BnL.FGDBAddToMasterGUI.Models;

public sealed record TransferResult(
    int CreatedFieldCount,
    int UpdatedMasterFeatureCount,
    int UnmatchedMasterFeatureCount,
    int UnmatchedAddFeatureCount,
    int NullJoinKeyCount,
    string? ErrorMessage = null)
{
    public bool IsSuccess => string.IsNullOrWhiteSpace(ErrorMessage);

    public static TransferResult Failed(string errorMessage) => new(0, 0, 0, 0, 0, errorMessage);
}
