using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BnL.CopyDissolverFGDB.Parameters;


/// <summary>
/// handles preparation of filter definitions
/// </summary>
public class ProcessParameter
{
    public List<FilterParameter> Filters = [];
    public List<BufferParameter> Buffers = [];
    public List<UnionParameter> Unions = [];


    private static IEnumerable<string[]> GetLinesWithoutComments(string filePath)
    {
        if (!File.Exists(filePath)) throw new Exception($"File '{filePath}' not found");

        using var fileStream = File.OpenRead(filePath);
        using var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128);
        
        while (!streamReader.EndOfStream)
        {
            var line = streamReader.ReadLine()!;
            if (!line.StartsWith("//"))
            {
                yield return line.Split(';');
            }
        }
    }

    /// <summary>
    ///  read filter conditions from file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadFilterParameter(string path)
    {
        Filters = GetLinesWithoutComments(path)
            .Select(attributes => new FilterParameter(attributes[0], attributes[1], "N.N.", attributes[2]))
            .ToList();
    }

    /// <summary>
    ///  read buffer conditions from file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadBufferParameter(string path)
    {
        Buffers = GetLinesWithoutComments(path)
            .Select(attributes =>
            {
                var legalState = attributes[0];
                var layerName = attributes[1];
                var distance = attributes[2];
                return new BufferParameter(legalState, layerName, "N.N.", distance);
            })
            .ToList();
    }


    /// <summary>
    ///  read buffer conditions from file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadUnionParameter(string path)
    {
        Unions = GetLinesWithoutComments(path)
            .Select(attributes =>
            {
                var resultLayerName = attributes[0];

                var year = attributes[1];
                var legalState = attributes[2];
                var layerName = attributes[3];
                return new UnionParameterLayer(resultLayerName, new LayerParameter(layerName, year, legalState));
            })
            .GroupBy(u => u.ResultLayerName)
            .Select(group => new UnionParameter(group.Key, group.Select(p => p.LayerParameter).ToList()))
            .ToList();

    }


    public void ShowFilters()
    {
        Console.WriteLine("======================= CURRENT FILTERS =======================");
        foreach (FilterParameter filter in Filters) Console.WriteLine($"{filter.Theme} {filter.Year} : {filter.WhereClause}");
        Console.WriteLine("===============================================================\n\n");
    }

    public void ShowBuffers()
    {
        Console.WriteLine("======================= CURRENT BUFFERS =======================");
        foreach (var buffer in Buffers) Console.WriteLine($"{buffer.LegalState} {buffer.Theme}, Radius [m]= {buffer.BufferDistanceMeter:F2}");
        Console.WriteLine("===============================================================\n\n");
    }

    public void ShowUnions()
    {
        Console.WriteLine("======================= CURRENT UNIONS =======================");
        foreach (var union in Unions)
        {
            Console.WriteLine($"unify into {union.ResultLayerName}");
            foreach (var lp in union.LayerParameters)
            {
                Console.WriteLine($"             - layer: {lp.Year}, {lp.LegalState}, {lp.Theme}");
            }
        }
        Console.WriteLine("===============================================================\n\n");
    }
}


