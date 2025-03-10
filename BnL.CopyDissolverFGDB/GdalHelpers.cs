using OSGeo.OGR;
using System;

namespace BnL.CopyDissolverFGDB
{
    internal static class GdalHelpers
    {

        public static wkbGeometryType ToMulti(this wkbGeometryType geomType)
        {
            return geomType switch
            {
                wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon => wkbGeometryType.wkbMultiPolygon,
                wkbGeometryType.wkbLineString or wkbGeometryType.wkbMultiLineString => wkbGeometryType.wkbMultiLineString,
                wkbGeometryType.wkbPoint or wkbGeometryType.wkbMultiPoint => wkbGeometryType.wkbMultiPoint,
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