using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OGCToolsNetCoreLib.Layer
{
    public class LayerNameKnownContent
    {
        public ELegalState LegalState { get; set; }

        /// <summary>
        /// the sub category, like Anhang
        /// </summary>
        public ESubCategory SubCategory { get; set; }

        /// <summary>
        /// the main category
        /// </summary>
        public ECategory Category { get; set; }
    }
}
