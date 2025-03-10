using OSGeo.OGR;
using System;

namespace GdalToolsLib.Common
{
    internal static class GdalHelpers
    {

        public static wkbGeometryType ToMulti(this wkbGeometryType geomType)
        {
            return geomType switch
            {
                wkbGeometryType.wkbPolygon => wkbGeometryType.wkbMultiPolygon,
                wkbGeometryType.wkbLineString => wkbGeometryType.wkbMultiLineString,
                wkbGeometryType.wkbPoint => wkbGeometryType.wkbMultiPoint,
                _ => throw new NotImplementedException(),
            };
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