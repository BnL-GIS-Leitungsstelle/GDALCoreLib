using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace GdalToolsLib.VectorTranslate
{
    public class VectorTranslate
    {
        /// <summary>
        /// This method should mimic the behaviour of ogr2ogr/VectorTranslate. 
        /// Therefore a description of all the available options can be found under <see href="https://gdal.org/en/stable/programs/ogr2ogr.html"/>
        /// To make it a bit easier to use, common options have been modeled out in the <see cref="VectorTranslateOptions"/> object
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="options"></param>
        public static void Run(string source, string destination, VectorTranslateOptions options)
        {
            GdalBase.ConfigureAll();
            Gdal.UseExceptions();
            // TODO: Figure out the driver based on the extension (like this only FGDBs work)
            using var ds = Gdal.OpenEx(source, (uint)GdalConst.OF_VECTOR, ["OpenFileGDB"], [], []);

            using var opts = new GDALVectorTranslateOptions(options.ToStringArray());

            using var _ = Gdal.wrapper_GDALVectorTranslateDestName(destination, ds, opts, null, null);
        }
    }
}
