using System;

namespace OGCToolsNetCoreLib.Exceptions
{
    public class DataSourceReadOnlyException:Exception
    {
        public DataSourceReadOnlyException(string message):base(message)
        {
        }
    }
}
