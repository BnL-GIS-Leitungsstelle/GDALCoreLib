using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OGCToolsNetCoreLib.Geometry
{
    public record SelfOverlapErrorResult(long FidA, long FidB, double Area);

    public class SelfOverlapErrorResultEqualityComparer : IEqualityComparer<SelfOverlapErrorResult>{
        public bool Equals(SelfOverlapErrorResult x, SelfOverlapErrorResult y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return (x.FidA == y.FidA && x.FidB == y.FidB) || (x.FidB == y.FidA && x.FidA == y.FidB);
        }

        public int GetHashCode(SelfOverlapErrorResult obj)
        {
            var sortedFIDs = new List<long>() { obj.FidA, obj.FidB };
            sortedFIDs.Sort();
            return HashCode.Combine(sortedFIDs[0], sortedFIDs[1]);
        }
    }
}
