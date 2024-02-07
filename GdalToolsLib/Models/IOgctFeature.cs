using System;
using System.Collections.Generic;
using GdalToolsLib.Feature;
using GdalToolsLib.Geometry;
using GdalToolsLib.Layer;
using OSGeo.OGR;

namespace GdalToolsLib.Models;

public interface IOgctFeature : IDisposable
{
    long FID { get; }
    string ObjNumber { get; }
    string ObjName { get; }
    IOgctGeometry OpenGeometry();

    wkbGeometryType GetGeomType();

    IOgctFeature CloneAndOpen();
    GeometryValidationResult ValidateGeometry();

    bool ValidateSchemaConstraints();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="feature"></param>
    /// <param name="fieldDef"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    dynamic ReadValue(FieldDefnInfo fieldDef);

    FeatureRow ReadRow(List<FieldDefnInfo> fieldList);

    /// <summary>
    /// write value to field
    /// </summary>
    /// <param name="feature"></param>
    /// <param name="layer"></param>
    /// <param name="field"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    FeatureFieldWriteResult WriteValue(FieldDefnInfo field, dynamic value);

    FeatureFieldWriteResult SetValue(FieldDefnInfo field, dynamic value);

    string GetFieldAsString(string fieldName);
    void SetGeometry(IOgctGeometry other);
}