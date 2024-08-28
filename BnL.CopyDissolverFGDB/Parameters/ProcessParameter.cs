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
    public List<FilterParameter> Filters { get; set; }
    public List<BufferParameter> Buffers { get; set; }
    public List<UnionParameter> Unions { get; set; }
    public List<UnionGroup> UnionGroups { get; set; }

    public ProcessParameter()
    {
        Filters = new List<FilterParameter>();
        Buffers = new List<BufferParameter>();
        Unions = new List<UnionParameter>();
        UnionGroups = new List<UnionGroup>();
    }


    /// <summary>
    ///  read filter conditions from file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadFilterParameter(string path)
    {
        var currPath = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
        if (!File.Exists(path)) throw new Exception("Filter file not found.");

        var filters = new List<FilterParameter>();

        using (var fileStream = File.OpenRead(path))
        {
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!line.StartsWith("//"))
                    {
                        string[] attributes = line.Split(';');
                        filters.Add(new FilterParameter(attributes[0], attributes[1], "N.N.", attributes[2]));
                    }
                }
            }
        }

        Filters = filters;
    }

    /// <summary>
    ///  read buffer conditions from file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadBufferParameter(string path)
    {
        if (!File.Exists(path)) throw new Exception("Buffer file not found.");

        var result = new List<BufferParameter>();

        using (var fileStream = File.OpenRead(path))
        {
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!line.StartsWith("//"))
                    {
                        string[] attributes = line.Split(';');
                        var legalState = attributes[0];
                        var layerName = attributes[1];
                        var distance = attributes[2];
                        result.Add(new BufferParameter(legalState, layerName, "N.N.", distance));
                    }
                }
            }
        }

        Buffers = result;
    }


    /// <summary>
    ///  read buffer conditions from file
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public void LoadUnionParameter(string path)
    {
        if (!File.Exists(path)) throw new Exception("Union file not found.");

        var parameterLayers = new List<UnionParameterLayer>();

        using (var fileStream = File.OpenRead(path))
        {
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!line.StartsWith("//"))
                    {
                        // N2001_Serie1_amphibLaichgebietUndWanderobjekte;2001;Serie1;amphibWanderobjekt
                        string[] attributes = line.Split(';');
                        var resultLayerName = attributes[0];

                        var year = attributes[1];
                        var legalState = attributes[2];
                        var layerName = attributes[3];
                        parameterLayers.Add(new UnionParameterLayer(resultLayerName, new LayerParameter(layerName, year, legalState)));
                    }
                }
            }
        }

        foreach (var group in parameterLayers.GroupBy(u => u.ResultLayerName))
        {
            Unions.Add(new UnionParameter(group.Key, group.Select(p => p.LayerParameter).ToList()));
        }
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


