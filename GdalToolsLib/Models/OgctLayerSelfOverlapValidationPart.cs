using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GdalToolsLib.Geometry;
using NetTopologySuite.Index.Strtree;
using Envelope = NetTopologySuite.Geometries.Envelope;

namespace GdalToolsLib.Models;


public partial class OgctLayer
{
    public async Task<IList<SelfOverlapErrorResult>> ValidateSelfOverlapAsync(
        Action<double> reportSelfOverlapValidationProgress = null, CancellationToken? cancellationToken = null)
    {
        if (!IsGeometryType())
        {
            return new List<SelfOverlapErrorResult>();
        }
        var features = new Dictionary<long, (IOgctGeometry geometry, Envelope envelope)>();
        
        OgctFeature feature;
        var featureIndex = 0;
        var featureCount = LayerDetails.FeatureCount;

        _layer.ResetReading();
        
        var rTree = new STRtree<long>();
        while ((feature = OpenNextFeature()) != null)
        {
            var geom = feature.OpenGeometry().CloneAndOpen();
            if (geom != null)
            {
                var bbox = geom.GetBoundingBox();
                var envelope = new Envelope(bbox.X, bbox.X + bbox.Width, bbox.Y, bbox.Y + bbox.Height);
                rTree.Insert(envelope, feature.FID); 
                features.Add(feature.FID, (geom, envelope));
            }
            feature.Dispose();
        }
        rTree.Build();
        _isValidatingFeatures = true;

        // to store the area of intersection for each pair of features
        var pairIntersections = new Dictionary<(long a, long b), double>();
        
        var progressReportBatch = Math.Min(featureCount / 10, 10000);
        
        foreach (var (featureId, (geometry, envelope)) in features)
        {
            if (++featureIndex % progressReportBatch == 0)
            {
                cancellationToken?.ThrowIfCancellationRequested();
                reportSelfOverlapValidationProgress?.Invoke((double)featureIndex / featureCount); // report partial progress for feature read
            }
            var potentialIntersections = rTree.Query(envelope);
            foreach (var otherId in potentialIntersections)
            {
                // if this feature pair has already been intersected the other way around, we can skip it here
                if (otherId == featureId || pairIntersections.ContainsKey((otherId, featureId))) continue;
                var otherFeature = features[otherId];
                
                // if there is no intersection or the only intersection consists of touching borders, we don't need to calculate further 
                if (!geometry.Intersects(otherFeature.geometry) || geometry.Touches(otherFeature.geometry)) continue;
                
                var intersectionArea = geometry.GetAndOpenIntersection(otherFeature.geometry).Area;

                if (intersectionArea > 1 || intersectionArea >= geometry.Area * 0.03 || intersectionArea >= otherFeature.geometry.Area * 0.03)
                {
                    pairIntersections.Add((featureId, otherId), intersectionArea);
                }
            }
        }
        reportSelfOverlapValidationProgress?.Invoke(1);
        return pairIntersections.Select(x => new SelfOverlapErrorResult(x.Key.a, x.Key.b, x.Value)).ToList();
    }
}