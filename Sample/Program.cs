using Shap.ShapDoc.Exporter;
using Shap.ShapDoc.Parser;

namespace Shap.ShapDoc.Sample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string inputPath = args[0], outputPath = args[1];
            string input = File.ReadAllText(inputPath);
            ShapDocDeserializer deserializer = new();
            ShapDoc doc = deserializer.Deserialize(input);
            ShapDocToHtml htmlConverter = new();
            string output = htmlConverter.Convert(doc);
            File.WriteAllText(outputPath, output);
            Console.WriteLine("All done ^-^\nPress any key...");
            Console.ReadKey();
        }
    }
}
