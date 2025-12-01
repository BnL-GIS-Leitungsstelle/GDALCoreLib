using System;

namespace BnL.ExcelXYPointDataToEnvelopeLayers.Models;

public class WolfModel
{
    public int? MonitoringYear { get; set; }
    public DateTime? ObservationDate { get; set; }
    public string? IndividualId { get; set; }
    public string? CompartmentMain { get; set; }
    public string? Canton { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public string? WolfAuthorisation { get; set; }
}
