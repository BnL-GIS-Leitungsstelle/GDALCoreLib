using System;
using System.Drawing;
using OSGeo.OGR;

namespace GdalToolsLib.Models;

public interface IOgctGeometry : IDisposable
{
    string Type { get; }
    bool IsEmpty { get; }
    bool IsSimple { get; }
    bool IsValid { get; }
    long GeometryCount { get; }
    double Area { get; }
    string GetWkt();

    IOgctGeometry CloneAndOpen();

    bool IsAMultiGeometryType();
    bool IsAGeometryCollectionOrMultiSurfaceGeometryType();

    bool Disjoint(IOgctGeometry candidate);

    bool Touches(IOgctGeometry candidate);

    bool Intersects(IOgctGeometry candidate);

    IOgctGeometry CreateMultipartGeometryAndOpen();
    IOgctGeometry GetAndOpenIntersection(IOgctGeometry target);
    IOgctGeometry GetAndOpenUnion(IOgctGeometry target);

    Rectangle GetBoundingBox();
    IOgctGeometry GetAndOpenBuffer(double distance);
    IOgctGeometry OpenRepaired();
    int Reproject(IOgctGeometry targetGeometry);
}