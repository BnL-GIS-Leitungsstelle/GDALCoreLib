using OGCToolsNetCoreLib.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OGCGeometryValidatorGUI.Models
{
    public class MessagesModel
    {
        public List<string> MessageLines { get; set; }
        public MessagesModel()
        {
            MessageLines = new List<string>();
        }


        public List<string> GetMessageLines(ProgressModel progressModel)
        {
            MessageLines.Clear();

            LayerValidationResult result = progressModel.ValidationResult;
            MessageLines.Add($"File: {result.LayerName}{Environment.NewLine}");

            if (result.IsValid)
            {
                MessageLines.Add($"  Layer: {result.LayerName} --> OK{Environment.NewLine}");
            }
            else
            {
                MessageLines.Add($"  Layer: {result.LayerName} --> has Errors{Environment.NewLine}");

                // if too many errors occure, then just give a summary
                bool tooManyErrors = 5000 < result.InvalidFeatures.Count;

                if (tooManyErrors)
                {
                    MessageLines.Add($" -  Summary: {result.InvalidFeatures.Count} Errors{Environment.NewLine}");

                    var cntEmptyGeom = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.EmptyGeometry).ToList().Count;

                    MessageLines.Add($"    Empty Geometry: {cntEmptyGeom} Errors{Environment.NewLine}");

                    var cntGeomCounterZero = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.GeometryCounterZero).ToList().Count;

                    MessageLines.Add($"    Geometry-Counter = Zero: {cntGeomCounterZero} Errors{Environment.NewLine}");

                    var cntRingSelfIntersects = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.RingSelfIntersects).ToList().Count;

                    MessageLines.Add($"    Ring Self-Intersects: {cntRingSelfIntersects} Errors{Environment.NewLine}");

                    var cntGeometrytypeMismatch = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.GeometrytypeMismatchAccordingToLayer).ToList().Count;

                    MessageLines.Add($"    Geometrytype-Mismatch according to Layer: {cntGeometrytypeMismatch} Warnings{Environment.NewLine}");

                    var cntMultiSurfaceType = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.FeatureToLayerMultiSurfaceTypeMismatch).ToList().Count;

                    MessageLines.Add($"    Feature to Layer MultiSurface-Type mismatch: {cntMultiSurfaceType} Warnings{Environment.NewLine}");


                    var cntNonSimpleGeometry = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.NonSimpleGeometry).ToList().Count;

                    MessageLines.Add($"    Non-simple Geometry: {cntNonSimpleGeometry} Errors{Environment.NewLine}");

                    var cntRepeatedPoints = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.RepeatedPoints).ToList().Count;

                    MessageLines.Add($"    Repeated Points: {cntRepeatedPoints} Errors{Environment.NewLine}");

                    var cntInvalidGeometryUnspecifiedReason = result.InvalidFeatures.Where(_ => _.ValidationResultType == EGeometryValidationType.InvalidGeometryUnspecifiedReason).ToList().Count;

                    MessageLines.Add($"    Invalid Geometry by unspecified Reason: {cntInvalidGeometryUnspecifiedReason} Errors{Environment.NewLine}");
                }
                else
                {
                    foreach (var item in result.InvalidFeatures)
                    {
                        MessageLines.Add($"     {item.ErrorLevel}: Oid= {item.FeatureFid}, Name= {item.Name}, ObjNummer= {item.ObjNummer}, {item.ValidationResultType} {item.Remarks}{Environment.NewLine}");
                    }
                }
            }

            return MessageLines;
        }



    }
}
