using System;
using System.Linq;
using OGCToolsNetCoreLib.Common;
using OSGeo.OSR;

namespace OGCToolsNetCoreLib.Layer
{
    public class LayerSpatialRef
    {
        public string Name { get; set; }

        public ESpatialRefWKT SpRef { get; set; }

        /// <summary>
        /// class to facilitate the usage of spatial reference, by masking the very long and cryptic WKT-definitions of a projection
        /// in a handy name. As there is no complete mapping table, the enum ESpatialRefWKT needs to be enhanced when a new
        /// spref-name is encountered. The spref-names need to be masked according to the naming restrictions in enums.
        /// </summary>
        /// <param name="layer"></param>
        public LayerSpatialRef(OSGeo.OGR.Layer layer)
        {
            using (var spatialRef = layer.GetSpatialRef())
            {
                Name = spatialRef == null ? "Undefined geographic SRS" : spatialRef.GetName();

                // mask string to use as valid enum-entry
                string spRefNameMaskedIntoEnum = String.Concat(Name.Where(c => !Char.IsWhiteSpace(c)));
                spRefNameMaskedIntoEnum = spRefNameMaskedIntoEnum.Replace("+", "plus");
                spRefNameMaskedIntoEnum = spRefNameMaskedIntoEnum.Replace("/", "_");
                spRefNameMaskedIntoEnum = spRefNameMaskedIntoEnum.Replace("-", "_");

                // uncomment, if the long long wkt-string is needed to add a new enum-entry
                if (spatialRef != null)
                {
                  //  Console.WriteLine(GetProjString(spatialRef,spRefNameMaskedIntoEnum));
                }

                SpRef = (ESpatialRefWKT)Enum.Parse(typeof(ESpatialRefWKT), spRefNameMaskedIntoEnum);
            }
        }


        /// <summary>
        /// see valid entries in...
        /// </summary>
        /// <param name="name"></param>
        public LayerSpatialRef(string name)
        {
            Name = name;
        }

        public string GetProjString( SpatialReference spRef, string spatialRefName)
        {
            if (spRef != null)
            {
                spRef.ExportToWkt(out string argout, new string[] { spatialRefName });
                return argout;
            }

            throw new NotImplementedException("GetProjection String called without Spatial Reference");
        }
    }
}
