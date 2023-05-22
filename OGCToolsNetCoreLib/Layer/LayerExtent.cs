using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Layer
{
    public class LayerExtent
    {
        public double MinX { get; }
        public double MaxX { get; }
        public double MinY { get; }
        public double MaxY { get; }

        /// <summary>
        /// true, if all values are 0
        /// </summary>
        public bool HasExtent { get; }

        [JsonIgnore]
        public string Json { get; set; }

        /// <summary>
        /// compared extents will be handled as equal, if the differences are below the tolerance
        /// </summary>
        private double _compareTolerance = 0.05;

        public LayerExtent(OSGeo.OGR.Layer layer)
        {
            using (var env = new Envelope())
            {
                layer.GetExtent(env, 0);
                MinX = env.MinX;
                MaxX = env.MaxX;
                MinY = env.MinY;
                MaxY = env.MaxY;
            }

            HasExtent = MaxX - MinX > 0 && MaxY - MinY > 0;

            Json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        }

        public bool IsEqual(LayerExtent otherExtent)
        {
            return Compare(otherExtent) == String.Empty;
        }

        public string Compare(LayerExtent otherExtent)
        {
            string result= String.Empty;

            double minXDiff = Math.Abs(MinX - otherExtent.MinX);
            double maxXDiff = Math.Abs(MaxX - otherExtent.MaxX);
            double minYDiff = Math.Abs(MinY - otherExtent.MinY);
            double maxYDiff = Math.Abs(MaxY - otherExtent.MaxY);

            if (minXDiff >= _compareTolerance) result += $"minX-difference is {minXDiff:F3} cm. ";
            if (maxXDiff >= _compareTolerance) result += $"maxX-difference is {maxXDiff:F3} cm. ";
            if (minYDiff >= _compareTolerance) result += $"minY-difference is {minYDiff:F3} cm. ";
            if (maxYDiff >= _compareTolerance) result += $"maxY-difference is {maxYDiff:F3} cm. ";

            return result;
        }
    }
}
