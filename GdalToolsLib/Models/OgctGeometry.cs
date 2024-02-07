using System;
using System.Drawing;
using OSGeo.OGR;
using OSGeo.OSR;
using Envelope = OSGeo.OGR.Envelope;

namespace GdalToolsLib.Models;

public class OgctGeometry : IOgctGeometry
{
    private readonly OSGeo.OGR.Geometry _geometry;

    public OgctGeometry(OSGeo.OGR.Geometry geometry)
    {
        _geometry = geometry;
    }

    internal OSGeo.OGR.Geometry OgrGeometry => _geometry;

    public string Type => _geometry.GetGeometryType().ToString();
    public bool IsEmpty => _geometry.IsEmpty();
    public bool IsSimple => _geometry.IsSimple();
    public bool IsValid => _geometry.IsValid();
    public long GeometryCount => _geometry.GetGeometryCount();
    public double Area => _geometry.GetArea();

    public string GetWkt()
    {
        _geometry.ExportToWkt(out var result);
        return result;
    }


    public IOgctGeometry CloneAndOpen()
    {
        var cloneGeometry = _geometry.Clone();
        return new OgctGeometry(cloneGeometry);
    }

    /// <summary>
    /// Some geometrytypes are not stored correctly regarding standards of features or layers.
    /// E.g. if you want to store a polygon in a multipolygon-layer/feature
    /// </summary>
    /// <param name="otherGeomType">the geomType of a layer or feature </param>
    /// <returns></returns>
    public IOgctGeometry CreateMultipartGeometryAndOpen(wkbGeometryType otherGeomType)
    {
        switch (this.OgrGeometry.GetGeometryType())
        {
            case wkbGeometryType.wkbPolygon25D:
            case wkbGeometryType.wkbPolygon:
                return new OgctGeometry(Ogr.ForceToMultiPolygon(_geometry));
               
            case wkbGeometryType.wkbLineString:
                return new OgctGeometry(Ogr.ForceToMultiLineString(_geometry));
            
            case wkbGeometryType.wkbPoint:
                return new OgctGeometry(Ogr.ForceToMultiPoint(_geometry));
                 
            default:
                throw new NotImplementedException("Convert to MultiPartGeometry not defined");
            // return this;
        }
    }

    public bool IsAMultiGeometryType()
    {
        var geomType = OgrGeometry.GetGeometryType();

        switch (geomType)
        {
            case wkbGeometryType.wkbPolygon:
            case wkbGeometryType.wkbPolygon25D:
            case wkbGeometryType.wkbLineString:
            case wkbGeometryType.wkbPoint:
                return false;

            case wkbGeometryType.wkbMultiPolygon:
            case wkbGeometryType.wkbMultiPolygon25D:
            case wkbGeometryType.wkbMultiLineString:
            case wkbGeometryType.wkbMultiPoint:
                return true;

            default:
                Console.WriteLine($"-- dissolved Geom is of type {geomType}");
                return true;  // unsure..;
        }
    }

    public bool IsAGeometryCollectionOrMultiSurfaceGeometryType()
    {
        var geomType = OgrGeometry.GetGeometryType();

        switch (geomType)
        {
            case wkbGeometryType.wkbGeometryCollection:
            case wkbGeometryType.wkbGeometryCollection25D:
            case wkbGeometryType.wkbGeometryCollectionM:
            case wkbGeometryType.wkbGeometryCollectionZM:
            case wkbGeometryType.wkbMultiSurface:
            case wkbGeometryType.wkbMultiSurfaceM:
            case wkbGeometryType.wkbMultiSurfaceZ:
            case wkbGeometryType.wkbMultiSurfaceZM:
                return true;

            default:
                return false;  // unsure..;
        }
    }

    public bool Intersects(IOgctGeometry candidate)
    {
        var ogctCandidate = (OgctGeometry)candidate;
        return _geometry.Intersects(ogctCandidate.OgrGeometry);
    }

    public bool Touches(IOgctGeometry candidate)
    {
        var ogctCandidate = (OgctGeometry)candidate;
        return _geometry.Touches(ogctCandidate.OgrGeometry);
    }

