using OSGeo.OGR;
using System;

namespace GdalToolsLib.Common
{
    public static class GdalHelpers
    {
        public static wkbGeometryType ToMulti(this wkbGeometryType geomType)
        {
            // Couldn't find a better way to check if a geometry is already multipart
            if (Ogr.GeometryTypeToName(geomType).Contains("Multi"))
            {
                return geomType;
            }
            return Ogr.GT_GetCollection(geomType);
        }

        public static string ToStringName(this wkbGeometryType geometryType)
        {
            return geometryType switch
            {
                wkbGeometryType.wkbPolygon => "POLYGON",
                wkbGeometryType.wkbMultiPolygon => "MULTIPOLYGON",
                wkbGeometryType.wkbLineString => "LINESTRING",
                wkbGeometryType.wkbMultiLineString => "MULTILINESTRING",
                wkbGeometryType.wkbPoint => "POINT",
                wkbGeometryType.wkbMultiPoint => "MULTIPOINT",
                _ => throw new NotImplementedException(),
            };
        }
    }
}