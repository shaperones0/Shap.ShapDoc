using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Shap.ShapDoc.CodeConverter
{
    public class CodeConverter
    {
        public enum Tags
        {
            Space,
            Keyword1,
            Keyword2,
            Operator,
            Number,
            Variable,
            String,
            Function,
            Class,
            Comment
        }

        static readonly Dictionary<string, Dictionary<string, Tags>> langToColorToToken = new()
        {
            ["Python"] = new()
            {
                ["3b3b3b"] = Tags.Space,
                ["af00db"] = Tags.Keyword1,
                ["0000ff"] = Tags.Keyword2,
                ["000000"] = Tags.Operator,
                ["098658"] = Tags.Number,
                ["001080"] = Tags.Variable,
                ["a31515"] = Tags.String,
                ["795d26"] = Tags.Function,
                ["267f99"] = Tags.Class,
                ["008000"] = Tags.Comment
            },
            ["C#"] = new()
            {
                ["3b3b3b"] = Tags.Space,
                ["af00db"] = Tags.Keyword1,
                ["0000ff"] = Tags.Keyword2,
                ["000000"] = Tags.Operator,
                ["098658"] = Tags.Number,
                ["001080"] = Tags.Variable,
                ["a31515"] = Tags.String,
                ["795d26"] = Tags.Function,
                ["267f99"] = Tags.Class,
                ["008000"] = Tags.Comment
            },
        };

        private readonly FormattedTextDeserializer htmlDeserialzer = new();

        public string Convert(IEnumerable<char> input, Dictionary<string, Tags> table)
        {
            IEnumerator<char> inputEnumerator = input.GetEnumerator();
            do
            {
                inputEnumerator.MoveNext();
            } while (inputEnumerator.Current != '<');

            XmlDocument doc = htmlDeserialzer.Deserialize(input);
            List<string> output = ["<pcode>"];

            XmlNodeList divs = doc.ChildNodes[0]!.ChildNodes[1]!.ChildNodes[1]!.ChildNodes[1]!.ChildNodes;
            foreach (XmlNode div in divs)
            {
                if (div.Name != "div") throw new ArgumentException("Input doesn't contain valid formatted text", nameof(input));
                XmlNodeList spans = div.ChildNodes;
                foreach (XmlNode spanOrBr in spans)
                {
                    switch (spanOrBr.Name)
                    {
                        case "span":
                            XmlAttribute styleAttr = spanOrBr.Attributes![0];
                            if (styleAttr.Name != "style") throw new ArgumentException("Input doesn't contain valid formatted text", nameof(input));
                            string colorStr = styleAttr.Value.Substring(8, 6);

                            if (table.TryGetValue(colorStr, out Tags tags))
                            {
                                output.Add($"<pcn type=\"{Enum.GetName(tags)}\">{spanOrBr.InnerText}</pcn>");
                            }
                            else throw new KeyNotFoundException("Unable to understand given color");
                            break;
                        case "br":
                            output.Add("<pcbr/>");
                            break;
                        default:
                            throw new ArgumentException("Input doesn't contain valid formatted text", nameof(input));
                    }
                    
                }
                output.Add("<pcbr/>");
            }

            output.Add("</pcode>");
            return string.Join("", output);
        }

        public string Convert(IEnumerable<char> input, string langName)
        {
            return Convert(input, langToColorToToken[langName]);
        }
    }
}
