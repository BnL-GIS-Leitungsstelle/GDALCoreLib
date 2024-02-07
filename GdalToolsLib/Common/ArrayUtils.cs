using System;
using System.Runtime.InteropServices;

namespace OGCToolsNetCoreLib.Common
{
    internal static class ArrayUtils
    {
        /// <summary>
        /// Get Max Array Size below 1 GB
        /// </summary>
        public static int GetMaxArrayLengthForType(Type type)
        {
            int oneGb = 1073741824;
            var size = Marshal.SizeOf(type);
            return oneGb / size;
        }
    }
}
