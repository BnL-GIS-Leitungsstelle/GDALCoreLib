using System;
using System.Threading;
using OGCToolsNetCoreLib.DataAccess;
using OSGeo.OGR;

namespace OGCToolsNetCoreLib.Extensions
{
    public static class LayerExtensions
    {

        public static void SetFeatureAvoidDbLock(this OSGeo.OGR.Layer layer, OSGeo.OGR.Feature feature)
        {
            var setFeatureSuccess = false;
            while (!setFeatureSuccess)
            {
                try
                {
                    layer.SetFeature(feature);
                    setFeatureSuccess = true;
                }
                catch (ApplicationException) { Thread.Sleep(1); }
            }
        }


        public static void CreateFeatureAvoidDbLock(this OSGeo.OGR.Layer layer, OSGeo.OGR.Feature feature)
        {
            var createFeatureSuccess = false;
            while (!createFeatureSuccess)
            {
                try
                {
                    layer.CreateFeature(feature);
                    createFeatureSuccess = true;
                }
                catch (ApplicationException ex)
                {
                    // log.Error(ex);
                    Thread.Sleep(1);
                }
            }
        }


        public static void DeleteFeatureAvoidDbBlock(this OSGeo.OGR.Layer layer, long fid)
        {
            var deleteFeatureSuccess = false;
            while (!deleteFeatureSuccess)
            {
                try
                {
                    layer.DeleteFeature(fid);
                    deleteFeatureSuccess = true;
                }
                catch (ApplicationException) { Thread.Sleep(1); }
            }
        }


        //public static List<FieldDefnInfo> GetLayerFieldInfo(this OSGeo.OGR.Layer layer)
        //{
        //    var result = new List<FieldDefnInfo>();

        //    for (int j = 0; j < layer.GetLayerDefn().GetFieldCount(); j++)
        //    {
        //        using (var field = layer.GetLayerDefn().GetFieldDefn(j))
        //        {
        //            result.Add(new FieldDefnInfo(field));
        //        }
        //    }
        //    return result;
        //}

        /// <summary>
        /// Searches for underlying database column being used as geometry column.
        /// If no column is found or not supported by the datasource-driver, no geometry will be indicated
        /// TODO: Double-check, if datasource-driver supports this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns>true, if underlying database column being used as the geometry column is found</returns>
        public static bool HasGeometry(this OSGeo.OGR.Layer layer, SupportedDatasource supportedDatasource)
        {
            bool isTable = layer.GetGeomType() == wkbGeometryType.wkbNone;

            if (isTable) return false;
            if (supportedDatasource.Type == EDataSourceType.SHP) return true;

            string geomColumn = layer.GetGeometryColumn();
            return !String.IsNullOrWhiteSpace(geomColumn);
        }

        /// <summary>
        /// Searches for underlying database column being used as geometry column.
        /// If no column is found or not supported by the datasource-driver, no geometry will be indicated
        /// TODO: Double-check, if datasource-driver supports this method
        /// </summary>
        /// <param name="layer"></param>
        /// <returns>true, if underlying database column being used as the geometry column is found</returns>
        public static bool IsGeometryType(this OSGeo.OGR.Layer layer)
        {
            bool isGeometryType = layer.GetGeomType() != wkbGeometryType.wkbNone;
            return isGeometryType;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="layer"></param>
        public static bool IsMultiSurface(this OSGeo.OGR.Layer layer)
        {
            var geomType = layer.GetGeomType();
            return geomType.ToString().StartsWith(wkbGeometryType.wkbMultiSurface.ToString());
        }

    }
}
