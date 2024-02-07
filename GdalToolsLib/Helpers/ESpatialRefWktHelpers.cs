using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OGCToolsNetCoreLib.Common;
using OSGeo.OSR;

namespace OGCToolsNetCoreLib.Helpers
{
    internal static class ESpatialRefWktHelpers
    {
        public static ESpatialRefWKT FromSpatialReference(SpatialReference spatialRef)
        {
            var name = spatialRef == null ? "Undefined geographic SRS" : spatialRef.GetName();

            string trimmedName = String.Concat(name.Where(c => !Char.IsWhiteSpace(c)));

            //  convert  CH1903 +/ LV95 into CH1903plus_LV95
            trimmedName = trimmedName.Replace("+", "plus");
            trimmedName = trimmedName.Replace("/", "_");

            return (ESpatialRefWKT)Enum.Parse(typeof(ESpatialRefWKT), trimmedName);
        }
    }
}
