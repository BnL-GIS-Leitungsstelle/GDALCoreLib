using System;

namespace GdalToolsLib.Exceptions;

public class DataSourceReadOnlyException:Exception
{
    public DataSourceReadOnlyException(string message):base(message)
    {
    }
}