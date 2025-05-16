using GdalToolsLib.Models;
using NetTopologySuite.Index.Strtree;
using OSGeo.OGR;
using Envelope = NetTopologySuite.Geometries.Envelope;

namespace PerfTest;

public static class SelfOverlapChecker
{
    public static async Task<Dictionary<(int a, int b), double>> GetSelfOverlaps(string dsName, string layerName, bool? forceConcurrency = null)
    {
        using var ds =  new OgctDataSourceAccessor().OpenOrCreateDatasource(dsName);
        using var layer = ds.OpenLayer(layerName);
        var featureCount = (int)layer.LayerDetails.FeatureCount;
        
        if (layer.LayerDetails.GeomType is not (wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon)) throw new Exception("Only polygons are supported.");
        
        // concurrency can be explicitly disabled, otherwise it will be enabled if working on a layer with more than one million features
        if (forceConcurrency == false || (forceConcurrency == null && featureCount <= 1_000_000))
        {
            return CalculateOverlaps(layer.OgrLayer, featureCount);
        }
        
        using var bbox = new OSGeo.OGR.Envelope();
        layer.OgrLayer.GetExtent(bbox, 0);

        // number of rows and columns for the tiles to process in parallel
        const int rows = 20;
        const int cols = 20;

        var tileWidth = (bbox.MaxX - bbox.MinX) / cols;
        var tileHeight = (bbox.MaxY - bbox.MinY) / rows;
        
        var tileTasks = new List<Task<Dictionary<(int a, int b), double>>>(rows * cols);

        
        var maxConcurrent = new SemaphoreSlim(Environment.ProcessorCount - 1);
        
        var featuresPerTileApproximation = featureCount / rows / cols * 2;
        var completed = 0;
        
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                // calculate the bounds of the current tile
                var tileMinX = bbox.MinX + j * tileWidth;
                var tileMinY = bbox.MinY + i * tileHeight;
                var tileMaxX = tileMinX + tileWidth;
                var tileMaxY = tileMinY + tileHeight;
                
                // create a geometry for the tiles bounding box (as a polygon)
                var tileGeometry = PolygonFromExtent(tileMinX, tileMaxX, tileMinY, tileMaxY);
                var x = j;
                var y = i;
                tileTasks.Add(Task.Run(async () =>
                {
                    await maxConcurrent.WaitAsync();
                    using var dsRef = new OgctDataSourceAccessor().OpenOrCreateDatasource(dsName);

                    using var layerRef = dsRef.OpenLayer(layerName);
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var overlaps = CalculateOverlaps(layerRef.OgrLayer, featuresPerTileApproximation, tileGeometry);
                    watch.Stop();
                    Console.WriteLine($"Tile {x}, {y} took: {watch.Elapsed.TotalSeconds}s");
                    maxConcurrent.Release();
                    Console.WriteLine($"{++completed}/{rows * cols}");
                    return overlaps;
                }));
            }
        }

        var tileResults = await Task.WhenAll(tileTasks);
        
        var totalCount = tileResults.Sum(t => t.Count);
        var result = new Dictionary<(int a, int b), double>(totalCount);

        // sum up the overlaps from different tiles
        foreach (var overlaps in tileResults)
        {
            foreach (var (idPair, area) in overlaps)
            {
                // if the id pair already exists, it means it came from a diffrent tile, so we need to sum the intresection area
                if (result.TryGetValue(idPair, out var val))
                {
                    result[idPair] = val + area;
                }
                // if the id pair reversed already exists, it means it came from a diffrent tile, so we need to sum the intresection area
                else if (result.TryGetValue((idPair.b, idPair.a), out val))
                {
                    result[idPair] = val + area;
                }
                else
                {
                    result.Add(idPair, area);
                }
            }
        }
        return result;

    }
    
    static Dictionary<(int a, int b), double> CalculateOverlaps(Layer layer, int aproximateFeatureCount, Geometry? clip = null, CancellationToken? cancellationToken = null)
    {
        var progressReportBatch = Math.Min(aproximateFeatureCount / 10, 100000);
        
        var features = new Dictionary<(int id, int part), (Geometry geometry, Envelope envelope)>(aproximateFeatureCount);
        
        var featureIndex = 0;
        
        var rTree = new STRtree<(int id, int part)>(aproximateFeatureCount);
        
        if (clip != null)
        {
            layer.SetSpatialFilter(clip);
        }
        
        Feature feature;
        while ((feature = layer.GetNextFeature()) != null)
        {
            featureIndex++;
            using var geom = feature.GetGeometryRef();

            if (geom != null)
            {
                var fid = (int)feature.GetFID();
                
                var clipped = clip == null ? geom.Clone() : geom.Intersection(clip);
                
                if (clipped.IsEmpty() || clipped.GetGeometryType() is not (wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon)) continue;
                
                var subdivided = clipped.SubdivideAndExplode(); 
                
                for (var i = 0; i < subdivided.Count; i++)
                {
                    var polyPart = subdivided[i];
                    var envelope = polyPart.GetNTSEnvelope();
                    rTree.Insert(envelope, (fid, i));
                    features.Add((fid, i), (polyPart, envelope));
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
                
                // if there is no intersection or the only intersection consists of touching borders, we don't need to calculate further 
                if (!geometry.Intersects(otherFeature.geometry) || geometry.Touches(otherFeature.geometry)) continue;
                
                var intersectionArea = geometry.Intersection(otherFeature.geometry).GetArea();

                if (intersectionArea > 0.5)
                {
                    // since we split the geometries, it's possible that there already is an intersection area for this pair of features, so we have to sum it up 
                    var val = pairIntersections.GetValueOrDefault((featureId.id, otherId.id), 0d);
                    pairIntersections[(featureId.id, otherId.id)] = intersectionArea + val;
                }
            }
        }
        // reportSelfOverlapValidationProgress?.Invoke(1);
        return pairIntersections.Where(entry => entry.Value > 1).ToDictionary();
    }

    /// <summary>
    /// Returns the envelope of the geometry as a NetTopologySuite Envelope object.  
    /// This is useful, when working with a NetTopologySuite spatial-index like <see cref="STRtree{TItem}"/> 
    /// </summary>
    static Envelope GetNTSEnvelope(this Geometry geometry)
    {
        using var bbox = new OSGeo.OGR.Envelope();
        geometry.GetEnvelope(bbox);
        return new Envelope(bbox.MinX, bbox.MaxX, bbox.MinY, bbox.MaxY);
    }

    
    /// <summary>
    /// Subdivides the geometry into smaller parts, until each part has no more vertices than the specified max value.
    /// Multipolygons will be exploded into single-part geometries.
    /// The splitting will recursively split the polygon in half, either horizontally or vertically, depending on the dimensions 
    /// </summary>
    /// <param name="geometry">Geometry to subdivide</param>
    /// <param name="maxVertices">The maximum amount of vertices the parts should have</param>
    /// <returns>A list of polygons, each with no more than the specified limit of vertices</returns>
    /// <exception cref="Exception"></exception>
    static List<Geometry> SubdivideAndExplode(this Geometry geometry, int maxVertices = 512)
    {
        var result = new List<Geometry>();
        switch (geometry.GetGeometryType())
        {
            case wkbGeometryType.wkbMultiPolygon:
                var polys = geometry.GetGeometryCount();
                // process each subspolygon separatly
                for (var i = 0; i < polys; i++)
                {
                    result.AddRange(geometry.GetGeometryRef(i).SubdivideAndExplode(maxVertices));
                }
                break;
            // polygons contain a linearring, which then contains the vertices
            case wkbGeometryType.wkbPolygon when geometry.GetGeometryRef(0).GetPointCount() <= maxVertices:
                return [geometry];
            case wkbGeometryType.wkbPolygon:
            {
                using var bbox = new OSGeo.OGR.Envelope();
                geometry.GetEnvelope(bbox);

                var oldWidth = bbox.MaxX - bbox.MinX;
                var oldHeight = bbox.MaxY - bbox.MinY;
                
                // either split horizontally or vertically, depending on the current dimensions of the bounding box
                var (splitsHorizontal, splitsVertical) = oldWidth > oldHeight ? (2, 1) : (1, 2);

                var splitTiles = new List<Geometry>(splitsHorizontal * splitsVertical);
                var width = oldWidth / splitsHorizontal;
                var height = oldHeight / splitsVertical;
                for (var x = bbox.MinX; x < bbox.MaxX; x += width)
                {
                    for (var y = bbox.MinY; y < bbox.MaxY; y += height)
                    {
                        splitTiles.Add(PolygonFromExtent(x, x + width, y, y + height));
                    }
                }
                // recursively subdivide the new split polygons again
                result.AddRange(splitTiles.SelectMany(tile => geometry.Intersection(tile).SubdivideAndExplode(maxVertices)));
                break;
            }
        }
        return result;
    }
    
    /// <summary>
    /// Constructs a polygon from the passed coordinates
    /// </summary>
    private static Geometry PolygonFromExtent(double tileMinX, double tileMaxX, double tileMinY, double tileMaxY)
    {
        var tileGeometry = new Geometry(wkbGeometryType.wkbPolygon);
        var linearRing = new Geometry(wkbGeometryType.wkbLinearRing);
        linearRing.AddPoint_2D(tileMinX, tileMinY);           // Bottom-left corner
        linearRing.AddPoint_2D(tileMaxX, tileMinY);           // Bottom-right corner
        linearRing.AddPoint_2D(tileMaxX, tileMaxY);           // Top-right corner
        linearRing.AddPoint_2D(tileMinX, tileMaxY);           // Top-left corner
        linearRing.AddPoint_2D(tileMinX, tileMinY);           // Bottom-left corner again, to close the ring
        tileGeometry.AddGeometry(linearRing);
        return tileGeometry;
    }
}