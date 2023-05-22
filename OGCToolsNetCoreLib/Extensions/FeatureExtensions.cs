using System;
using System.IO;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Extensions
{
    public static class FeatureExtensions
    {
        // public static Logger log;

        /// <summary>
        /// returns the content of the field 'ObjNummer'
        /// <para>if field 'ObjNummer' isn't present, returns empty string</para>
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static string ObjNumber(this OSGeo.OGR.Feature feature, OSGeo.OGR.Layer layer)
        {
            return GetFeatureFieldAsString(feature, layer, "ObjNummer");
        }

        /// <summary>
        /// returns the content of the field 'Name'
        /// <para>if field 'Name' isn't present, returns empty string</para>
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="layer"></param>
        /// <returns></returns>
        public static string ObjName(this OSGeo.OGR.Feature feature, OSGeo.OGR.Layer layer)
        {
            return GetFeatureFieldAsString(feature, layer, "Name");
        }

        private static string GetFeatureFieldAsString(OSGeo.OGR.Feature feature, OSGeo.OGR.Layer layer, string field)
        {
            var idx = layer.FindFieldIndex(field, 1);
            return idx < 0 ? string.Empty : feature.GetFieldAsString(idx);
        }


        /// <summary>
        /// creates a full copy from a given feature incl. the content of fields with equal names.
        /// the geometry can be exchanges (e.g. to save the results of geometric processes)
        /// </summary>
        /// <param name="feature">must be initialized with FieldDefn from source feature</param>
        /// <param name="fields"></param>
        /// <param name="sourceFeature"></param>
        /// <param name="inGeom">if not the geometry of the source-feature</param>
        /// <returns></returns>
        public static void CreateFromOther(this OSGeo.OGR.Feature feature, FeatureDefn fields, OSGeo.OGR.Feature sourceFeature, OSGeo.OGR.Geometry inGeom = null)
        {
            var fid = sourceFeature.GetFID();

            if (inGeom == null)
            {
                inGeom = sourceFeature.GetGeometryRef().Clone();  // new geometry         
            }
            //inGeom.ExportToWkt(out string wkt);

            if (!inGeom.IsValid())
            {
                //log.Warn($" Invalid geometry {fid}");
                //throw new InvalidDataException($" Invalid geometry {fid}");
            }

            wkbGeometryType inGeomType = inGeom.GetGeometryType();

            OSGeo.OGR.Geometry outGeom = null;
            OSGeo.OGR.Geometry outGeomValid = null;
            OSGeo.OGR.Geometry outGeomClean = null;

            switch (inGeomType)
            {
                case wkbGeometryType.wkbGeometryCollection:
                case wkbGeometryType.wkbLineString:
                case wkbGeometryType.wkbPolygon:
                    outGeom = Ogr.ForceToMultiPolygon(inGeom);
                    // MakeValid()
                    // Attempts to make an invalid geometry valid without losing vertices. 
                    // details: https://gdal.org/doxygen/classOGRGeometry.html#a700a2d4b1c719e1f65fa3009bfc04f78
                    // papszOptions	NULL terminated list of options, or NULL. The following options are available:
                    // METHOD = LINEWORK / STRUCTURE.LINEWORK is the default method, which combines all rings into a set of noded lines and then extracts valid polygons from that linework. The STRUCTURE method(requires GEOS >= 3.10 and GDAL >= 3.4) first makes all rings valid, then merges shells and subtracts holes from shells to generate valid result. Assumes that holes and shells are correctly categorized.
                    // KEEP_COLLAPSED = YES / NO.Only for METHOD = STRUCTURE.NO(default): collapses are converted to empty geometries YES: collapses are converted to a valid geometry of lower dimension.

                    outGeomValid = outGeom.MakeValid(null); 
                    outGeomClean = outGeomValid.RemoveLowerDimensionSubGeoms();
                    break;

                default:
                    break; // conversion not needed
            }

            if (outGeom != null)
            {
                wkbGeometryType outGeomType = outGeomClean.GetGeometryType();
                feature.SetGeometry(outGeomClean);
                outGeomClean.Dispose();
                outGeomValid.Dispose();
                outGeom.Dispose();
            }
            else
            {
                feature.SetGeometry(inGeom);
            }

            inGeom.Dispose();

            feature.SetFID(fid);
            feature.CopyFieldContent(fields, sourceFeature);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="fields"></param>
        /// <param name="sourceFeature"></param>
        private static void CopyFieldContent(this OSGeo.OGR.Feature feature, FeatureDefn fields, OSGeo.OGR.Feature sourceFeature)
        {
            // Set fields for copied feature
            for (int j = 0; j < fields.GetFieldCount(); j++)
            {
                FieldDefn field = fields.GetFieldDefn(j);
                string fieldName = field.GetName();
                var type = field.GetFieldType();

                //if (fieldName == "Shape_Area" || fieldName == "Shape_Length")
                //{
                //    continue;
                //}

                switch (field.GetFieldType())
                {
                    case FieldType.OFTInteger:
                        feature.SetField(fieldName, sourceFeature.GetFieldAsInteger(fieldName));
                        break;

                    case FieldType.OFTInteger64:
                        feature.SetField(fieldName, sourceFeature.GetFieldAsInteger64(fieldName));
                        break;

                    case FieldType.OFTReal:
                        feature.SetField(fieldName, sourceFeature.GetFieldAsDouble(fieldName));
                        break;

                    case FieldType.OFTDateTime:
                        sourceFeature.GetFieldAsDateTime(fieldName, out var year, out var month, out var day, out var hour,
                            out var minute, out var second, out var tzflag);

                        // update content, if date is valid (not <null>)
                        if (year != 0 && month != 0 && day != 0)
                        {
                            feature.SetField(fieldName, year, month, day, hour, minute, second, tzflag);
                        }
                        break;

                    case FieldType.OFTString:
                        feature.SetField(fieldName, sourceFeature.GetFieldAsString(fieldName));
                        break;

                    default:
                        feature.SetField(fieldName, sourceFeature.GetFieldAsString(fieldName));
                        break;
                }
            }
        }
    }
}
