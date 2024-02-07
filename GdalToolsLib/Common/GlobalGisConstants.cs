using System.Collections.Generic;

namespace GdalToolsLib.Common;

public static class GlobalGisConstants
{
    // all extensions concerning shp-files
    /// <summary>
    ///     SHP: Die Hauptdatei, in der die Feature-Geometrie gespeichert wird; erforderlich.
    /// https://desktop.arcgis.com/de/arcmap/10.3/manage-data/shapefiles/shapefile-file-extensions.htm
    /// 
    ///SHX: Die Indexdatei, in der der Index der Feature-Geometrie gespeichert wird; erforderlich.
    /// DBF: Die dBASE-Tabelle, in der die Attributinformationen von Features gespeichert werden; erforderlich.
    /// Zwischen der Geometrie und den Attributen besteht eine Eins-zu-Eins-Beziehung, die auf einer Datensatznummer basiert. Attributdatensätze in der dBASE-Datei müssen die gleiche Reihenfolge wie Datensätze in der Hauptdatei aufweisen.
    /// SBN und SBX: Die Dateien, in denen der räumliche Index der Features gespeichert wird.
    /// FBN und FBX: Die Dateien, in denen der räumliche Index von Features für schreibgeschützte Shapefiles gespeichert wird.
    /// AIN und AIH: Die Dateien, in denen der Attributindex der aktiven Felder in einer Tabelle oder einer Attributtabelle eines Themas gespeichert wird.
    /// ATX: Für jeden Shapefile- oder dBASE-Attributindex in ArcCatalog wird eine ATX-Datei erstellt. ArcView GIS 3.x-Attributindizes für Shapefiles und dBASE-Dateien werden von ArcGIS nicht verwendet. Für Shapefiles und dBASE-Dateien wurde ein neues Modell zur Indizierung von Attributen entwickelt.
    /// IXS: Geokodierungsindex für Shapefiles mit Lese-/Schreibzugriff.
    /// MXS: Geokodierungsindex für Shapefiles mit Lese-/Schreibzugriff (ODB-Format).
    /// PRJ: Die Datei, in der die Koordinatensysteminformationen gespeichert werden; verwendet von ArcGIS.
    /// XML: Metadaten für ArcGIS. Dient dem Speichern von Informationen über das Shapefile.
    /// CPG: Eine optionale Datei, die zum Angeben der Codeseite mit dem zu verwendenden Zeichensatz genutzt werden kann.
    /// </summary>
    public static List<string> WellKnownShapeExtensions = new()
    {
        ".shp", ".shx", ".dbf", ".prj", ".sbn", ".sbx", ".fbn",".fbx", ".ain", ".aih",
        ".atx", ".ixs", ".mxs", ".xml", ".shp.xml",".cpg"
    };

}