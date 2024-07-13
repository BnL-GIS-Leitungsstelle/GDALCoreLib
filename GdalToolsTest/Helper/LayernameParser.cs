using System;
using GdalToolsLib.Geometry;

namespace GdalToolsTest.Helper;

/// <summary>
/// the testdata for this test-project are mostly located in geodata-files or geodatabases.
/// the name of the layer contains informations about some properties of the data that can be used as
/// a reference of the desired state.
/// 
/// </summary>
public class LayernameParser
{
    public bool HasErrorsOfType { get; private set; }

    public int ErrorCount { get; private set; }


    public LayernameParser(string layerName, EGeometryValidationType geometryValidationType)
    {
        ParseErrorType(layerName, geometryValidationType.ToString());
    }


    private void ParseErrorType(string layerName, string validationType)
    {
        if (layerName.Contains(validationType))
        {
            HasErrorsOfType = true;

            ParseErrorCount(layerName, validationType);
        }
    }


    private void ParseErrorCount(string layerName, string validationType)
    {
        var startIndex = layerName.IndexOf(validationType);

        var startindexCounter = startIndex + validationType.Length;

        string b = string.Empty;
        int val = 0;

        for (int i = startindexCounter; i < layerName.Length; i++)
        {
            if (Char.IsDigit(layerName[i]))
            {
                b += layerName[i];
            }
            else
            {
                break;
            }
        }

        if (b.Length > 0)
            val = int.Parse(b);

        ErrorCount = val;
    }

}