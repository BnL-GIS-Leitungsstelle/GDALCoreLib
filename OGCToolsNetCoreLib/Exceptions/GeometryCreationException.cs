using System;

namespace OGCToolsNetCoreLib.Exceptions;

public class GeometryCreationException : Exception
{
    public GeometryCreationException(string message) : base(message)
    {
    }
}

