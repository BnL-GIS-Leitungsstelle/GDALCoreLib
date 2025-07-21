using GdalToolsLib.Geometry;
using NetTopologySuite.Index.Strtree;
using OSGeo.OGR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Envelope = NetTopologySuite.Geometries.Envelope;

namespace GdalToolsLib.Models;

public partial class OgctLayer
{
    public async Task<IList<SelfOverlapErrorResult>> ValidateSelfOverlapAsync(
        Action<double> reportSelfOverlapValidationProgress = null,
        CancellationToken? cancellationToken = null)
    {
        if (LayerDetails.GeomType is not (wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon)) throw new Exception("Only polygons are supported.");

        _layer.ResetReading();
        var featureCount = (int)LayerDetails.FeatureCount;
        Dictionary<(int a, int b), double> selfOverlaps = new();
        (object LockObj, double Weight, double CurrProgress, Action<double>? ProgressAction) progress = (new object(), 1, 0, reportSelfOverlapValidationProgress);

        if (featureCount <= 1_000_000)
        {
            await Task.Run(() =>
            {
                selfOverlaps = CalculateOverlaps(_layer, featureCount, ref progress);
            });
        }
        else
        {
            using var bbox = new OSGeo.OGR.Envelope();
            _layer.GetExtent(bbox, 0);

            // number of rows and columns for the tiles to process in parallel
            const int rows = 20;
            const int cols = 20;
            progress.Weight = 1d / (rows * cols);

            var tileWidth = (bbox.MaxX - bbox.MinX) / cols;
            var tileHeight = (bbox.MaxY - bbox.MinY) / rows;

            var tileResults = new ConcurrentBag<Dictionary<(int a, int b), double>>();
            var featuresPerTileApproximation = featureCount / rows / cols * 2;

            await Task.Run(() =>
            {
                var parallelOptions = new ParallelOptions()
                {
                    CancellationToken = cancellationToken ?? CancellationToken.None,
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };
                Parallel.For(0, rows * cols, parallelOptions, (i) =>
                {
                    var row = i / cols;
                    var col = i % cols;

                    // calculate the bounds of the current tile
                    var tileMinX = bbox.MinX + col * tileWidth;
                    var tileMinY = bbox.MinY + row * tileHeight;
                    var tileMaxX = tileMinX + tileWidth;
                    var tileMaxY = tileMinY + tileHeight;
                    var tileGeometry = PolygonFromExtent(tileMinX, tileMaxX, tileMinY, tileMaxY);

                    using var dsRef = new OgctDataSourceAccessor().OpenOrCreateDatasource(LayerDetails.DataSourceFileName);
                    using var layerRef = (OgctLayer)dsRef.OpenLayer(LayerDetails.Name);

                    tileResults.Add(CalculateOverlaps(layerRef.OgrLayer, featuresPerTileApproximation, ref progress, tileGeometry));
                });
            }, cancellationToken ?? CancellationToken.None);

            selfOverlaps = AggregateTileResults(tileResults);
        }

        reportSelfOverlapValidationProgress?.Invoke(1);

        return selfOverlaps
            .Select(e => new SelfOverlapErrorResult(e.Key.a, e.Key.b, e.Value))
            .ToList();
    }

