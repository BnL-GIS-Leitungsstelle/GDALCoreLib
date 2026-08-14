using System.Collections.Generic;

namespace BnL.FGDBAddToMasterGUI.Models;

public sealed record TransferRequest(
    string MasterDatabasePath,
    string MasterLayerName,
    string AddDatabasePath,
    string AddLayerName,
    IReadOnlyList<string> FieldNamesToAdd,
    IReadOnlyList<string> JoinFieldNames);
