using GdalToolsLib.Layer;
using NetTopologySuite.Operation.Distance;
using System.Globalization;

namespace BnL.CopyDissolverFGDB.Parameters;

public class BufferParameter
{
    public string LegalState { get; }
    public string Theme { get; }
    public double BufferDistanceMeter { get; }

    public BufferParameter(string[] line)
    {
        LegalState = line[0];
        Theme = line[1];
        BufferDistanceMeter = double.Parse(line[2], CultureInfo.InvariantCulture);
    }
}