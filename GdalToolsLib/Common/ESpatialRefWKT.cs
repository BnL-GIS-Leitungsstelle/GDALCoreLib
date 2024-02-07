using System.ComponentModel;

namespace OGCToolsNetCoreLib.Common
{
    public enum ESpatialRefWKT
    {

        [Description("COMPD_CS[\"CH1903 + / LV95 + LN02 height\",PROJCS[\"CH1903 + / LV95\",GEOGCS[\"CH1903 + \",DATUM[\"CH1903 + \",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],AUTHORITY[\"EPSG\",\"6150\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4150\"]],PROJECTION[\"Hotine_Oblique_Mercator_Azimuth_Center\"],PARAMETER[\"latitude_of_center\",46.9524055555556],PARAMETER[\"longitude_of_center\",7.43958333333333],PARAMETER[\"azimuth\",90],PARAMETER[\"rectified_grid_angle\",90],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",2600000],PARAMETER[\"false_northing\",1200000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"2056\"]],VERT_CS[\"LN02 height\",VERT_DATUM[\"Landesnivellement 1902\",2005,AUTHORITY[\"EPSG\",\"5127\"]],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Gravity - related height\",UP],AUTHORITY[\"EPSG\",\"5728\"]]]")]
        CH1903plus_LV95plusLN02height,
        /// <remarks/>
        [Description("PROJCS[\"CH1903+ / LV95\",GEOGCS[\"CH1903+\",DATUM[\"CH1903+\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128,AUTHORITY[\"EPSG\",\"7004\"]],TOWGS84[674.374,15.056,405.346,0,0,0,0],AUTHORITY[\"EPSG\",\"6150\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4150\"]],PROJECTION[\"Hotine_Oblique_Mercator_Azimuth_Center\"],PARAMETER[\"latitude_of_center\",46.95240555555556],PARAMETER[\"longitude_of_center\",7.439583333333333],PARAMETER[\"azimuth\",90],PARAMETER[\"rectified_grid_angle\",90],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",2600000],PARAMETER[\"false_northing\",1200000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"2056\"]]")]
        CH1903plus_LV95,
                
        [Description("PROJCS[\"CH1903 / LV03\", GEOGCS[\"CH1903\", DATUM[\"CH1903\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128, AUTHORITY[\"EPSG\",\"7004\"]], AUTHORITY[\"EPSG\",\"6149\"]], PRIMEM[\"Greenwich\",0, AUTHORITY[\"EPSG\",\"8901\"]], UNIT[\"degree\",0.0174532925199433, AUTHORITY[\"EPSG\",\"9122\"]], AUTHORITY[\"EPSG\",\"4149\"]], PROJECTION[\"Hotine_Oblique_Mercator_Azimuth_Center\"], PARAMETER[\"latitude_of_center\",46.9524055555556], PARAMETER[\"longitude_of_center\",7.43958333333333], PARAMETER[\"azimuth\",90], PARAMETER[\"rectified_grid_angle\",90], PARAMETER[\"scale_factor\",1], PARAMETER[\"false_easting\",600000], PARAMETER[\"false_northing\",200000], UNIT[\"metre\",1, AUTHORITY[\"EPSG\",\"9001\"]], AXIS[\"Easting\", EAST], AXIS[\"Northing\", NORTH], AUTHORITY[\"EPSG\",\"21781\"]]")]
        CH1903_LV03,

        [Description("PROJCS[\"CH1903 / LV03\", GEOGCS[\"CH1903\", DATUM[\"CH1903\",SPHEROID[\"Bessel 1841\",6377397.155,299.1528128, AUTHORITY[\"EPSG\",\"7004\"]], AUTHORITY[\"EPSG\",\"6149\"]], PRIMEM[\"Greenwich\",0, AUTHORITY[\"EPSG\",\"8901\"]], UNIT[\"degree\",0.0174532925199433, AUTHORITY[\"EPSG\",\"9122\"]], AUTHORITY[\"EPSG\",\"4149\"]], PROJECTION[\"Hotine_Oblique_Mercator_Azimuth_Center\"], PARAMETER[\"latitude_of_center\",46.9524055555556], PARAMETER[\"longitude_of_center\",7.43958333333333], PARAMETER[\"azimuth\",90], PARAMETER[\"rectified_grid_angle\",90], PARAMETER[\"scale_factor\",1], PARAMETER[\"false_easting\",600000], PARAMETER[\"false_northing\",200000], UNIT[\"metre\",1, AUTHORITY[\"EPSG\",\"9001\"]], AXIS[\"Easting\", EAST], AXIS[\"Northing\", NORTH], AUTHORITY[\"EPSG\",\"21781\"]]")]
        CH1903,

        [Description("GEOGCS[\"WGS 84\",DATUM[\"WGS_1984\",SPHEROID[\"WGS 84\",6378137,298.257223563,AUTHORITY[\"EPSG\", \"7030\"]], AUTHORITY[\"EPSG\",\"6326\"]], PRIMEM[\"Greenwich\",0, AUTHORITY[\"EPSG\",\"8901\"]], UNIT[\"degree\",0.0174532925199433, AUTHORITY[\"EPSG\",\"9122\"]], AUTHORITY[\"EPSG\",\"4326\"]]")]
        WGS84,
        
        [Description("PROJCS[\"Bessel_1841_Hotine_Oblique_Mercator_Azimuth_Natural_Origin\", GEOGCS[\"Unknown datum based upon the Bessel 1841 ellipsoid\", DATUM[\"Not_specified_based_on_Bessel_1841_ellipsoid\", SPHEROID[\"Bessel 1841\",6377397.155,299.1528128, AUTHORITY[\"EPSG\",\"7004\"]], AUTHORITY[\"EPSG\",\"6004\"]], PRIMEM[\"Greenwich\",0], UNIT[\"Degree\",0.0174532925199433]], PROJECTION[\"Hotine_Oblique_Mercator\"], PARAMETER[\"latitude_of_center\",46.9524055555556], PARAMETER[\"longitude_of_center\",7.43958333333333], PARAMETER[\"azimuth\",90], PARAMETER[\"rectified_grid_angle\",90], PARAMETER[\"scale_factor\",1], PARAMETER[\"false_easting\",-9419820.5907], PARAMETER[\"false_northing\",200000], UNIT[\"metre\",1, AUTHORITY[\"EPSG\",\"9001\"]], AXIS[\"Easting\", EAST], AXIS[\"Northing\", NORTH]]")]
        Bessel_1841_Hotine_Oblique_Mercator_Azimuth_Natural_Origin,

        [Description("PROJCS[\"ETRS89 - extended / LAEA Europe\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Lambert_Azimuthal_Equal_Area\"],PARAMETER[\"latitude_of_center\",52],PARAMETER[\"longitude_of_center\",10],PARAMETER[\"false_easting\",4321000],PARAMETER[\"false_northing\",3210000],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Northing\",NORTH],AXIS[\"Easting\",EAST],AUTHORITY[\"EPSG\",\"3035\"]]")]
        ETRS89_extended_LAEAEurope,

        [Description("GEOGCS[\"Undefined geographic SRS\",DATUM[\"unknown\",SPHEROID[\"unknown\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AXIS[\"Latitude\",NORTH],AXIS[\"Longitude\",EAST]]")]
        UndefinedgeographicSRS,  //  SRID 0

        [Description("Undefined cartesian SRS")]
        UndefinedProjectedSRS,  //  SRID -1

        [Description("None")]
        None

    }
}