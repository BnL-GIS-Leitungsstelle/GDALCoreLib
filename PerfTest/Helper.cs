using GdalToolsLib.Geometry;
using GdalToolsLib.Layer;
using GdalToolsLib.Models;
using GdalToolsLib.VectorTranslate;
using MaxRev.Gdal.Core;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO;
using OSGeo.GDAL;
using OSGeo.OGR;
using Envelope = NetTopologySuite.Geometries.Envelope;
using Geometry = NetTopologySuite.Geometries.Geometry;

namespace PerfTest;

public static class Helper
{
    private static WKBReader wkbReader = new();

    public static async Task<Dictionary<(int a, int b), double>> GetSelfOverlaps(string dsName, string layerName, bool? forceConcurrency = null)
    {
        GdalBase.ConfigureAll();
        // var inMem = "/vsimem/" + dsName;
        // VectorTranslate.Run(dsName, inMem, new VectorTranslateOptions { SourceLayerName = layerName });
        using var ds = Ogr.Open(dsName, GdalConst.GA_ReadOnly);
        using var layer = ds.GetLayerByName(layerName);
        var featureCount = (int)layer.GetFeatureCount(0);
        
        if (forceConcurrency != true)
        {
            return new Dictionary<(int a, int b), double>();
            // var intersections = CalculateIntersections(layer.OgrLayer, featureCount, null);
        }
        // Get the bounding box of the layer
        using var bbox = new OSGeo.OGR.Envelope();
        layer.GetExtent(bbox, 1); // 1 means the extent will be updated

        // Number of rows and columns for the grid
        const int rows = 20; // Adjust for the desired number of rows
        const int cols = 20; // Adjust for the desired number of columns

        // Calculate the width and height of each tile
        var tileWidth = (bbox.MaxX - bbox.MinX) / cols;
        var tileHeight = (bbox.MaxY - bbox.MinY) / rows;
        
        // List to hold tasks for parallel processing
        var tasks = new List<Task<Dictionary<(int a, int b), double>>>(rows * cols);

        var maxConcurrent = new SemaphoreSlim(7);
        
        var featuresPerTileApproximation = featureCount / rows / cols * 2;
        var completed = 0;
        // Process each tile in parallel
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                // if (!(i == 2 && j == 6)) continue;
                // Calculate the bounds of the current tile
                var tileMinX = bbox.MinX + j * tileWidth;
                var tileMinY = bbox.MinY + i * tileHeight;
                var tileMaxX = tileMinX + tileWidth;
                var tileMaxY = tileMinY + tileHeight;
                
                // Create a geometry for the tile's bounding box (as a Polygon)
                var tileGeometry = PolyFromExtent(tileMinX, tileMaxX, tileMinY, tileMaxY);
                var x = j;
                var y = i;
                tasks.Add(Task.Run(async () =>
                {
                    await maxConcurrent.WaitAsync();
                    using var dsRef = new OgctDataSourceAccessor().OpenOrCreateDatasource(dsName);

                    using var layerRef = dsRef.OpenLayer(layerName);
                    var ogrLayer = layerRef.OgrLayer;
                    ogrLayer.SetSpatialFilter(tileGeometry);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var intersections = CalculateIntersections(ogrLayer, featuresPerTileApproximation, tileGeometry);
                    watch.Stop();
                    Console.WriteLine($"Tile {x}, {y} took: {watch.Elapsed.TotalSeconds}s");
                    maxConcurrent.Release();
                    Console.WriteLine($"{++completed}/{rows * cols}");
                    return intersections;
                }));
            }
        }

        var finished = await Task.WhenAll(tasks);
        var totalCount = finished.Sum(t => t.Count);
        finished.ElementAt(0).EnsureCapacity(totalCount);
        var sum = finished.Aggregate((total, current) =>
        {
            foreach (var (k, v) in current)
            {
                if (total.TryGetValue(k, out var val))
                {
                    total[k] = val + v;
                }
                else if (total.ContainsKey((k.b, k.a)))
                {
                    total[(k.b, k.a)] = val + v;
                }
                else
                {
                    total.Add(k, v);
                }
            }

            return total;
        });
        return sum;

    }

    private static OSGeo.OGR.Geometry PolyFromExtent(double tileMinX, double tileMaxX, double tileMinY, double tileMaxY)
    {
        var tileGeometry = new OSGeo.OGR.Geometry(wkbGeometryType.wkbPolygon);
        var linearRing = new OSGeo.OGR.Geometry(wkbGeometryType.wkbLinearRing);
        linearRing.AddPoint_2D(tileMinX, tileMinY);           // Bottom-left corner
        linearRing.AddPoint_2D(tileMaxX, tileMinY);           // Bottom-right corner
        linearRing.AddPoint_2D(tileMaxX, tileMaxY);           // Top-right corner
        linearRing.AddPoint_2D(tileMinX, tileMaxY);           // Top-left corner
        linearRing.AddPoint_2D(tileMinX, tileMinY);           // Bottom-left corner again, to close the ring
        tileGeometry.AddGeometry(linearRing);
        return tileGeometry;
    }

    static Dictionary<(int a, int b), double> CalculateIntersections(Layer layer, int featureCount, OSGeo.OGR.Geometry clip, CancellationToken? cancellationToken = null)
    {
        var progressReportBatch = Math.Min(featureCount / 10, 100000);
        
        var features = new Dictionary<(int id, int part), (OSGeo.OGR.Geometry geometry, Envelope envelope)>(featureCount);
        
        var featureIndex = 0;
        
        var rTree = new STRtree<(int id, int part)>(featureCount);
        
        Feature feature;
        var maxVertices = 0;
        
        while ((feature = layer.GetNextFeature()) != null)
        {
            featureIndex++;
            using var geom = feature.GetGeometryRef();

            if (geom != null)
            {
                var fid = (int)feature.GetFID();
                var clipped = geom.Intersection(clip);
                if (clipped.IsEmpty()) continue;
                // var subDiv = ntGeo.SubdivideAndExplode();
                var subDiv = clipped.SubdivideAndExplode(); 
                
                for (var i = 0; i < subDiv.Count; i++)
                {
                    var polyPart = subDiv[i];
                    var envelope = polyPart.GetNTSEnvelope();
                    rTree.Insert(envelope, (fid, i));
                    features.Add((fid, i), (polyPart, envelope));
                    maxVertices = Math.Max(maxVertices, polyPart.GetPointCount());
                }
                
            }
            feature.Dispose();
            
            // if (++featureIndex % progressReportBatch == 0)
            // {
            //     cancellationToken?.ThrowIfCancellationRequested();
            //     reportSelfOverlapValidationProgress?.Invoke((double)featureIndex / featureCount); // report partial progress for feature read
            // }
        }
        GC.Collect();
        rTree.Build();
        Console.WriteLine($"Before: {featureIndex}, After: {features.Count}");

        // to store the area of intersection for each pair of features
        var pairIntersections = new Dictionary<(int a, int b), double>();

        featureIndex = 0;
        // return pairIntersections;
        foreach (var (featureId, (geometry, envelope)) in features)
        {
            featureIndex++;
            // if (++featureIndex % progressReportBatch == 0)
            // {
            //     cancellationToken?.ThrowIfCancellationRequested();
            //     reportSelfOverlapValidationProgress?.Invoke((double)featureIndex / featureCount); // report partial progress for feature read
            // }
            var potentialIntersections = rTree.Query(envelope);
            foreach (var otherId in potentialIntersections)
            {
                // if this feature pair has already been intersected the other way around, we can skip it here
                if (otherId == featureId || pairIntersections.ContainsKey((otherId.id, featureId.id))) continue;
                var otherFeature = features[otherId];

                // var prep = new PreparedPolygon(geometry);
                
                // if there is no intersection or the only intersection consists of touching borders, we don't need to calculate further 
                if (!geometry.Intersects(otherFeature.geometry) || geometry.Touches(otherFeature.geometry)) continue;
                
                var intersectionArea = geometry.Intersection(otherFeature.geometry).GetArea();

                if (intersectionArea > 1)
                {
                    var val = pairIntersections.GetValueOrDefault((featureId.id, otherId.id), 0d);
                    pairIntersections[(featureId.id, otherId.id)] = intersectionArea + val;
                }
            }
        }
        // reportSelfOverlapValidationProgress?.Invoke(1);
        return pairIntersections;
        // return pairIntersections.Select(x => new SelfOverlapErrorResult(x.Key.a, x.Key.b, x.Value)).ToList();
    }

    static Envelope GetNTSEnvelope(this OSGeo.OGR.Geometry geometry)
    {
        using var bbox = new OSGeo.OGR.Envelope();
        geometry.GetEnvelope(bbox);
        return new Envelope(bbox.MinX, bbox.MaxX, bbox.MinY, bbox.MaxY);

    }

    static Geometry ToNTSGeometry(this OSGeo.OGR.Geometry geometry)
    {
        var bSize = geometry.WkbSize();
        var bytes = new byte[bSize];
        geometry.ExportToWkb(bytes);
        return wkbReader.Read(bytes);
    }

    static List<OSGeo.OGR.Geometry> SubdivideAndExplode(this OSGeo.OGR.Geometry source, int maxSize = 512)
    {
        var result = new List<OSGeo.OGR.Geometry>();
        switch (source.GetGeometryType())
        {
            case wkbGeometryType.wkbMultiPolygon:
                var plys = source.GetGeometryCount();
                for (var i = 0; i < plys; i++)
                {
                    result.AddRange(source.GetGeometryRef(i).SubdivideAndExplode(maxSize));
                }

                break;
            case wkbGeometryType.wkbPolygon when source.GetGeometryRef(0).GetPointCount() <= maxSize:
                return [source];
            case wkbGeometryType.wkbPolygon:
            {
                using var bbox = new OSGeo.OGR.Envelope();
                source.GetEnvelope(bbox);

                // either split horizontally or vertically, depending on the current orientation
                var wid = bbox.MaxX - bbox.MinX;
                var hei = bbox.MaxY - bbox.MinY;
                var (splitsHorizontal, splitsVertical) = wid > hei ? (2, 1) : (1, 2);

                var newBboxes = new List<OSGeo.OGR.Geometry>(splitsHorizontal * splitsVertical);
                var width = wid / splitsHorizontal;
                var height = hei / splitsVertical;
                for (var x = bbox.MinX; x < bbox.MaxX; x += width)
                {
                    for (var y = bbox.MinY; y < bbox.MaxY; y += height)
                    {
                        newBboxes.Add(PolyFromExtent(x, x + width, y, y + height));
                    }
                }

                result.AddRange(newBboxes.SelectMany(bbx => source.Intersection(bbx).SubdivideAndExplode(maxSize)));
                break;
            }
        }

        return result;
    }

    // static List<Polygon> SubdivideAndExplode(this Geometry source, int maxSize = 512)
    // {
    //     var result = new List<Polygon>();
    //     switch (source)
    //     {
    //         case MultiPolygon multiPolygon:
    //             result.AddRange(multiPolygon.Geometries.SelectMany(g => g.SubdivideAndExplode(maxSize)));
    //             break;
    //         case Polygon polygon when polygon.NumPoints <= maxSize:
    //             return [polygon];
    //         case Polygon polygon:
    //         {
    //             var bbox = polygon.EnvelopeInternal;
    //
    //             LineString splitLine;
    //             
    //             // either split horizontally or vertically, depending on the current orientation
    //             var (splitsHorizontal, splitsVertical) = bbox.Width > bbox.Height ? (2, 1) : (1, 2);
    //
    //             var newBboxes = new List<Geometry>(splitsHorizontal * splitsVertical);
    //             var width = bbox.Width / splitsHorizontal;
    //             var height = bbox.Height / splitsVertical;
    //             for (var x = bbox.MinX; x < bbox.MaxX; x += width)
    //             {
    //                 for (var y = bbox.MinY; y < bbox.MaxY; y += height)
    //                 {
    //                     newBboxes.Add(new Envelope(x, x + width, y, y + height).ToPolygon());
    //                 }
    //             }
    //             
    //             result.AddRange(newBboxes.SelectMany(bbx => polygon.Intersection(bbx).SubdivideAndExplode(maxSize)));
    //             break;
    //         }
    //     }
    //
    //     return result;
    // }

    // static Polygon ToPolygon(this Envelope envelope)
    // {
    //     return GeometryFactory.Default.CreatePolygon([
    //         new Coordinate(envelope.MinX, envelope.MinY),
    //         new Coordinate(envelope.MaxX, envelope.MinY),
    //         new Coordinate(envelope.MaxX, envelope.MaxY),
    //         new Coordinate(envelope.MinX, envelope.MaxY),
    //         new Coordinate(envelope.MinX, envelope.MinY),
    //     ]);
    // }
}