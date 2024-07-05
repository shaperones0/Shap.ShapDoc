using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
                ["795e26"] = Tags.Function,
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
                ["795e26"] = Tags.Function,
                ["267f99"] = Tags.Class,
                ["008000"] = Tags.Comment
            },
        };

        private readonly FormattedTextDeserializer htmlDeserialzer = new();

        public string Convert(IEnumerable<char> input, Dictionary<string, Tags> table)
        {
            IEnumerator<char> inputEnumerator = input.GetEnumerator();
            int toSkip = 0;
            do
            {
                inputEnumerator.MoveNext(); toSkip++;
            } while (inputEnumerator.Current != '<');

            List<List<ColoredSpan>> code = new FormattedTextDeserializer().Deserialize(input.Skip(toSkip-1));
            List<string> output = ["<pcode>"];

            foreach (List<ColoredSpan> line in code)
            {
                foreach (ColoredSpan codeNode in line)
                {
                    if (table.TryGetValue(codeNode.colorCode, out Tags tag))
                    {
                        string unescaped = WebUtility.HtmlDecode(codeNode.str);
                        string escaped = WebUtility.HtmlEncode(unescaped);  // TODO oh gwd make it stop
                        output.Add($"<pcn type=\"{Enum.GetName(tag)}\" text=\"{escaped}\"></pcn>");
                    }
                    else throw new KeyNotFoundException("Unable to understand given color");
                }
                output.Add("<pcbr/>");
            }

            output.RemoveRange(output.Count - 3, 3);
            output.Add("</pcode>");
            return string.Join("", output);
        }

        public string Convert(IEnumerable<char> input, string langName)
        {
            return Convert(input, langToColorToToken[langName]);
        }
    }
}
