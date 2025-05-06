using GdalToolsLib.Models;
using OSGeo.OGR;

namespace GdalToolsLib.Layer;

public class LayerDetails
{
    public string? DataSourceFileName { get; private set; }

    public string? Name { get; private set; }

    public wkbGeometryType GeomType { get; private set; }

    public string FidColumnName  { get; private set; } 

    public ELayerType LayerType { get; private set; }


    // public SpatialReference SpatialRef { get; private set; }

    public LayerSpatialRef? Projection { get; set; }

    public LayerExtent? Extent { get; set; }

    public LayerSchema? Schema { get; set; }

    public int FieldCount { get; private set; }

    public long FeatureCount { get; private set; }

    /// <summary>
    /// Set a new attribute query.
    /// This function sets the attribute query string to be used when fetching features via the OGR_L_GetNextFeature() function.
    /// Only features for which the query evaluates as true will be returned.
    /// The query string should be in the format of an SQL WHERE clause.For instance "population > 1000000 and population < 5000000" where
    /// population is an attribute in the layer.
    /// The query format is a restricted form of SQL WHERE clause as defined
    /// "eq_format=restricted_where" about half way through this document:
    /// http://ogdi.sourceforge.net/prop/6.2.CapabilitiesMetadata.html
    /// Note that installing a query string will generally result in resetting the current reading position (ala OGR_L_ResetReading()).
    /// </summary>
    public string? AttributeFilter { get; private set; }


    /// <summary>
    /// return widely used layer properties
    /// </summary>
    /// <param name="file"></param>
    /// <param name="layerName"></param>
    /// <param name="attributeFilter">a kind of WHERE-clause</param>
    public LayerDetails(string? file, string? layerName, string? attributeFilter = default!)
    {
        using (var ds = new OgctDataSourceAccessor().OpenOrCreateDatasource(file))
        {
            using (var layer = ds.OpenLayer(layerName))
            {
                Initialize(file, layer, attributeFilter);
            }
        }
    }
    /// <summary>
    /// return widely used layer properties
    /// </summary>
    /// <param name="dataSource"></param>
    /// <param name="layerName"></param>
    /// <param name="attributeFilter">a kind of WHERE-clause</param>
    public LayerDetails(OgctDataSource dataSource, string? layerName, string? attributeFilter = default)
    {
        using (var layer = dataSource.OpenLayer(layerName))
        {
            Initialize(dataSource.Name, layer, attributeFilter);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="layerName"></param>
    /// <param name="projection"></param>
    /// <param name="geometryType"></param>
    public LayerDetails(string? layerName, string projection, wkbGeometryType geometryType)
    {
        Name = layerName;
        Projection = new LayerSpatialRef(projection);
        GeomType = geometryType;
        Schema = new LayerSchema();
    }

    /// <summary>
    /// layer stays open, it is not de-referenced
    /// </summary>
    /// <param name="layer"></param>
    /// <param name="attributeFilter"></param>
    public LayerDetails(IOgctLayer layer, string? attributeFilter = default)
    {
        Initialize(layer.DataSource.Name, layer, attributeFilter);
    }

    private void Initialize(string? file, IOgctLayer layer, string? attributeFilter)
    {
        var ogrLayer = ((OgctLayer)layer).OgrLayer;
        GeomType = ogrLayer.GetLayerDefn().GetGeomType();
        FidColumnName =ogrLayer.GetFIDColumn();
        SetLayerType();
        //SpatialRef = layer.GetSpatialRef();
        Projection = new LayerSpatialRef(ogrLayer);
        DataSourceFileName = file;
        Name = ogrLayer.GetName();
        FieldCount = ogrLayer.GetLayerDefn().GetFieldCount();
        FeatureCount = ogrLayer.GetFeatureCount(1);
        AttributeFilter = attributeFilter;
        Extent = new LayerExtent(ogrLayer);
        Schema = new LayerSchema(ogrLayer);
    }

    /// <summary>
    /// set LayerType according to type of geometry
    /// </summary>
    private void SetLayerType()
    {
        LayerType = ELayerType.All;

        var geomType = GeomType.ToString();

        if (geomType.StartsWith("wkbNone"))
        {
            LayerType = ELayerType.Table;
        }
        if (geomType.StartsWith("wkbMultiPolygon") || geomType.StartsWith("wkbPolygon"))
        {
            LayerType = ELayerType.Polygon;
        }
        if (geomType.StartsWith("wkbMultiLineString") || geomType.StartsWith("wkbLineString"))
        {
            LayerType = ELayerType.Polyline;
        }
        if (geomType.StartsWith("wkbMultiPoint") || geomType.StartsWith("wkbPoint"))
        {
            LayerType = ELayerType.Point;
        }
    }

    public override string ToString()
    {
        return $"Layer {Name}, Type {GeomType}, Projection {Projection.Name}";
    }
}