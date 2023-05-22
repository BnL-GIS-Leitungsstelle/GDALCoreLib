using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OGCToolsNetCoreLib.Layer;
using OSGeo.OGR;

namespace CreateOptimizedGeopackage
{
    internal class GeopackageDefinition
    {
        public string Path { get; set; }
        public string Name { get; set; }

        public List<LayerDetails> LayersDetailsList { get; set; }

        public GeopackageDefinition()
        {
            LayersDetailsList = new List<LayerDetails>();
        }

        /// <summary>
        /// adds a new layer details item to the list and returns its reference
        /// </summary>
        /// <returns></returns>
        public LayerDetails InsertLayerDetails(string layerName, string projection, string geometryType)
        {
            var geomType = (wkbGeometryType)Enum.Parse(typeof(wkbGeometryType), geometryType);

            var layerDetails = new LayerDetails(layerName, projection, geomType);

            LayersDetailsList.Add(layerDetails);
            return layerDetails;
        }

        /// <summary>
        ///  read filter conditions from file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static GeopackageDefinition ReadFromDefinitionFile(string path)
        {
            if (!File.Exists(path)) throw new Exception("Geopackage definition file not found.");

            var gpkgDef = new GeopackageDefinition();


            using (var fileStream = File.OpenRead(path))
            {
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, 128))
                {
                    String line;
                    LayerDetails currentLayer = null;

                    while ((line = streamReader.ReadLine()) != null)
                    {
                        if (line.StartsWith("//") || String.IsNullOrEmpty(line)) continue;

                        string[] attributes = line.Split(';');

                        switch (attributes[0])
                        {
                            case "#gpkg_Name":
                                gpkgDef.Name = attributes[1];
                                break;
                            case "#layer":  // 1. name 2. ESpatialRefWKT 3. wkbGeometryType
                                currentLayer = gpkgDef.InsertLayerDetails(attributes[1], attributes[2], attributes[3]);

                                break;
                            case "#field":
                                currentLayer.Schema.InsertField(attributes[1], attributes[2], attributes[3],
                                    attributes[4], attributes[5]);
                                break;
                            case "#layer_End":
                                break;


                            default:
                                throw new NotImplementedException("line interpretation not implemented");
                        }

                        // filters.Add(new FilterParameter(attributes[0], attributes[1], attributes[2]));

                    }
                }
            }
            return gpkgDef;
        }

        public void ShowDefinition()
        {
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine(this);

            foreach (var layer in LayersDetailsList)
            {
                Console.WriteLine();
                Console.WriteLine("  " + layer);

                foreach (var field in layer.Schema.FieldList)
                {
                    Console.WriteLine("    " + field);
                }
            }
        }

        public override string ToString()
        {
            return $"GPKG-Definition for {Name} with {LayersDetailsList.Count} Layers";
        }
    }


}
