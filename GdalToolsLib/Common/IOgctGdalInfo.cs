using System.Collections.Generic;

namespace GdalToolsLib.Common;

public interface IOgctGdalInfo
{
    List<string> ShowSupportedDatasources();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    List<string> GetAvailableDriverNames();
}