using System;

namespace OGCToolsNetCoreLib.Exceptions
{
    public class DataSourceMethodNotImplementedException : Exception
    {
        public DataSourceMethodNotImplementedException(string message) : base(message)
        {
        }
    }
}
