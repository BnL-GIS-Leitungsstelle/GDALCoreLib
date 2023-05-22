using System.Diagnostics;
using System.Reflection;

namespace OGCGeometryValidatorGUI.Models
{
    public class AppInfoModel
    {
        public string? CompanyName { get; set; }

        public string? ProductName { get; set; }

        public string? ProductVersion { get; set; }

        public AppInfoModel()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);

            CompanyName = fvi.CompanyName;
            ProductName = fvi.ProductName;
            ProductVersion = fvi.ProductVersion;
        }


        public override string ToString()
        {
            return $"({CompanyName})   {ProductName}, Version {ProductVersion}";
        }


    }
}
