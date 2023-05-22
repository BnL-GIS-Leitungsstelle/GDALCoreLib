using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

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
