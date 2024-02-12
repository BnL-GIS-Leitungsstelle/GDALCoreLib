using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GdalToolsLib.Feature;
using GdalToolsLib.Layer;
using OSGeo.OGR;
using SpreadCheetah;

namespace ReadLayerDataIntoExcel;

public static class ExcelWriter
{
    public static async Task WriteToFileAsync(ExcelContent content)
    {
        // SpreadCheetah can write to any writeable stream.
        // To write to a file, start by creating a file stream.
        System.Console.WriteLine($"Write Excel-file : {content.Filename}");

        using (var stream = File.Create(content.Filename))
        using (var spreadsheet = await Spreadsheet.CreateNewAsync(stream))
        {
            // A spreadsheet must contain at least one worksheet.
            await spreadsheet.StartWorksheetAsync("Fields");

            // Rows are inserted from top to bottom.
            await spreadsheet.AddRowAsync(GetFieldNamesRow(content.FieldList));

            // Cells are inserted row by row.

            foreach (var dataRow in GetDataRows(content.FeatureRows))
            {
                await spreadsheet.AddRowAsync(dataRow);
            }

            // Remember to call Finish before disposing.
            // This is important to properly finalize the XLSX file.
            await spreadsheet.FinishAsync();
        }
    }

    private static List<Cell> GetFieldNamesRow(List<FieldDefnInfo> contentFieldList)
    {
        var row = new List<Cell>();

        foreach (var fieldInfo in contentFieldList)
        {
            row.Add(new Cell(fieldInfo.Name));
        }

        return row;
    }


    private static List<List<Cell>> GetDataRows(List<FeatureRow> featureRows)
    {
        List<List<Cell>> rows = new List<List<Cell>>();

        foreach (var featureRow in featureRows)
        {
            rows.Add(GetDataRow(featureRow.Items));
        }
        return rows;
    }


    private static List<Cell> GetDataRow(List<FeatureRowItem> featureRowsItems)
    {
        var row = new List<Cell>();

        foreach (var rowItem in featureRowsItems)
        {
            if (rowItem == null)
            {
                row.Add(new Cell(""));
                continue;
            }

            var type = rowItem.fieldType;

            switch (type)
            {
                case FieldType.OFTInteger:
                    row.Add(new Cell((int)rowItem));
                    break;
                case FieldType.OFTReal:
                    row.Add(new Cell((double)rowItem));
                    break;
                case FieldType.OFTDateTime:
                    row.Add(new Cell(((DateTime)rowItem).ToString("dd.MM.yyyy")));
                    break;

                default:
                    row.Add(new Cell(rowItem.ToString()));
                    break;
            }
        }
        return row;
    }
}