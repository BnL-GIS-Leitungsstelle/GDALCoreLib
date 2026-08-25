using System.Collections.Generic;
using BnL.FGDBAddToMasterGUI.Models;

namespace BnL.FGDBAddToMasterGUI.Services;

public interface IGeodatabaseTransferService
{
    IReadOnlyList<string> GetLayerNames(string databasePath);
    IReadOnlyList<FieldDescriptor> GetFields(string databasePath, string layerName);
    string CreateJoinCondition(IEnumerable<string> fieldNames);
    TransferResult Transfer(TransferRequest request);
}
