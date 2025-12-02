namespace BnL.ExcelXYPointDataToEnvelopeLayers.Models;

public class WolfEnvelopeModel
{
    public string? ObservationDate { get; set; }
    public string? IndividualId { get; set; }
    public int IndividuumCount { get; set; } = 1;
    public string? CompartmentMain { get; set; }
    public string? Canton { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public double? EnvelopeLowerLeftX { get; set; }
    public double? EnvelopeLowerLeftY { get; set; }
    public double? EnvelopeUpperRightX { get; set; }
    public double? EnvelopeUpperRightY { get; set; }
}