using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using GdalToolsLib.Feature;
using GdalToolsLib.Layer;
using OSGeo.OGR;
using SpreadCheetah;
using SpreadCheetah.Worksheets;

namespace ReadLayerDataIntoExcel;

public static class ExcelWriter
{
    public static async Task WriteToFileAsync(ExcelContent content)
    {
        // SpreadCheetah can write to any writeable stream.
        // To write to a file, start by creating a file stream.
        System.Console.WriteLine($"Write Excel-file : {content.Filename}");

        using (var stream = File.Create(content.Filename))
        {
            using (var spreadsheet = await Spreadsheet.CreateNewAsync(stream))
            {
                // A spreadsheet must contain at least one worksheet.
                await spreadsheet.StartWorksheetAsync("layer_content");

                // Rows are inserted from top to bottom.
                await spreadsheet.AddRowAsync(GetFieldNamesRow(content.FieldList));

                // Cells are inserted row by row.

                foreach (var dataRow in GetDataRows(content.FeatureRows))
                {
                    await spreadsheet.AddRowAsync(dataRow);
                }

                // add more worksheets
                foreach (var fieldInfo in content.FieldList.Where(x => x.Type == FieldType.OFTString || x.Type == FieldType.OFTInteger))
                {
                    int maxLength = 30; // definition of Excel

                    if (fieldInfo.Name.Length > maxLength)
                        fieldInfo.Name = fieldInfo.Name.Substring(0, maxLength); 

                    await spreadsheet.StartWorksheetAsync($"{fieldInfo.Name}");
                    // Cells are inserted row by row.
                    await spreadsheet.AddRowAsync(new List<Cell> { new("Wert"), new($"Anzahl Nennungen") });

                    var columnPosition = content.FieldList.Select(x => x.Name).ToList().IndexOf(fieldInfo.Name);

                    var columnValues = new List<string>();

                    foreach (var row in content.FeatureRows)
                    {
                        if (row.Items[columnPosition] is not null)
                        {
                            columnValues.Add(row.Items[columnPosition].ToString());
                        }
                        else
                        {
                            columnValues.Add(String.Empty);
                        }
                    }

                    var columnValuesDistinct = columnValues.Distinct().Order().ToList();

                    var columnValueAndNumbers = new Dictionary<string, int>();

                    foreach (string value in columnValuesDistinct)
                    {
                        int number = columnValues.Count(x => x == value);
                        columnValueAndNumbers.Add(value, number);
                    }
                    
                    foreach (var dataRow in columnValueAndNumbers)
                    {
                        await spreadsheet.AddRowAsync(new List<Cell> { new($"{dataRow.Key}"), new($"{dataRow.Value}") });
                    }
                }


                // Remember to call Finish before disposing.
                // This is important to properly finalize the XLSX file.
                await spreadsheet.FinishAsync();
            }
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