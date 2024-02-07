using System;
using System.Collections.Generic;

namespace OGCToolsNetCoreLib.Layer
{
    public class LayerNameBafuContent
    {
        public int Year { get; set; }

        public string LegalState { get; set; }

        /// <summary>
        /// like "Anhang"
        /// </summary>
        public string SubCategory { get; set; }

        /// <summary>
        /// the essential part of the layer name
        /// </summary>
        public string Category { get; set; }

        public ECategory ECategory { get; set; }

        public DateOnly ValidFromDate { get; set; }
        public DateOnly UpdateDate { get; set; }

        public string FormatErrorInfo { get; set; }

        //public string Dates { get; set; }

        /// <summary>
        /// Geodate of protected site are stored in an layername format in BAFU, 
        /// that can be used to extract info on protection status from the given layername.
        /// If the layer follows a standard scheme of, like:
        /// Nyyyy_legalState_Theme_appendix(if available)_date(s)
        /// N2017_Revision_amphibWanderobjekt_Anhang3_20171101,
        /// Needed information can be retrieved.
        /// If the scheme isn't found, the properties stay empty
        /// </summary>
        /// <param name="layerName"></param>
        public LayerNameBafuContent(string layerName)
        {
            SubCategory = String.Empty;
            ECategory = ECategory.Unknown;
            ParseLayerName(layerName);
        }

        private void ParseLayerName(string layerName)
        {
            var partsOfName = layerName.Split('_');

            if (HasValidFormat(partsOfName) == false)
            {
                return;
            }

            Year = Convert.ToInt32(partsOfName[0].Substring(1, 4));

            LegalState = partsOfName[1];

            Category = partsOfName[2];

            ECategory = MapFriendlyNameToEnum(Category);



            if (partsOfName.Length == 4)
            {
                if (TryGetDateOnly(partsOfName[3], out var dateResult))
                {
                    ValidFromDate = dateResult;
                }
                else
                {
                    SubCategory = partsOfName[3];
                }
            }

            // can be 'N2014_Revision_jagdbann_20140101_20220503'
            // or 'N2014_Revision_jagdbann_Anhang_20140101'
            if (partsOfName.Length == 5)
            {
                if (TryGetDateOnly(partsOfName[3], out var dateResultPart3))
                {
                    ValidFromDate = dateResultPart3;

                    if (TryGetDateOnly(partsOfName[4], out var dateResultPart4))
                    {
                        UpdateDate = dateResultPart4;
                    }
                    else
                    {

                    }
                }
                else
                {
                    SubCategory = partsOfName[3];
                    if (TryGetDateOnly(partsOfName[4], out var dateResultPart4))
                    {
                        ValidFromDate = dateResultPart4;
                    }
                    else
                    {

                    }

                }


            }
        }

        public static ECategory MapFriendlyNameToEnum(string category)
        {
            var dict = new Dictionary<string, ECategory>
                {
                { "nationalpark", ECategory.Nationalpark },
                { "parkkernzone", ECategory.Parkkernzone },
                { "parkperimeter", ECategory.Parkperimeter },
                { "hochmoor", ECategory.Hochmoor },
                { "flachmoor", ECategory.Flachmoor },
                { "auengebiete", ECategory.Auengebiete },
                { "amphiblaichgebietundwanderobjekteunion", ECategory.AmphiblaichgebietUndWanderobjekteUnion },
                { "trockenwiesenweiden", ECategory.Trockenwiesenweiden },
                { "wasserzugvogel", ECategory.Wasserzugvogel },
                { "jagdbann", ECategory.Jagdbann },
                { "landschaftnaturdenkmal", ECategory.LandschaftNaturdenkmal },
                { "moorlandschaft", ECategory.Moorlandschaft },
                { "biosphaerenreservat", ECategory.Biosphaerenreservat },
                { "ramsar", ECategory.Ramsar },
                { "smaragd", ECategory.Smaragd },
                { "unescoworldnaturalheritage", ECategory.UnescoWorldNaturalHeritage },
                { "vaew", ECategory.VAEW }
            };

            if (dict.ContainsKey(category.ToLowerInvariant()) == false)
            {
                return ECategory.Unknown;
            }

            return dict[category.ToLowerInvariant()];

        }

        /// <summary>
        /// gets the date from a string like '20011224lalala'
        /// </summary>
        /// <param name="value"></param>
        /// <param name="dateResult"></param>
        /// <returns></returns>
        private bool TryGetDateOnly(string value, out DateOnly dateResult)
        {
            if (value.Length < 8)
            {
                dateResult = default;
                return false;
            }

            var strtempDate = value.Substring(0, 8);
            var tmpYear = strtempDate.Substring(0, 4);
            var tmpMonth = strtempDate.Substring(4, 2);
            var tmpDay = strtempDate.Substring(6, 2);

            if (DateOnly.TryParse($"{tmpDay}.{tmpMonth}.{tmpYear}", out dateResult) == false)
            {
                FormatErrorInfo = "Could not parse 'Date'";
                return false;
            }

            return true;
        }

        public bool HasValidFormat(string[] partsOfName)
        {
            if (partsOfName.Length < 3)
            {
                FormatErrorInfo = "Missing information";
                return false;
            }

            if (partsOfName[0].StartsWith("N", comparisonType: StringComparison.InvariantCulture) == false
                || partsOfName[0].Length != 5)
            {
                FormatErrorInfo = "Does not start with 'N'";
                return false;
            }

            var result = 0;
            if (Int32.TryParse(partsOfName[0].Substring(1, 4), out result) == false)
            {
                FormatErrorInfo = "Could not parse 'Year'";
                return false;
            }


            return true;
        }

        public override string ToString()
        {
            return $"{Year} {LegalState} {Category} {SubCategory} {ValidFromDate}";
        }
    }
}