    private Dictionary<(int a, int b), double> CalculateOverlaps(
        OSGeo.OGR.Layer layer, 
        int aproximateFeatureCount,
        ref (object LockObj, double Weight, double CurrProgress, Action<double>? ProgressAction) progress,
        OSGeo.OGR.Geometry? clip = null,
        CancellationToken? cancellationToken = null)
    {
        var progressReportBatch = Math.Max(Math.Min(aproximateFeatureCount / 10, 10_000), 100);

        var features = new Dictionary<(int id, int part), (OSGeo.OGR.Geometry geometry, Envelope envelope)>(aproximateFeatureCount);
        var rTree = new STRtree<(int id, int part)>(aproximateFeatureCount);

        if (clip != null)
        {
            layer.SetSpatialFilter(clip);
        }

        OSGeo.OGR.Feature feature;
        while ((feature = layer.GetNextFeature()) != null)
        {
            using var geom = feature.GetGeometryRef();

            if (geom != null)
            {
                var fid = (int)feature.GetFID();

                var clipped = clip == null ? geom.Clone() : geom.Intersection(clip);

                if (clipped.IsEmpty() || clipped.GetGeometryType() is not (wkbGeometryType.wkbPolygon or wkbGeometryType.wkbMultiPolygon)) continue;

                var subdivided = SubdivideAndExplode(clipped);

                for (var i = 0; i < subdivided.Count; i++)
                {
                    var polyPart = subdivided[i];
                    var envelope = GetNTSEnvelope(polyPart);
                    rTree.Insert(envelope, (fid, i));
                    features.Add((fid, i), (polyPart, envelope));
                }
            }
            feature.Dispose();

            cancellationToken?.ThrowIfCancellationRequested();
        }

        rTree.Build();

        // to store the area of intersection for each pair of features
        var pairIntersections = new Dictionary<(int a, int b), double>();

        var featureIndex = 0;
        foreach (var (featureId, (geometry, envelope)) in features)
        {
            cancellationToken?.ThrowIfCancellationRequested();
            if (++featureIndex % progressReportBatch == 0 ||
                featureIndex == features.Count - 1)
            {
                lock (progress.LockObj)
                {
                    var batchSize = featureIndex % progressReportBatch == 0 ? progressReportBatch : (featureIndex % progressReportBatch + 1);
                    var increment = (double)batchSize / features.Count * progress.Weight;
                    progress.CurrProgress += increment;
                    progress.ProgressAction?.Invoke(progress.CurrProgress);
                }
            }

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

        // if no features in tile --> report fill weight at once
        if (!features.Any())
        {
            lock (progress.LockObj)
            {
                progress.CurrProgress += progress.Weight;
                progress.ProgressAction?.Invoke(progress.CurrProgress);
            }
        }
        
        return pairIntersections.Where(entry => entry.Value > 1).ToDictionary();
    }

    private Dictionary<(int a, int b), double> AggregateTileResults(IEnumerable<Dictionary<(int a, int b), double>> tileResults)
    {
        var aggregatedResults = new Dictionary<(int a, int b), double>();

        // sum up the overlaps from different tiles
        foreach (var overlaps in tileResults)
        {
            foreach (var (idPair, area) in overlaps)
            {
                // if the id pair already exists, it means it came from a different tile, so we need to sum the intresection area
                if (aggregatedResults.TryGetValue(idPair, out var val) ||
                    aggregatedResults.TryGetValue((idPair.b, idPair.a), out val)) // check for reversed order as well
                {
                    aggregatedResults[idPair] = val + area;
                }
                else
                {
                    aggregatedResults.Add(idPair, area);
                }
            }
        }

        return aggregatedResults;
    }

    /// <summary>
    /// Returns the envelope of the geometry as a NetTopologySuite Envelope object.  
    /// This is useful, when working with a NetTopologySuite spatial-index like <see cref="STRtree{TItem}"/> 
    /// </summary>
    private Envelope GetNTSEnvelope(OSGeo.OGR.Geometry geometry)
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
    static List<OSGeo.OGR.Geometry> SubdivideAndExplode(OSGeo.OGR.Geometry geometry, int maxVertices = 512)
    {
        var result = new List<OSGeo.OGR.Geometry>();
        switch (geometry.GetGeometryType())
        {
            case wkbGeometryType.wkbMultiPolygon:
                var polys = geometry.GetGeometryCount();
                // process each subspolygon separatly
                for (var i = 0; i < polys; i++)
                {
                    result.AddRange(SubdivideAndExplode(geometry.GetGeometryRef(i)));
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

                    var splitTiles = new List<OSGeo.OGR.Geometry>(splitsHorizontal * splitsVertical);
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
                    result.AddRange(splitTiles.SelectMany(tile => SubdivideAndExplode(geometry.Intersection(tile))));
                    break;
                }
        }
        return result;
    }

    /// <summary>
    /// Constructs a polygon from the passed coordinates
    /// </summary>
    private static OSGeo.OGR.Geometry PolygonFromExtent(double tileMinX, double tileMaxX, double tileMinY, double tileMaxY)
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
}