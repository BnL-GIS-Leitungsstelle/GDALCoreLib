using System;
using System.Linq;
using GdalToolsLib.Common;
using OSGeo.OSR;

namespace GdalToolsLib.Helpers;

internal static class ESpatialRefWktHelpers
{
    public static ESpatialRefWkt FromSpatialReference(SpatialReference spatialRef)
    {
        var name = spatialRef == null ? "Undefined geographic SRS" : spatialRef.GetName();

        string trimmedName = String.Concat(name.Where(c => !Char.IsWhiteSpace(c)));

        //  convert  CH1903 +/ LV95 into CH1903plus_LV95
        trimmedName = trimmedName.Replace("+", "plus");
        trimmedName = trimmedName.Replace("/", "_");

        return (ESpatialRefWkt)Enum.Parse(typeof(ESpatialRefWkt), trimmedName);
    }
}