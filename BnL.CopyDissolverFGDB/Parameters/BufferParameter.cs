using GdalToolsLib.Layer;
using NetTopologySuite.Operation.Distance;
using System.Globalization;

namespace BnL.CopyDissolverFGDB.Parameters;

public class BufferParameter : LayerParameter
{
    public double BufferDistanceMeter { get; private set; }

    public BufferParameter(string legalState, string layername, string year, string distanceInMeter) : base(layername, year, legalState)
    {
        BufferDistanceMeter = double.Parse(distanceInMeter, CultureInfo.InvariantCulture);
    }
    public BufferParameter(string[] line) : this(line[0], line[1], "N.N.", line[2]) { }

    public override string ToString()
    {
        return $"{LegalState,30}, {Theme,40}, Radius: {BufferDistanceMeter:F2} ";
    }
}