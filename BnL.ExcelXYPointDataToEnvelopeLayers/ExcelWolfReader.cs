using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using BnL.ExcelXYPointDataToEnvelopeLayers.Models;

namespace BnL.ExcelXYPointDataToEnvelopeLayers;

public static class ExcelWolfReader
{
    private static readonly XNamespace SpreadsheetNamespace = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    public static List<WolfModel> Read(string excelPath)
    {
        using var archive = ZipFile.OpenRead(excelPath);
        var sharedStrings = ReadSharedStrings(archive);
        var sheet = LoadFirstSheet(archive);

        var rows = sheet.Root?
            .Element(SpreadsheetNamespace + "sheetData")?
            .Elements(SpreadsheetNamespace + "row")
            .ToList();

        if (rows is null || rows.Count < 2)
        {
            return new List<WolfModel>();
        }

        var headerRow = rows[0];
        var headerByColumn = BuildHeaderMap(headerRow, sharedStrings);
        var result = new List<WolfModel>();

        foreach (var row in rows.Skip(1))
        {
            var cellValues = ExtractRowValues(row, sharedStrings);

            string? GetValue(string headerKey)
            {
                return headerByColumn.TryGetValue(headerKey, out var index) && cellValues.TryGetValue(index, out var value)
                    ? value
                    : null;
            }

            result.Add(new WolfModel
            {
                MonitoringYear = ParseInt(GetValue("monitoringjahr")),
                ObservationDate = ParseDate(GetValue("observationdate")),
                IndividualId = GetValue("individualid"),
                CompartmentMain = GetValue("compartmentmain"),
                Canton = GetValue("canton"),
                X = ParseDouble(GetValue("x")),
                Y = ParseDouble(GetValue("y")),
                WolfAuthorisation = GetValue("wolfauthorisation")
            });
        }

        return result;
    }

    private static Dictionary<int, string?> ExtractRowValues(XElement row, IReadOnlyList<string> sharedStrings)
    {
        var values = new Dictionary<int, string?>();

        foreach (var cell in row.Elements(SpreadsheetNamespace + "c"))
        {
            var columnIndex = GetColumnIndex(cell.Attribute("r")?.Value);
            if (columnIndex < 0)
            {
                continue;
            }

            values[columnIndex] = GetCellValue(cell, sharedStrings);
        }

        return values;
    }

    private static Dictionary<string, int> BuildHeaderMap(XElement headerRow, IReadOnlyList<string> sharedStrings)
    {
        var headerByColumn = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.Elements(SpreadsheetNamespace + "c"))
        {
            var headerText = GetCellValue(cell, sharedStrings);
            var columnIndex = GetColumnIndex(cell.Attribute("r")?.Value);

            if (string.IsNullOrWhiteSpace(headerText) || columnIndex < 0)
            {
                continue;
            }

            headerByColumn[NormalizeHeader(headerText)] = columnIndex;
        }

        return headerByColumn;
    }

    private static string NormalizeHeader(string header) => header.Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
        {
            return new List<string>();
        }

        using var reader = new StreamReader(entry.Open());
        var document = XDocument.Parse(reader.ReadToEnd());

        return document.Root?
            .Elements(SpreadsheetNamespace + "si")
            .Select(si => string.Concat(si.Descendants(SpreadsheetNamespace + "t").Select(t => t.Value)))
            .ToList() ?? new List<string>();
    }

    private static XDocument LoadFirstSheet(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (entry is null)
        {
            throw new FileNotFoundException("Worksheet 'sheet1' not found in workbook.");
        }

        using var reader = new StreamReader(entry.Open());
        return XDocument.Parse(reader.ReadToEnd());
    }

    private static string? GetCellValue(XElement cell, IReadOnlyList<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        var valueElement = cell.Element(SpreadsheetNamespace + "v");

        if (type == "s")
        {
            if (valueElement is null)
            {
                return null;
            }

            if (int.TryParse(valueElement.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sharedIndex) && sharedIndex >= 0 && sharedIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedIndex];
            }

            return null;
        }

        if (type == "inlineStr")
        {
            return cell.Element(SpreadsheetNamespace + "is")?.Value;
        }

        return valueElement?.Value;
    }

    private static int GetColumnIndex(string? cellReference)
    {
        if (string.IsNullOrWhiteSpace(cellReference))
        {
            return -1;
        }

        var index = 0;
        foreach (var character in cellReference)
        {
            if (!char.IsLetter(character))
            {
                break;
            }

            index = (index * 26) + (char.ToUpperInvariant(character) - 'A' + 1);
        }

        return index - 1;
    }

    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static double? ParseDouble(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        return null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var oaDate))
        {
            return DateTime.FromOADate(oaDate);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dateTime))
        {
            return dateTime;
        }

        return null;
    }
}
