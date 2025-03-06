//using System;
//using System.Collections.Generic;
//using System.Linq;
//using BnL.CopyDissolverFGDB.Parameters;

//namespace BnL.CopyDissolverFGDB;

//public class WorkList
//{
//    public List<WorkLayer> WorkLayers { get; }

//    public WorkList()
//    {
//        WorkLayers = new List<WorkLayer>();
//    }

//    public void AddLayer(WorkLayer worklayer)
//    {
//        WorkLayers.Add(worklayer);
//    }

//    public List<WorkLayer> QueryWorkLayerFilterForProtectedArea(int year, string theme)
//    {
//        var result = WorkLayers.Where(x => x.LayerContentInfo.Year == year &&
//                                                                  x.LayerContentInfo.Category.Equals(theme, StringComparison.InvariantCultureIgnoreCase)).ToList();
//        return result;
//    }

//    public List<WorkLayer> QueryWorkLayerToBuffer(string legalState, string theme)
//    {
//        var result = WorkLayers.Where(w => w.LayerContentInfo.LegalState.Contains(legalState, StringComparison.CurrentCultureIgnoreCase) &&
//                                           w.LayerContentInfo.Category.Contains(theme, StringComparison.CurrentCultureIgnoreCase)).ToList();
//        return result;
//    }

//    [Obsolete]
//    public List<WorkLayer> QueryWorkLayerToUnion(int year, string legalState, string themeA, string themeB)
//    {
//        var result = WorkLayers.Where(x =>
//                                            x.WorkState == EWorkState.IsDissolved &&
//                                            x.LayerContentInfo.Year == year &&
//                                            x.LayerContentInfo.LegalState.Contains(legalState, StringComparison.CurrentCultureIgnoreCase) &&
//                                            x.LayerContentInfo.SubCategory.StartsWith("Anhang") == false &&
//                                            (x.LayerContentInfo.Category.Contains(themeA, StringComparison.CurrentCultureIgnoreCase) ||
//                                            x.LayerContentInfo.Category.Contains(themeB, StringComparison.CurrentCultureIgnoreCase))).ToList();

//        return result;
//    }

//    public List<WorkLayer> QueryWorkLayerToUnion(List<LayerParameter> layers)
//    {
//        List<WorkLayer> result = new();

//        //foreach (var lp in layers)
//        //{
//        //    result.AddRange(WorkLayers.Where(x =>
//        //        x.WorkState == EWorkState.IsDissolved &&
//        //        x.LayerContentInfo.Year == Convert.ToInt32(lp.Year) &&
//        //        x.LayerContentInfo.LegalState.Contains(lp.LegalState, StringComparison.CurrentCultureIgnoreCase) &&
//        //        x.LayerContentInfo.SubCategory.StartsWith("Anhang") == false &&
//        //        x.LayerContentInfo.Category.Contains(lp.Theme, StringComparison.CurrentCultureIgnoreCase)).ToList());
//        //}

//        return result;
//    }


//    public List<WorkLayer> QueryWorkLayerToSelectFinalLayers(List<string> nameEndings)
//    {
//        var result = new List<WorkLayer>();

//        foreach (var nameEnding in nameEndings)
//        {
//            result.AddRange(WorkLayers.Where(x =>
//            x.OriginalLayerName.EndsWith(nameEnding, StringComparison.InvariantCultureIgnoreCase)).ToList());
//        }

//        return result;
//    }

//    public List<WorkLayer> QueryWorkLayerToDissolve()
//    {
//        var result = WorkLayers.Where(x =>
//            x.WorkState == EWorkState.ValidDissolveFields).ToList();
//        return result;
//    }

//    public void Clear()
//    {
//        WorkLayers.Clear();
//    }
//}
