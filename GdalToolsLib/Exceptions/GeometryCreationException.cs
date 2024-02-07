using System;

namespace GdalToolsLib.Exceptions;

public class GeometryCreationException : Exception
{
    public GeometryCreationException(string message) : base(message)
    {
    }
}

