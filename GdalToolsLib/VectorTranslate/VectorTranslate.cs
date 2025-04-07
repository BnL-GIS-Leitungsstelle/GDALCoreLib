using MaxRev.Gdal.Core;
using OSGeo.GDAL;

namespace GdalToolsLib.VectorTranslate
{
    public static class VectorTranslate
    {
        static VectorTranslate()
        {
            GdalBase.ConfigureAll();
            Gdal.UseExceptions();
        }

        /// <summary>
        /// This method should mimic the behaviour of ogr2ogr/VectorTranslate. 
        /// Therefore, a description of all the available options can be found under <see href="https://gdal.org/en/stable/programs/ogr2ogr.html"/>
        /// To make it a bit easier to use, common options have been modeled out in the <see cref="VectorTranslateOptions"/> object
        /// 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="options"></param>
        public static void Run(string source, string destination, VectorTranslateOptions? options)
        {
            using var ds = Gdal.OpenEx(source, (uint)GdalConst.OF_VECTOR, null, null, null);

            using var opts = new GDALVectorTranslateOptions(options?.ToStringArray() ?? []);
            Gdal.wrapper_GDALVectorTranslateDestName(destination, ds, opts, null, null).Dispose();
        }
    }
}
