using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using OSGeo.OGR;

namespace GdalToolsLib.Workflows;

internal sealed class SchemaExports(string directory) : IDisposable
{
    private sealed record Entry(string Path, string Temporary, string Backup);
    private readonly List<Entry> _entries = [];
    private readonly Stack<Entry> _published = new();

    public void Stage(string role, OSGeo.OGR.Layer layer)
    {
        using var schema = layer.GetLayerDefn();
        var fields = Enumerable.Range(0, schema.GetFieldCount()).Select(index =>
        {
            using var field = schema.GetFieldDefn(index);
            return new { Name = field.GetName(), Type = field.GetFieldType().ToString(), Width = field.GetWidth() };
        }).ToArray();
        var json = JsonSerializer.Serialize(new { Layer = layer.GetName(), ObjectId = layer.GetFIDColumn(), Geometry = layer.GetGeometryColumn(), Fields = fields },
            new JsonSerializerOptions { WriteIndented = true });
        var path = Path.Combine(directory, role + ".json");
        var temp = path + "." + Guid.NewGuid().ToString("N");
        var entry = new Entry(path, temp + ".tmp", temp + ".bak");
        _entries.Add(entry);
        File.WriteAllText(entry.Temporary, json);
    }

    public void Publish()
    {
        foreach (var entry in _entries)
        {
            if (File.Exists(entry.Path)) File.Replace(entry.Temporary, entry.Path, entry.Backup);
            else File.Move(entry.Temporary, entry.Path);
            _published.Push(entry);
        }
    }

    public void Restore()
    {
        while (_published.TryPop(out var entry))
        {
            if (File.Exists(entry.Backup)) File.Move(entry.Backup, entry.Path, true);
            else File.Delete(entry.Path);
        }
    }

    public void Dispose()
    {
        foreach (var entry in _entries) File.Delete(entry.Temporary);
        foreach (var entry in _published) File.Delete(entry.Backup);
    }
}