    public bool Disjoint(IOgctGeometry candidate)
    {
        var ogctCandidate = (OgctGeometry)candidate;
        return _geometry.Disjoint(ogctCandidate.OgrGeometry);
    }

    public Rectangle GetBoundingBox()
    {
        using var env = new Envelope();
        _geometry.GetEnvelope(env);

        return new Rectangle((int)env.MinX, (int)env.MinY, (int)(env.MaxX-env.MinX)+1,(int)(env.MaxY-env.MinY)+1);
    }

    public IOgctGeometry GetAndOpenIntersection(IOgctGeometry target)
    {
        var targetGeometry = ((OgctGeometry)target)._geometry;
        if (targetGeometry.IsValid() && _geometry.IsValid())
        {
            return new OgctGeometry(_geometry.Intersection(targetGeometry));

        }
        else
        {
            // MakeValid()
            // Attempts to make an invalid geometry valid without losing vertices. 
            // details: https://gdal.org/doxygen/classOGRGeometry.html#a700a2d4b1c719e1f65fa3009bfc04f78
            // papszOptions	NULL terminated list of options, or NULL. The following options are available:
            // METHOD = LINEWORK / STRUCTURE.LINEWORK is the default method, which combines all rings into a set of noded lines and then extracts valid polygons from that linework. The STRUCTURE method(requires GEOS >= 3.10 and GDAL >= 3.4) first makes all rings valid, then merges shells and subtracts holes from shells to generate valid result. Assumes that holes and shells are correctly categorized.
            // KEEP_COLLAPSED = YES / NO.Only for METHOD = STRUCTURE.NO(default): collapses are converted to empty geometries YES: collapses are converted to a valid geometry of lower dimension.
            using var repairedSelfGeometry = _geometry.MakeValid(null);
            using var repairedTargetGeometry = targetGeometry.MakeValid(null);
            return new OgctGeometry(repairedSelfGeometry.Intersection(repairedTargetGeometry));
        }


    }


    public IOgctGeometry GetAndOpenUnion(IOgctGeometry target)
    {
        var targetGeometry = ((OgctGeometry)target)._geometry;
        return new OgctGeometry(_geometry.Union(targetGeometry));
    }


    /// <summary>
    /// buffers the given geometry.
    /// See https://gdal.org/doxygen/classOGRGeometry.html#a8694e757f44388bd6da4cf7be696b7e7
    /// fore more info on Buffer
    /// </summary>
    /// <param name="distance">in units of the layer</param>
    /// <returns></returns>
    public IOgctGeometry GetAndOpenBuffer(double distance)
    {
        return new OgctGeometry(_geometry.Buffer(distance,30));
    }

    public IOgctGeometry OpenRepaired()
    {
        // MakeValid()
        // Attempts to make an invalid geometry valid without losing vertices. 
        // details: https://gdal.org/doxygen/classOGRGeometry.html#a700a2d4b1c719e1f65fa3009bfc04f78
        // papszOptions	NULL terminated list of options, or NULL. The following options are available:
        // METHOD = LINEWORK / STRUCTURE.LINEWORK is the default method, which combines all rings into a set of noded lines and then extracts valid polygons from that linework. The STRUCTURE method(requires GEOS >= 3.10 and GDAL >= 3.4) first makes all rings valid, then merges shells and subtracts holes from shells to generate valid result. Assumes that holes and shells are correctly categorized.
        // KEEP_COLLAPSED = YES / NO.Only for METHOD = STRUCTURE.NO(default): collapses are converted to empty geometries YES: collapses are converted to a valid geometry of lower dimension.
        var repaired = _geometry.MakeValid(null);
        return new OgctGeometry(repaired);
    }

    public int Reproject(IOgctGeometry targetGeometry)
    {
        var targetOgctGeometry = (OgctGeometry)targetGeometry;
        var transformation = new CoordinateTransformation(_geometry.GetSpatialReference(),
            targetOgctGeometry.OgrGeometry.GetSpatialReference());
        return _geometry.Transform(transformation);
    }


    public void Dispose()
    {
        _geometry?.Dispose();
    }

    ~OgctGeometry()
    {
        _geometry?.Dispose();
    }
}