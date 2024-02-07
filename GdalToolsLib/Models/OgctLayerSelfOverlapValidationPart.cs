using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GdalToolsLib.Geometry;

namespace GdalToolsLib.Models;

public partial class OgctLayer
{
    private readonly int _maxSelfOverlapTasksToEnqueue = ThreadPool.ThreadCount * 6;

    private record SelfOverlapValidationTask(long FidA, long FidB, IOgctGeometry GeomA, IOgctGeometry GeomB) : IDisposable
    {
        public void Dispose()
        {
            GeomA.Dispose();
            GeomB.Dispose();
        }
    }
    private SelfOverlapErrorResult ValidateFeatureOverlap(SelfOverlapValidationTask task)
    {
        if (!task.GeomA.Intersects(task.GeomB))
        {
            task.Dispose();
            return null;
        }

        using var intersection = task.GeomA.GetAndOpenIntersection(task.GeomB);

        if (intersection?.Area > 1 || intersection?.Area >= task.GeomA.Area * 0.03 || intersection?.Area >= task.GeomB.Area * 0.03)
        {
            task.Dispose();
            return new SelfOverlapErrorResult(task.FidA, task.FidB, intersection.Area);
        }


        task.Dispose();
        return null;
    }

    private async Task EnqueueSelfOverlapTasksForFeatureWithComparisons(long baseFeatureId, List<long> featureIdsToAnalyze, ConcurrentBag<Task<SelfOverlapErrorResult>> tasks, CancellationToken? cancellationToken = null)
    {
        using var baseFeature = OpenFeatureByFid(baseFeatureId);
        using var baseFeatureGeometry = baseFeature.OpenGeometry();


        foreach (var candidateFeatureId in featureIdsToAnalyze)
        {
            if (cancellationToken?.IsCancellationRequested ?? false)
            {
                break;
            }
            if (candidateFeatureId == baseFeatureId)
            {
                continue;
            }

            using var candidateFeature = OpenFeatureByFid(candidateFeatureId);
            using var candidateFeatureGeometry = candidateFeature.OpenGeometry();


            while (tasks.Count >= _maxSelfOverlapTasksToEnqueue - 10) //wait until we can queue a few tasks
            {
                Thread.Yield();
            }

            var baseGeometryCopy = baseFeatureGeometry.CloneAndOpen();
            var comparisonGeometryCopy = candidateFeatureGeometry.CloneAndOpen();
            tasks.Add(Task.Run(() => ValidateFeatureOverlap(new SelfOverlapValidationTask(baseFeatureId, candidateFeatureId, baseGeometryCopy, comparisonGeometryCopy))));
        }
    }

    public async Task<IList<SelfOverlapErrorResult>> ValidateSelfOverlapAsync(
        Action<double> reportSelfOverlapValidationProgress = null, CancellationToken? cancellationToken = null)
    {
        if (!IsGeometryType())
        {
            return new List<SelfOverlapErrorResult>();
        }
        var fidVsBoundingRect = new Dictionary<long, Rectangle>();

        var featureIds = new ConcurrentBag<long>();

        OgctFeature feature;
        var featureIndex = 0;
        var featureCount = LayerDetails.FeatureCount;

        _layer.ResetReading();

        while ((feature = OpenNextFeature()) != null)
        {
            if (++featureIndex % 10000 == 0)
            {
                reportSelfOverlapValidationProgress?.Invoke((double)featureIndex / featureCount / 10d); // report partial progress for feature read
            }


            featureIds.Add(feature.FID);
            using var geom = feature.OpenGeometry();
            if (geom != null)
            {
                fidVsBoundingRect.Add(feature.FID, geom.GetBoundingBox()); 
            }
            feature.Dispose();
        }

        if (fidVsBoundingRect.Values.Count == 0)
        {
            return new List<SelfOverlapErrorResult>();
        }
        var minX = fidVsBoundingRect.Values.Min(rect => rect.Left);
        var minY = fidVsBoundingRect.Values.Min(rect => rect.Top);
        var maxX = fidVsBoundingRect.Values.Max(rect => rect.Right) + 1; // add 1 to make sure no feature actually reatures the borders
        var maxY = fidVsBoundingRect.Values.Max(rect => rect.Bottom) + 1;

        double width = maxX - minX;
        double height = maxY - minY;

        var desiredNumberOfBins = Math.Max(featureIds.Count / 10, 1);
        var numberOfBinsPerSide = (int)Math.Ceiling(Math.Sqrt(desiredNumberOfBins));

        var widthStep = width / numberOfBinsPerSide;
        var heightStep = height / numberOfBinsPerSide;

        var bins = new List<long>[numberOfBinsPerSide, numberOfBinsPerSide];

        for (var x = 0; x < numberOfBinsPerSide; x++)
        {
            for (var y = 0; y < numberOfBinsPerSide; y++)
            {
                bins[x, y] = new List<long>();
            }
        }

        foreach (var featureId in featureIds)
        {
            var rect = fidVsBoundingRect[featureId];
            var leftAdjusted = rect.Left - minX;
            var rightAdjusted = rect.Right - minX;
            var bottomAdjusted = rect.Bottom - minY;
            var topAdjusted = rect.Top - minY;

            var leftBin = (int)Math.Floor(leftAdjusted / widthStep);
            var rightBin = (int)Math.Floor(rightAdjusted / widthStep);

            var bottomBin = (int)Math.Floor(bottomAdjusted / heightStep);
            var topBin = (int)Math.Floor(topAdjusted / heightStep);

            for (var x = leftBin; x <= rightBin; x++)
            {
                for (var y = bottomBin; y <= topBin; y++)
                {
                    bins[x, y].Add(featureId);
                }
            }
        }

        _isValidatingFeatures = true;
        var tasks = new ConcurrentBag<Task<SelfOverlapErrorResult>>();
        var results = new ConcurrentBag<SelfOverlapErrorResult>();
        var processingTask = Task.Run(() => ProcessOverlapResultsAsync(tasks, results, cancellationToken));
        var binIndex = 0;
        foreach (var bin in bins)
        {
            reportSelfOverlapValidationProgress?.Invoke((double)binIndex++ / bins.Length);
            if (cancellationToken?.IsCancellationRequested ?? false)
            {
                break;
            }

            foreach (var featureId in bin)
            {
                await EnqueueSelfOverlapTasksForFeatureWithComparisons(featureId, bin.ToList(), tasks,
                    cancellationToken);
            }

        }
        _isValidatingFeatures = false;

        await processingTask;

        return results.Distinct(new SelfOverlapErrorResultEqualityComparer()).ToList();

    }


    private async Task ProcessOverlapResultsAsync(ConcurrentBag<Task<SelfOverlapErrorResult>> tasks,
        ConcurrentBag<SelfOverlapErrorResult> results, CancellationToken? cancellationToken = null)
    {
        while (!(cancellationToken?.IsCancellationRequested ?? false))
        {
            while (tasks.TryTake(out var taskToProcess))
            {
                var result = await taskToProcess;
                if (result != null)
                {
                    results.Add(result);
                }
            }

            if (tasks.IsEmpty && !_isValidatingFeatures)
            {
                break;
            }
        }
    }
}