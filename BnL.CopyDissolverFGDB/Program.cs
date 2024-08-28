using System;
using System.Collections.Generic;

namespace BnL.CopyDissolverFGDB;

public class Program
{
    static void Main(string[] args)
    {
        const string FolderTimestampFormat = "yyyyMMdd_HHmmss";

        var _inputfolders = new List<string>
        {
            @"G:\BnL\Daten\Ablage\DNL\Bundesinventare",
            @"G:\BnL\Daten\Ablage\DNL\Schutzgebiete",
        };


        string _outputFolder = @$"D:\Analyse\Flaechenstatistik_Generiert_FGDB{DateTime.Now.ToString(FolderTimestampFormat, System.Globalization.CultureInfo.InvariantCulture)}";

        var copyDissolver = new PrepareOptimizedGeodataOfProtectedAreasForFurtherAnalysisUseCase(_inputfolders, _outputFolder, new List<string>() { "ObjNummer", "Name" });

        copyDissolver.ShowAbout();

        copyDissolver.Run();


        Console.WriteLine();
        Console.WriteLine("Press ENTER to end..");
        Console.ReadLine();
    }
}