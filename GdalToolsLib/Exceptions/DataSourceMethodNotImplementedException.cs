using System;

namespace GdalToolsLib.Exceptions;

public class DataSourceMethodNotImplementedException : Exception
{
    public DataSourceMethodNotImplementedException(string message) : base(message)
    {
    }
}