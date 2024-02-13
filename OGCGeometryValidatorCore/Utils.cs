using System;
using System.Diagnostics;

namespace OGCGeometryValidatorCore;

internal static class Utils
{
    /// <summary>
    /// formats the elapsed seconds of the stopwatch
    /// into a nicely formatted string with hour, minutes and seconds
    /// </summary>
    /// <param name="stopWatch"></param>
    /// <returns></returns>
    internal static string ToNicelyTimeFormatString(Stopwatch stopWatch)
    {
            long seconds = stopWatch.ElapsedMilliseconds / 1000;
            TimeSpan time = TimeSpan.FromSeconds(seconds);
            //here backslash is just a character to get : in output
            return time.ToString(@"hh\:mm\:ss\:fff");
        }
}